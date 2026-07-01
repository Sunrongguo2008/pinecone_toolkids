using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Toolkids.Services.Sandbox
{
    /// <summary>注册表的 32 / 64 位视图。</summary>
    public enum RegView { Bit64, Bit32 }

    /// <summary>
    /// 注册表操作。存在性检查用 .NET API；导入/导出/删除用 reg.exe（处理 .reg 最省事）。
    /// HKLM / HKCR 会同时处理 32 与 64 两个视图（32 位程序的 HKLM\Software 实际落在 WOW6432Node）；
    /// HKCU / HKU / HKCC 不受 WOW64 重定向影响，只走一个视图。
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

        /// <summary>该规则需要操作的视图：HKLM / HKCR 两个（含 32 位重定向），其它只一个。</summary>
        public static IReadOnlyList<RegView> ViewsFor(string regPath)
        {
            if (TryParse(regPath, out RegistryHive hive, out _)
                && (hive == RegistryHive.LocalMachine || hive == RegistryHive.ClassesRoot))
                return new[] { RegView.Bit64, RegView.Bit32 };
            return new[] { RegView.Bit64 };
        }

        public static bool KeyExists(string regPath, RegView view)
        {
            if (!TryParse(regPath, out RegistryHive hive, out string sub) || sub.Length == 0) return false;
            try
            {
                using RegistryKey baseKey = RegistryKey.OpenBaseKey(
                    hive, view == RegView.Bit32 ? RegistryView.Registry32 : RegistryView.Registry64);
                using RegistryKey? k = baseKey.OpenSubKey(sub);
                return k != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>任一视图存在即算存在。</summary>
        public static bool KeyExistsAny(string regPath)
        {
            foreach (RegView v in ViewsFor(regPath))
                if (KeyExists(regPath, v)) return true;
            return false;
        }

        public static void Export(string regPath, string regFile, RegView view) =>
            RunReg($"export \"{regPath}\" \"{regFile}\" /y {Flag(view)}");

        public static void Import(string regFile, RegView view) =>
            RunReg($"import \"{regFile}\" {Flag(view)}");

        public static void DeleteKey(string regPath, RegView view)
        {
            if (!KeyExists(regPath, view)) return;
            RunReg($"delete \"{regPath}\" /f {Flag(view)}");
        }

        /// <summary>去掉路径里的 <c>WOW6432Node</c> 段：扫描到 32 位项时用，得到干净的逻辑路径。</summary>
        public static string NormalizeWow(string regPath) =>
            Regex.Replace(regPath, @"\\WOW6432Node(?=\\|$)", "", RegexOptions.IgnoreCase);

        private static string Flag(RegView v) => v == RegView.Bit32 ? "/reg:32" : "/reg:64";

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
