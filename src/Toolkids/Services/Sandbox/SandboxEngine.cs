using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Toolkids.Models;

namespace Toolkids.Services.Sandbox
{
    /// <summary>
    /// 沙盒引擎：编排“启动前还原 → 运行并等待 → 退出后备份并清理”，带崩溃恢复。
    /// 本类不依赖 WinForms；与用户的交互通过 <see cref="ISandboxInteraction"/> 回调。
    /// 应在后台线程调用 <see cref="Run"/>（其中的等待会阻塞到软件退出）。
    /// </summary>
    public sealed class SandboxEngine
    {
        private readonly AppLauncher _launcher;
        private readonly Logger _log;

        public SandboxEngine(AppLauncher launcher, Logger log)
        {
            _launcher = launcher;
            _log = log;
        }

        public void Run(ToolItem tool, AppSettings settings, ISandboxInteraction ui)
        {
            SandboxRules rules = tool.Config.SandboxRules;
            bool sandbox = (rules.Registry.Count > 0 || rules.Files.Count > 0)
                           && !(PeEnvironment.IsWinPE && settings.SkipSandboxInPE);

            if (!sandbox)
            {
                LaunchOnly(tool);
                return;
            }

            var storage = new SandboxStorage(tool);
            var journal = new SandboxJournal(storage.BackupDir);

            // 0. 崩溃恢复：上次没走到“清理完成” → 先把系统里的残留备份+清理
            RecoverIfNeeded(tool, storage, journal);

            // 1. 还原
            if (storage.HasAnyBackup(rules))
            {
                var reg = new List<string>(rules.Registry);
                var files = new List<string>(rules.Files);

                List<ConflictItem> conflicts = DetectConflicts(rules);
                if (conflicts.Count > 0 && settings.ConfirmConflictBeforeRestore)
                {
                    ConflictDecision decision = ui.ResolveConflicts(tool.DisplayName, conflicts);
                    if (!decision.Proceed)
                    {
                        _log.Info("用户取消还原/启动：" + tool.DisplayName);
                        return;
                    }
                    foreach (ConflictItem c in decision.Items)
                        if (!c.Overwrite)
                        {
                            if (c.Kind == ConflictKind.Registry) reg.Remove(c.Rule);
                            else files.Remove(c.Rule);
                        }
                }

                ui.OnProgress("正在还原设置…");
                journal.Set(SandboxPhase.Restoring, tool.FolderName);
                Restore(storage, reg, files);
                journal.Set(SandboxPhase.Restored, tool.FolderName);
            }

            // 2. 启动 + 等待退出
            ui.OnProgress("软件启动中…");
            journal.Set(SandboxPhase.Running, tool.FolderName);
            Process proc;
            try
            {
                proc = _launcher.Launch(tool);
            }
            catch (Win32Exception w) when (w.NativeErrorCode == 1223) // 用户在 UAC 点了取消
            {
                _log.Info("用户取消提权，回滚清理：" + tool.DisplayName);
                CleanupAll(rules);
                journal.Clear();
                return;
            }
            catch (Exception ex)
            {
                _log.Error("启动失败，回滚清理", ex);
                CleanupAll(rules);
                journal.Clear();
                ui.OnError("启动失败：" + ex.Message);
                return;
            }

            ui.OnProgress("等待软件退出…");
            ProcessWaiter.Wait(proc, tool.Config.Launch.WaitMode, tool.Config.Launch.WaitProcessName);

            // 3. 备份 + 清理
            if (settings.AskBackupOnExit && !ui.ConfirmBackupOnExit(tool.DisplayName))
            {
                _log.Info("用户选择不备份/清理，保留系统现状：" + tool.DisplayName);
                journal.Clear();
                return;
            }

            ui.OnProgress("正在备份并清理…");
            journal.Set(SandboxPhase.BackingUp, tool.FolderName);
            BackupAll(storage, rules);
            journal.Set(SandboxPhase.Cleaning, tool.FolderName);
            CleanupAll(rules);
            journal.Clear();
            _log.Info("沙盒完成：" + tool.DisplayName);
        }

