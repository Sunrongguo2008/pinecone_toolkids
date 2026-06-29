using System.Collections.Generic;

namespace Toolkids.Services.Sandbox
{
    public enum ConflictKind { Registry, File }

    /// <summary>还原时与系统现有内容的一处冲突。</summary>
    public sealed class ConflictItem
    {
        public ConflictKind Kind { get; init; }

        /// <summary>原始规则（带变量），用于回查备份。</summary>
        public string Rule { get; init; } = "";

        /// <summary>系统中的实际位置（展开后），用于展示。</summary>
        public string Target { get; init; } = "";

        /// <summary>用户是否选择覆盖（默认覆盖）。</summary>
        public bool Overwrite { get; set; } = true;
    }

    /// <summary>冲突对话框的结果。</summary>
    public sealed class ConflictDecision
    {
        /// <summary>false = 取消整个还原与启动。</summary>
        public bool Proceed { get; init; }

        public IReadOnlyList<ConflictItem> Items { get; init; } = new List<ConflictItem>();
    }

    /// <summary>
    /// 沙盒引擎在后台线程运行，需要与用户交互时通过本接口回调。
    /// UI 层负责把这些调用 marshal 回主线程。
    /// </summary>
    public interface ISandboxInteraction
    {
        ConflictDecision ResolveConflicts(string toolName, IReadOnlyList<ConflictItem> conflicts);
        bool ConfirmBackupOnExit(string toolName);
        void OnProgress(string message);
        void OnError(string message);
    }
}
