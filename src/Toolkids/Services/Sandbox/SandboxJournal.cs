using System;
using System.IO;
using Newtonsoft.Json;

namespace Toolkids.Services.Sandbox
{
    public enum SandboxPhase { Idle, Restoring, Restored, Running, BackingUp, Cleaning }

    public sealed class JournalState
    {
        public SandboxPhase Phase { get; set; } = SandboxPhase.Idle;
        public string Tool { get; set; } = "";
        public string Time { get; set; } = "";
    }

    /// <summary>
    /// 事务日志：记录沙盒当前处于哪个阶段。崩溃/断电后，下次运行据此判断
    /// 系统里是否残留了“已还原但未清理”的内容，从而先做一次恢复清理。
    /// </summary>
    public sealed class SandboxJournal
    {
        private readonly string _path;

        public SandboxJournal(string backupDir) => _path = Path.Combine(backupDir, ".journal.json");

        public JournalState? Read()
        {
            try
            {
                return File.Exists(_path)
                    ? JsonConvert.DeserializeObject<JournalState>(File.ReadAllText(_path))
                    : null;
            }
            catch { return null; }
        }

        public void Set(SandboxPhase phase, string tool)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                File.WriteAllText(_path, JsonConvert.SerializeObject(
                    new JournalState { Phase = phase, Tool = tool, Time = DateTime.Now.ToString("s") }));
            }
            catch { /* 日志失败不阻断主流程 */ }
        }

        public void Clear()
        {
            try { if (File.Exists(_path)) File.Delete(_path); } catch { }
        }

        /// <summary>该阶段是否意味着系统里可能残留了还原内容。</summary>
        public static bool IndicatesResidue(SandboxPhase p) =>
            p is SandboxPhase.Restoring or SandboxPhase.Restored or SandboxPhase.Running
              or SandboxPhase.BackingUp or SandboxPhase.Cleaning;
    }
}
