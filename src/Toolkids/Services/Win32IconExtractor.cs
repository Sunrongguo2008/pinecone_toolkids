using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Toolkids.Services
{
    /// <summary>
    /// 用 Shell 的系统图像列表提取较大(48px)的程序图标，比 <c>Icon.ExtractAssociatedIcon</c>(32px) 更清晰。
    /// 仅适用于 exe/dll 等有 shell 图标的文件；失败返回 null 由调用方回退。
    /// </summary>
    internal static class Win32IconExtractor
    {
        public static Image? GetLargeIcon(string path)
        {
            try
            {
                var info = new SHFILEINFO();
                IntPtr r = SHGetFileInfo(path, 0, ref info, (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_SYSICONINDEX);
                if (r == IntPtr.Zero) return null;

                Guid iid = IID_IImageList;
                if (SHGetImageList(SHIL_EXTRALARGE, ref iid, out IntPtr himl) != 0 || himl == IntPtr.Zero)
                    return null;

                IntPtr hicon = ImageList_GetIcon(himl, info.iIcon, ILD_TRANSPARENT);
                if (hicon == IntPtr.Zero) return null;
                try
                {
                    using Icon ic = Icon.FromHandle(hicon);
                    return ic.ToBitmap();
                }
                finally { DestroyIcon(hicon); }
            }
            catch
            {
                return null;
            }
        }

        private const uint SHGFI_SYSICONINDEX = 0x4000;
        private const int SHIL_EXTRALARGE = 2; // 48x48
        private const int ILD_TRANSPARENT = 1;
        private static Guid IID_IImageList = new("46EB5926-582E-4017-9FDF-E8998DAA0950");

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("shell32.dll")]
        private static extern int SHGetImageList(int iImageList, ref Guid riid, out IntPtr ppv);

        [DllImport("comctl32.dll")]
        private static extern IntPtr ImageList_GetIcon(IntPtr himl, int i, int flags);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}
