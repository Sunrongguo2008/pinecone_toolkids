using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace Toolkids.Services.Sandbox
{
    /// <summary>
    /// 注册表操作。检查存在性用 .NET API；导入/导出/删除用 reg.exe（处理 .reg 格式最省事）。
    /// 注：默认走 64 位视图（本程序为 x64）；HKCU 不受 WOW64 重定向影响，覆盖了最常见情况。
    /// </summary>
    public static class RegistryHelper
    {
        /// <summary>解析 <c>HKCU\Software\X</c> 为 (hive, 子键)。</summary>
        public static bool TryParse(string regPath, out RegistryHive hive, out string subKey)
        {
            hive = RegistryHive.CurrentUser;
            subKey = "";
            if (string.IsNullOrWhiteSpace(regPath)) return false;

            int slash = regPath.IndexOf('\\');
            string root = (slash < 0 ? regPath : regPath.Substring(0, slash)).Trim().ToUpperInvariant();
            subKey = slash < 0 ? "" : regPath.Substring(slash + 1);

            switch (root)
            {
                case "HKCU": case "HKEY_CURRENT_USER": hive = RegistryHive.CurrentUser; return true;
                case "HKLM": case "HKEY_LOCAL_MACHINE": hive = RegistryHive.LocalMachine; return true;
                case "HKCR": case "HKEY_CLASSES_ROOT": hive = RegistryHive.ClassesRoot; return true;
                case "HKU": case "HKEY_USERS": hive = RegistryHive.Users; return true;
                case "HKCC": case "HKEY_CURRENT_CONFIG": hive = RegistryHive.CurrentConfig; return true;
                default: return false;
            }
        }

        public static bool KeyExists(string regPath)
        {
            if (!TryParse(regPath, out RegistryHive hive, out string sub) || sub.Length == 0) return false;
            try
            {
                using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using RegistryKey? k = baseKey.OpenSubKey(sub);
                return k != null;
            }
            catch
            {
                return false;
            }
        }

        public static void Export(string regPath, string regFile) =>
            RunReg($"export \"{regPath}\" \"{regFile}\" /y");

        public static void Import(string regFile) =>
            RunReg($"import \"{regFile}\"");

        public static void DeleteKey(string regPath)
        {
            if (!KeyExists(regPath)) return;
            RunReg($"delete \"{regPath}\" /f");
        }

        private static void RunReg(string args)
        {
            var psi = new ProcessStartInfo("reg.exe", args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using Process p = Process.Start(psi) ?? throw new InvalidOperationException("无法启动 reg.exe");
            string err = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode != 0)
                throw new InvalidOperationException($"reg {args} 失败({p.ExitCode}): {err.Trim()}");
        }
    }
}
