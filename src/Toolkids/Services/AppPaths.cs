using System;
using System.IO;

namespace Toolkids.Services
{
    /// <summary>
    /// 集中管理程序用到的路径，并处理只读介质（如 PE 光盘/ISO）下的可写回退。
    /// </summary>
    public sealed class AppPaths
    {
        public const string GlobalConfigFileName = "kidconfig.conf";
        public const string ToolConfigFileName = "toolconfig.json";

        /// <summary>工具箱程序所在目录（exe 旁）。</summary>
        public string RootDir { get; }

        /// <summary>程序所在目录是否可写。</summary>
        public bool IsRootWritable { get; }

        /// <summary>可写的数据根：RootDir 可写则用它，否则回退到 %TEMP%\Toolkids。</summary>
        public string WritableRoot { get; }

        /// <summary>日志文件路径。</summary>
        public string LogPath { get; }

        private AppPaths(string rootDir)
        {
            RootDir = rootDir;
            IsRootWritable = IsDirWritable(rootDir);
            WritableRoot = IsRootWritable ? rootDir : EnsureTempRoot();
            LogPath = Path.Combine(WritableRoot, "toolkids.log");
        }

        public static AppPaths Create()
        {
            string root = AppContext.BaseDirectory
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return new AppPaths(root);
        }

        /// <summary>软件容器目录（Data）的绝对路径。</summary>
        public string GetDataDir(string dataDirName) => Path.Combine(RootDir, dataDirName);

        private static string EnsureTempRoot()
        {
            string dir = Path.Combine(Path.GetTempPath(), "Toolkids");
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>探测某目录是否可写（写一个临时文件再删）。</summary>
        public static bool IsDirWritable(string dir)
        {
            try
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string probe = Path.Combine(dir, ".w_" + Guid.NewGuid().ToString("N") + ".tmp");
                File.WriteAllText(probe, "x");
                File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
