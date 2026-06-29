using System.Collections.Generic;

namespace Toolkids.Services.Sandbox
{
    public enum ScanKind { Registry, File }

    /// <summary>扫描发现的一个“新增项”。</summary>
    public sealed class ScanItem
    {
        public ScanKind Kind { get; init; }

        /// <summary>便携形式的规则（注册表用 HKCU\...；文件用 %AppData%\... 等）。</summary>
        public string Rule { get; init; } = "";

        /// <summary>是否选中写入。</summary>
        public bool Selected { get; set; } = true;

        public string Display => $"[{(Kind == ScanKind.Registry ? "注册表" : "文件")}] {Rule}";
    }

    /// <summary>扫描引擎在后台线程运行，与用户交互通过本接口（UI 层 marshal 回主线程）。</summary>
    public interface IScanInteraction
    {
        /// <summary>展示扫描结果供勾选；返回选中的项，取消则返回 null。</summary>
        IReadOnlyList<ScanItem>? ChooseItems(string toolName, IReadOnlyList<ScanItem> found);

        void OnProgress(string message);
        void OnInfo(string message);
        void OnError(string message);
    }
}
