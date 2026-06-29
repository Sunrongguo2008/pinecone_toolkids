using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Toolkids.Models;

namespace Toolkids.Services
{
    /// <summary>负责 <c>kidconfig.conf</c> 与 <c>toolconfig.json</c> 的读写。</summary>
    public sealed class ConfigStore
    {
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Converters = { new StringEnumConverter() }
        };

        private readonly AppPaths _paths;
        private readonly Logger _log;

        public ConfigStore(AppPaths paths, Logger log)
        {
            _paths = paths;
            _log = log;
        }

        // ---------- 全局配置 ----------

        public GlobalConfig LoadGlobal()
        {
            string path = ResolveGlobalLoadPath();
            if (!File.Exists(path))
            {
                _log.Info("未找到全局配置，创建默认配置。");
                var def = new GlobalConfig();
                def.Categories.Add(new Category { Id = "general", Name = "常用工具" });
                TrySaveGlobal(def);
                return def;
            }
            try
            {
                var cfg = JsonConvert.DeserializeObject<GlobalConfig>(File.ReadAllText(path), JsonSettings);
                return cfg ?? new GlobalConfig();
            }
            catch (Exception ex)
            {
                _log.Error("读取全局配置失败，使用默认配置：" + path, ex);
                return new GlobalConfig();
            }
        }

        public void SaveGlobal(GlobalConfig cfg)
        {
            string path = ResolveGlobalSavePath();
            File.WriteAllText(path, JsonConvert.SerializeObject(cfg, JsonSettings));
            _log.Info("已保存全局配置：" + path);
        }

        private void TrySaveGlobal(GlobalConfig cfg)
        {
            try { SaveGlobal(cfg); }
            catch (Exception ex) { _log.Error("保存默认全局配置失败", ex); }
        }

        // ---------- 软件配置 ----------

        public ToolConfig LoadTool(string toolFolderPath)
        {
            string path = Path.Combine(toolFolderPath, AppPaths.ToolConfigFileName);
            if (!File.Exists(path)) return new ToolConfig();
            try
            {
                return JsonConvert.DeserializeObject<ToolConfig>(File.ReadAllText(path), JsonSettings)
                       ?? new ToolConfig();
            }
            catch (Exception ex)
            {
                _log.Error("读取软件配置失败：" + path, ex);
                return new ToolConfig();
            }
        }

        public void SaveTool(string toolFolderPath, ToolConfig config)
        {
            Directory.CreateDirectory(toolFolderPath);
            string path = Path.Combine(toolFolderPath, AppPaths.ToolConfigFileName);
            File.WriteAllText(path, JsonConvert.SerializeObject(config, JsonSettings));
        }

        // ---------- 路径解析 ----------

        // 读：优先程序目录里的配置（便携），否则可写目录里的
        private string ResolveGlobalLoadPath()
        {
            string inRoot = Path.Combine(_paths.RootDir, AppPaths.GlobalConfigFileName);
            if (File.Exists(inRoot)) return inRoot;
            return Path.Combine(_paths.WritableRoot, AppPaths.GlobalConfigFileName);
        }

        // 写：程序目录可写就写程序目录（便携），否则写可写目录（如 %TEMP%）
        private string ResolveGlobalSavePath() =>
            _paths.IsRootWritable
                ? Path.Combine(_paths.RootDir, AppPaths.GlobalConfigFileName)
                : Path.Combine(_paths.WritableRoot, AppPaths.GlobalConfigFileName);
    }
}