        private void LaunchOnly(ToolItem tool)
        {
            try { _launcher.Launch(tool); }
            catch (Win32Exception w) when (w.NativeErrorCode == 1223) { _log.Info("用户取消提权：" + tool.DisplayName); }
        }

        private void RecoverIfNeeded(ToolItem tool, SandboxStorage storage, SandboxJournal journal)
        {
            JournalState? st = journal.Read();
            if (st == null || !SandboxJournal.IndicatesResidue(st.Phase)) return;

            _log.Warn($"检测到上次未完成的沙盒({st.Phase})，执行恢复清理：{tool.DisplayName}");
            try
            {
                BackupAll(storage, tool.Config.SandboxRules);
                CleanupAll(tool.Config.SandboxRules);
            }
            catch (Exception ex) { _log.Error("恢复清理失败", ex); }
            journal.Clear();
        }

        private static List<ConflictItem> DetectConflicts(SandboxRules rules)
        {
            var list = new List<ConflictItem>();
            foreach (string r in rules.Registry)
                if (RegistryHelper.KeyExistsAny(r))
                    list.Add(new ConflictItem { Kind = ConflictKind.Registry, Rule = r, Target = r });
            foreach (string f in rules.Files)
            {
                string target = EnvPaths.Expand(f);
                if (FileOps.Exists(target))
                    list.Add(new ConflictItem { Kind = ConflictKind.File, Rule = f, Target = target });
            }
            return list;
        }

        private void Restore(SandboxStorage storage, List<string> reg, List<string> files)
        {
            foreach (string r in reg)
                foreach (RegView v in RegistryHelper.ViewsFor(r))
                {
                    string regFile = storage.RegFile(r, v);
                    if (File.Exists(regFile)) { RegistryHelper.Import(regFile, v); _log.Info($"还原注册表[{v}]：" + r); }
                }
            foreach (string f in files)
            {
                string store = storage.FileStore(f);
                if (FileOps.Exists(store)) { FileOps.CopyAny(store, EnvPaths.Expand(f)); _log.Info("还原目录：" + f); }
            }
        }

        private void BackupAll(SandboxStorage storage, SandboxRules rules)
        {
            Directory.CreateDirectory(storage.RegDir);
            Directory.CreateDirectory(storage.FilesDir);

            foreach (string r in rules.Registry)
                foreach (RegView v in RegistryHelper.ViewsFor(r))
                {
                    if (!RegistryHelper.KeyExists(r, v)) continue;
                    try { RegistryHelper.Export(r, storage.RegFile(r, v), v); _log.Info($"备份注册表[{v}]：" + r); }
                    catch (Exception ex) { _log.Error("备份注册表失败：" + r, ex); }
                }
            foreach (string f in rules.Files)
            {
                string target = EnvPaths.Expand(f);
                if (!FileOps.Exists(target)) continue;
                try
                {
                    string store = storage.FileStore(f);
                    FileOps.DeleteAny(store);        // 清掉旧备份再写
                    FileOps.CopyAny(target, store);
                    _log.Info("备份目录：" + f);
                }
                catch (Exception ex) { _log.Error("备份目录失败：" + f, ex); }
            }
        }

        private void CleanupAll(SandboxRules rules)
        {
            foreach (string r in rules.Registry)
                foreach (RegView v in RegistryHelper.ViewsFor(r))
                {
                    try { RegistryHelper.DeleteKey(r, v); _log.Info($"清理注册表[{v}]：" + r); }
                    catch (Exception ex) { _log.Error("清理注册表失败：" + r, ex); }
                }
            foreach (string f in rules.Files)
            {
                try { FileOps.DeleteAny(EnvPaths.Expand(f)); _log.Info("清理目录：" + f); }
                catch (Exception ex) { _log.Error("清理目录失败：" + f, ex); }
            }
        }
    }
}
