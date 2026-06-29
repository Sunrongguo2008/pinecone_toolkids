using System;
using System.Text;

namespace Toolkids.Services.Sandbox
{
    /// <summary>路径/名称工具：展开环境变量、把规则字符串转成安全的存储名。</summary>
    public static class EnvPaths
    {
        /// <summary>展开 <c>%AppData%</c> 等环境变量。</summary>
        public static string Expand(string pathWithVars) =>
            Environment.ExpandEnvironmentVariables(pathWithVars ?? "").Trim();

        private static readonly string[] PortableVars =
            { "%AppData%", "%LocalAppData%", "%ProgramData%", "%UserProfile%", "%Temp%" };

        /// <summary>把绝对路径尽量还原成带变量的便携形式（取最长匹配的变量），用于扫描结果写回规则。</summary>
        public static string ToPortable(string absPath)
        {
            string bestVar = "", bestExp = "";
            foreach (string v in PortableVars)
            {
                string exp = Expand(v);
                if (exp.Length > bestExp.Length && absPath.StartsWith(exp, StringComparison.OrdinalIgnoreCase))
                {
                    bestVar = v;
                    bestExp = exp;
                }
            }
            return bestVar.Length == 0 ? absPath : bestVar + absPath.Substring(bestExp.Length);
        }

        /// <summary>
        /// 把一条规则（如 <c>%AppData%\CPUID</c> 或 <c>HKCU\Software\X</c>）映射为稳定且安全的存储名，
        /// 用作 Backup 下的文件/文件夹名。基于规则原文，跨机器/用户稳定。
        /// </summary>
        public static string SafeName(string rule)
        {
            var sb = new StringBuilder(rule.Length);
            foreach (char c in rule)
                sb.Append(char.IsLetterOrDigit(c) || c == '-' ? c : '_');
            string s = sb.ToString().Trim('_');
            if (s.Length > 60) s = s.Substring(0, 60);
            if (s.Length == 0) s = "item";
            return s + "_" + Fnv1a(rule);
        }

        // 稳定的短哈希（FNV-1a），避免不同规则清洗后同名
        private static string Fnv1a(string s)
        {
            uint h = 2166136261;
            foreach (char c in s)
            {
                h ^= c;
                h *= 16777619;
            }
            return h.ToString("x8");
        }
    }
}
