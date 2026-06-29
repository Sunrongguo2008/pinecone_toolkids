using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Toolkids.Models;

namespace Toolkids.Services
{
    /// <summary>管理 Data 目录下的软件：扫描、读取、增删。</summary>
    public sealed class ToolRepository
    {
        private readonly AppPaths _paths;
        private readonly ConfigStore _store;
        private readonly Logger _log;

        public ToolRepository(AppPaths paths, ConfigStore store, Logger log)
        {
            _paths = paths;
            _store = store;
            _log = log;
        }

        /// <summary>当前 Data 目录绝对路径。</summary>
        public string DataDir(GlobalConfig cfg) => _paths.GetDataDir(cfg.DataDir);

        /// <summary>加载某分类下的全部软件（按 <see cref="Category.Apps"/> 顺序，忽略不存在的）。</summary>
        public IReadOnlyList<ToolItem> LoadTools(GlobalConfig cfg, Category category)
        {
            string dataDir = DataDir(cfg);
            var list = new List<ToolItem>();
            foreach (string folderName in category.Apps)
            {
                string folder = Path.Combine(dataDir, folderName);
                if (!Directory.Exists(folder))
                {
                    _log.Warn("软件文件夹不存在，跳过：" + folder);
                    continue;
                }
                ToolConfig config = _store.LoadTool(folder);
                if (string.IsNullOrWhiteSpace(config.Name)) config.Name = folderName;
                list.Add(new ToolItem(folderName, folder, config));
            }
            return list;
        }

        /// <summary>Data 目录下的全部“软件文件夹名”（含尚未归类的），按名称排序。</summary>
        public IReadOnlyList<string> AllToolFolders(GlobalConfig cfg)
        {
            string dataDir = DataDir(cfg);
            if (!Directory.Exists(dataDir)) return Array.Empty<string>();
            return Directory.GetDirectories(dataDir)
                .Select(Path.GetFileName)
                .Where(n => !string.IsNullOrEmpty(n))
                .Select(n => n!)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>新建一个软件文件夹（含空的 Apps 子目录和默认 toolconfig.json）。</summary>
        public ToolItem CreateTool(GlobalConfig cfg, string folderName, ToolConfig config)
        {
            string folder = Path.Combine(DataDir(cfg), folderName);
            Directory.CreateDirectory(Path.Combine(folder, "Apps"));
            _store.SaveTool(folder, config);
            _log.Info("新建软件：" + folder);
            return new ToolItem(folderName, folder, config);
        }

        /// <summary>是否已存在同名文件夹。</summary>
        public bool FolderExists(GlobalConfig cfg, string folderName) =>
            Directory.Exists(Path.Combine(DataDir(cfg), folderName));

        public void SaveTool(ToolItem tool) => _store.SaveTool(tool.FolderPath, tool.Config);

        /// <summary>从磁盘删除软件文件夹（含其下所有文件）。</summary>
        public void DeleteToolFolder(GlobalConfig cfg, string folderName)
        {
            string folder = Path.Combine(DataDir(cfg), folderName);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
                _log.Info("删除软件文件夹：" + folder);
            }
        }
    }
}
