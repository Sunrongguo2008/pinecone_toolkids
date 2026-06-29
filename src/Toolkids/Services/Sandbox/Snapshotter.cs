using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using Toolkids.Models;

namespace Toolkids.Services.Sandbox
{
    /// <summary>系统快照（限定范围内的注册表项集合 + 文件/目录路径集合）。</summary>
    public sealed class SystemSnapshot
    {
        public HashSet<string> Registry { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>拍快照与对比差异。只在 <see cref="SnapshotScope"/> 限定的范围内扫描，保持“轻量”。</summary>
    public static class Snapshotter
    {
        private const int MaxEntries = 200_000; // 安全上限，避免 %LocalAppData% 之类把内存撑爆

        // 跳过这些“噪声/巨大”目录（缓存/临时/UWP 包等）：扫描更快、差异更干净。
        // 仍会把目录本身计入快照（新增时显示为一项），只是不深入遍历。
        private static readonly HashSet<string> SkipDirNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "Cache", "Caches", "Code Cache", "GPUCache", "GrShaderCache", "ShaderCache", "DawnCache",
            "Temp", "Temporary Internet Files", "INetCache", "WebCache", "Packages", "CrashDumps"
        };

        public static SystemSnapshot Take(SnapshotScope scope, Logger log)
        {
            var snap = new SystemSnapshot();
            foreach (string root in scope.RegistryRoots) EnumReg(root, snap.Registry, log);
            foreach (string root in scope.FileRoots) EnumFiles(root, snap.Files, log);
            return snap;
        }

        /// <summary>对比，返回“新增”的注册表项与文件（各自只保留最顶层的新增项）。</summary>
        public static (List<string> regKeys, List<string> files) Diff(SystemSnapshot before, SystemSnapshot after)
        {
            var newReg = new List<string>();
            foreach (string k in after.Registry)
                if (!before.Registry.Contains(k)) newReg.Add(k);

            var newFiles = new List<string>();
            foreach (string f in after.Files)
                if (!before.Files.Contains(f)) newFiles.Add(f);

            return (CollapseTops(newReg, '\\'), CollapseTops(newFiles, Path.DirectorySeparatorChar));
        }

        // 若 A\B 和 A\B\C 都是新增的，只保留 A\B（最顶层）
        private static List<string> CollapseTops(IEnumerable<string> paths, char sep)
        {
            var set = new HashSet<string>(paths, StringComparer.OrdinalIgnoreCase);
            var tops = new List<string>();
            foreach (string p in set)
            {
                bool hasAncestor = false;
                string cur = p;
                int idx;
                while ((idx = cur.LastIndexOf(sep)) > 0)
                {
                    cur = cur.Substring(0, idx);
                    if (set.Contains(cur)) { hasAncestor = true; break; }
                }
                if (!hasAncestor) tops.Add(p);
            }
            tops.Sort(StringComparer.OrdinalIgnoreCase);
            return tops;
        }

        private static void EnumReg(string root, HashSet<string> set, Logger log)
        {
            if (!RegistryHelper.TryParse(root, out RegistryHive hive, out string sub)) return;
            try
            {
                using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using RegistryKey? start = baseKey.OpenSubKey(sub);
                if (start != null) Recurse(start, root, set, 0);
            }
            catch (Exception ex) { log.Warn("快照注册表失败：" + root + " | " + ex.Message); }
        }

        private static void Recurse(RegistryKey key, string path, HashSet<string> set, int depth)
        {
            if (depth > 40 || set.Count > MaxEntries) return;
            string[] subs;
            try { subs = key.GetSubKeyNames(); } catch { return; }
            foreach (string s in subs)
            {
                string childPath = path + "\\" + s;
                set.Add(childPath);
                if (set.Count > MaxEntries) return;
                try
                {
                    using RegistryKey? child = key.OpenSubKey(s);
                    if (child != null) Recurse(child, childPath, set, depth + 1);
                }
                catch { }
            }
        }

        private static void EnumFiles(string rootVar, HashSet<string> set, Logger log)
        {
            string root = EnvPaths.Expand(rootVar);
            if (!Directory.Exists(root)) return;

            var stack = new Stack<string>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                if (set.Count > MaxEntries) { log.Warn("快照文件达到上限，停止扫描：" + rootVar); return; }
                string dir = stack.Pop();
                string[] entries;
                try { entries = Directory.GetFileSystemEntries(dir); } catch { continue; }
                foreach (string e in entries)
                {
                    set.Add(e);
                    if (Directory.Exists(e) && !SkipDirNames.Contains(Path.GetFileName(e)))
                        stack.Push(e);
                }
            }
        }
    }
}
