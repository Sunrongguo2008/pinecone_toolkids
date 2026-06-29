using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Toolkids.Models;

namespace Toolkids.Services
{
    /// <summary>提取并缓存软件图标。</summary>
    public sealed class IconService : IDisposable
    {
        private readonly Logger _log;
        private readonly Dictionary<string, Image> _cache = new(StringComparer.OrdinalIgnoreCase);

        public IconService(Logger log) => _log = log;

        /// <summary>取得某软件用于显示的图标；失败返回 <c>null</c>（调用方用占位图）。</summary>
        public Image? GetIcon(ToolItem tool)
        {
            string source = ResolveIconSource(tool);
            if (source.Length == 0) return null;
            if (_cache.TryGetValue(source, out Image? cached)) return cached;

            Image? img = LoadFrom(source);
            if (img != null) _cache[source] = img;
            return img;
        }

        // 图标来源优先级：配置的 Icon -> 启动 exe
        private static string ResolveIconSource(ToolItem tool)
        {
            string folder = tool.FolderPath;

            string? icon = tool.Config.Icon;
            if (!string.IsNullOrWhiteSpace(icon))
            {
                string p = Path.Combine(folder, icon);
                if (File.Exists(p)) return p;
            }

            string exe = tool.Config.Launch.Exe;
            if (!string.IsNullOrWhiteSpace(exe))
            {
                string p = Path.Combine(folder, exe);
                if (File.Exists(p)) return p;
            }

            return "";
        }

        private Image? LoadFrom(string path)
        {
            try
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                switch (ext)
                {
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                    case ".bmp":
                    case ".gif":
                        return Image.FromFile(path);
                    case ".ico":
                        using (var ic = new Icon(path)) return ic.ToBitmap();
                    default:
                    {
                        // exe / dll：优先用 Shell 大图标(48px)，更清晰；失败回退 32px
                        Image? large = Win32IconExtractor.GetLargeIcon(path);
                        if (large != null) return large;
                        using Icon? extracted = Icon.ExtractAssociatedIcon(path);
                        return extracted?.ToBitmap();
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Warn("加载图标失败：" + path + " | " + ex.Message);
                return null;
            }
        }

        public void Dispose()
        {
            foreach (Image img in _cache.Values) img.Dispose();
            _cache.Clear();
        }
    }
}
