using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Toolkids.Models;

namespace Toolkids.Services.Sandbox
{
    /// <summary>
    /// 沙盒扫描：拍快照 → 启动软件并等待退出 → 再拍快照 → 对比出新增的注册表/文件 →
    /// 让用户勾选 → 写回该软件的 <see cref="SandboxRules"/>。
    /// 不依赖 WinForms；应在后台线程调用。返回写入的规则条数。
    /// </summary>
    public sealed class ScanEngine
    {
        private readonly AppLauncher _launcher;
        private readonly Logger _log;

        public ScanEngine(AppLauncher launcher, Logger log)
        {
            _launcher = launcher;
            _log = log;
        }

        public int Scan(ToolItem tool, SnapshotScope scope, IScanInteraction ui)
        {
            _log.Info("扫描开始：" + tool.DisplayName);

            ui.OnProgress("正在拍摄快照（启动前）…");
            SystemSnapshot before = Snapshotter.Take(scope, _log);

            ui.OnProgress("软件启动中…");
            Process proc;
            try
            {
                proc = _launcher.Launch(tool);
            }
            catch (Win32Exception w) when (w.NativeErrorCode == 1223)
            {
                _log.Info("用户取消提权，扫描中止");
                return 0;
            }
            catch (Exception ex)
            {
                _log.Error("扫描启动失败", ex);
                ui.OnError("启动失败：" + ex.Message);
                return 0;
            }

            ui.OnProgress("等待软件退出…");
            ProcessWaiter.Wait(proc, tool.Config.Launch.WaitMode, tool.Config.Launch.WaitProcessName);

            ui.OnProgress("正在拍摄快照（退出后）…");
            SystemSnapshot after = Snapshotter.Take(scope, _log);
            ui.OnProgress("正在对比差异…");
            (List<string> regKeys, List<string> files) = Snapshotter.Diff(before, after);

            var items = new List<ScanItem>();
            foreach (string r in regKeys) items.Add(new ScanItem { Kind = ScanKind.Registry, Rule = RegistryHelper.NormalizeWow(r) });
            foreach (string f in files) items.Add(new ScanItem { Kind = ScanKind.File, Rule = EnvPaths.ToPortable(f) });

            // 去重并排序（注册表在前）
            items = items
                .GroupBy(i => i.Kind + "|" + i.Rule, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(i => i.Kind)
                .ThenBy(i => i.Rule, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _log.Info($"扫描发现 {items.Count} 个新增项：" + tool.DisplayName);
            if (items.Count == 0)
            {
                ui.OnInfo("没有检测到该软件新增的注册表项或文件。\r\n（可能它把设置写在了扫描范围之外，可在“设置”里调整扫描范围。）");
                return 0;
            }

            IReadOnlyList<ScanItem>? chosen = ui.ChooseItems(tool.DisplayName, items);
            if (chosen == null || chosen.Count == 0) return 0;

            return Merge(tool.Config.SandboxRules, chosen);
        }

        private static int Merge(SandboxRules rules, IReadOnlyList<ScanItem> items)
        {
            int added = 0;
            foreach (ScanItem it in items)
            {
                List<string> list = it.Kind == ScanKind.Registry ? rules.Registry : rules.Files;
                if (!list.Contains(it.Rule, StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(it.Rule);
                    added++;
                }
            }
            return added;
        }
    }
}
