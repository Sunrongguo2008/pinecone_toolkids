namespace Toolkids.Models
{
    /// <summary>
    /// 运行时的“软件”对象：把磁盘上的文件夹、其 <see cref="ToolConfig"/> 和解析后的路径聚到一起。
    /// 不参与序列化。
    /// </summary>
    public sealed class ToolItem
    {
        /// <summary>软件文件夹名（Data 下的子目录名），作为唯一标识。</summary>
        public string FolderName { get; }

        /// <summary>软件文件夹的绝对路径。</summary>
        public string FolderPath { get; }

        /// <summary>该软件的配置。</summary>
        public ToolConfig Config { get; }

        public ToolItem(string folderName, string folderPath, ToolConfig config)
        {
            FolderName = folderName;
            FolderPath = folderPath;
            Config = config;
        }

        /// <summary>用于界面显示的名称（配置名优先，否则用文件夹名）。</summary>
        public string DisplayName =>
            string.IsNullOrWhiteSpace(Config.Name) ? FolderName : Config.Name;
    }
}
