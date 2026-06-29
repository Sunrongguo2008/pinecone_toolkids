using System;
using Microsoft.Win32;

namespace Toolkids.Services
{
    /// <summary>WinPE 环境探测（结果缓存）。</summary>
    public static class PeEnvironment
    {
        private static readonly Lazy<bool> _isWinPE = new(Detect);

        /// <summary>当前是否运行在 WinPE 中。</summary>
        public static bool IsWinPE => _isWinPE.Value;

        // WinPE 的标志：存在注册表项 HKLM\SYSTEM\CurrentControlSet\Control\MiniNT
        private static bool Detect()
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\MiniNT");
                return key != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
