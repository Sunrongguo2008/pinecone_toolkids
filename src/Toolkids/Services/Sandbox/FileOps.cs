using System;
using System.IO;
using System.Threading;

namespace Toolkids.Services.Sandbox
{
    /// <summary>文件/目录复制与删除（删除带重试，应对刚退出软件时句柄未释放）。</summary>
    public static class FileOps
    {
        public static bool Exists(string path) =>
            !string.IsNullOrEmpty(path) && (File.Exists(path) || Directory.Exists(path));

        /// <summary>复制文件或目录（自动判别）。</summary>
        public static void CopyAny(string source, string dest)
        {
            if (File.Exists(source)) CopyFile(source, dest);
            else if (Directory.Exists(source)) CopyDir(source, dest);
        }

        private static void CopyFile(string source, string dest)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(source, dest, overwrite: true);
        }

        private static void CopyDir(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(dest, Path.GetRelativePath(source, dir)));
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(dest, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target, overwrite: true);
            }
        }

        /// <summary>删除文件或目录（带重试）。不存在则无操作。</summary>
        public static void DeleteAny(string path)
        {
            if (File.Exists(path)) DeleteWithRetry(() => File.Delete(path));
            else if (Directory.Exists(path)) DeleteWithRetry(() => Directory.Delete(path, recursive: true));
        }

        private static void DeleteWithRetry(Action del)
        {
            for (int i = 0; i < 3; i++)
            {
                try { del(); return; }
                catch (IOException) { Thread.Sleep(300); }
                catch (UnauthorizedAccessException) { Thread.Sleep(300); }
            }
            del(); // 最后一次，失败则把异常抛给调用方
        }
    }
}
