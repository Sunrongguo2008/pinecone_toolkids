namespace Toolkids.Services
{
    using Toolkids.Services.Sandbox;

    /// <summary>
    /// 服务容器（手动依赖注入的“组合根”）。
    /// 在 <c>Program.Main</c> 里构造一次，传给各窗体使用，避免到处用静态单例。
    /// </summary>
    public sealed class AppServices
    {
        public AppPaths Paths { get; }
        public Logger Log { get; }
        public ConfigStore Config { get; }
        public ToolRepository Tools { get; }
        public IconService Icons { get; }
        public AppLauncher Launcher { get; }
        public SandboxEngine Sandbox { get; }
        public ScanEngine Scan { get; }

        public AppServices(AppPaths paths, Logger log)
        {
            Paths = paths;
            Log = log;
            Config = new ConfigStore(paths, log);
            Tools = new ToolRepository(paths, Config, log);
            Icons = new IconService(log);
            Launcher = new AppLauncher(log);
            Sandbox = new SandboxEngine(Launcher, log);
            Scan = new ScanEngine(Launcher, log);
        }
    }
}
