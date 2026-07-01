using System.IO;
using Toolkids.Models;

namespace Toolkids.Services.Sandbox
{
    /// <summary>
    /// Backup 目录布局：
    /// <code>
    /// &lt;软件&gt;/Backup/
    ///   reg/&lt;safe&gt;.reg     注册表项导出
    ///   files/&lt;safe&gt;/...    文件/目录镜像
    ///   .journal.json        事务日志
    /// </code>
    /// </summary>
    public sealed class SandboxStorage
    {
        private readonly ToolItem _tool;

        public SandboxStorage(ToolItem tool) => _tool = tool;

        public string BackupDir => Path.Combine(_tool.FolderPath, "Backup");
        public string RegDir => Path.Combine(BackupDir, "reg");
        public string FilesDir => Path.Combine(BackupDir, "files");

        public string RegFile(string rule, RegView view) =>
            Path.Combine(RegDir, EnvPaths.SafeName(rule) + (view == RegView.Bit32 ? ".32.reg" : ".64.reg"));
        public string FileStore(string rule) => Path.Combine(FilesDir, EnvPaths.SafeName(rule));

        /// <summary>该软件是否已有任何备份内容（决定启动时要不要还原）。</summary>
        public bool HasAnyBackup(SandboxRules rules)
        {
            foreach (string r in rules.Registry)
                foreach (RegView v in RegistryHelper.ViewsFor(r))
                    if (File.Exists(RegFile(r, v))) return true;
            foreach (string f in rules.Files)
                if (FileOps.Exists(FileStore(f))) return true;
            return false;
        }
    }
}
