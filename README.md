# 🧰 Toolkids 便携工具箱

![License](https://img.shields.io/github/license/Sunrongguo2008/pinecone_toolkids)
![Release](https://img.shields.io/github/v/release/Sunrongguo2008/pinecone_toolkids)
![Downloads](https://img.shields.io/github/downloads/Sunrongguo2008/pinecone_toolkids/total)
![Platform](https://img.shields.io/badge/platform-Win7~11%20%7C%20PE-blue)
![.NET](https://img.shields.io/badge/.NET-6.0-512BD4)

给绿色软件穿上"隐身衣"的 Windows 工具箱。

Windows 便携式工具箱（类“图吧工具箱”），核心特色是给绿色软件做**便携化运行**：启动前还原、退出后备份并清理系统里的注册表/目录，做到“用完不留配置垃圾”；并带一个**沙盒扫描**功能，自动找出软件用到的注册表项/目录、一键生成规则。自定添加你的软件，**把它变成你的专属工具箱**！

软件用完,系统干干净净。配置垃圾?不存在的。✨

> ⚠️ 其实说白了是备份+还原,不是真隔离。别拿它当虚拟机使。

## ✨ 能干啥

- 🗂️ **分类管理** —— 左边分类,右边程序图标墙。增删改,随你折腾。
- 🫥 **便携化运行**
  - 开跑前,工具箱把它的注册表/目录还原回系统;
  - 撞上同名的?弹窗问你覆盖吗,你一项项勾;
  - 环境准备好了，开心用软件吧！
  - 软件退出后,备份+清理,挥一挥衣袖,不带走一片☁️。
  - 🛡️ 崩溃?断电?日志记着呢,下次自动收拾残局。
- 🔍 **沙盒扫描** —— 开软件前后各拍一张"快照",一对比就知道它动了啥。勾一勾,规则自动生成。不用你猜。
- 🌚 **深色界面** —— 扁平,朴素,不花哨。Win7 和 PE 也认。
- 💿 **PE 友好** —— 进 PE 自动识别;只读盘也能凑合,配置会自己找地方落脚。

## 💻 跑得动吗

| 在哪跑 | 行不行 | 备注 |
|---|:---:|---|
| Win 11 / 10 / 8.1 / 8 | ✅ | 自包含,免装运行时 |
| Win 7 SP1（x64） | ✅ | 老机器可能要先打个补丁 KB4474419 |
| WinPE（x64） | ✅ | 实测能跑 🎉 |
| 32位系统 / 32位PE | ❌ | 目前只出 x64 |

## 🛠️ 用啥做的

- **.NET 6** —— 最后一个还认 Win7 的 .NET,还能打包成单文件。
- **WinForms** —— 不花哨,但在 PE 里最稳定。🪨
- 自包含单文件 + 压缩,只出 x64。整个就 **几十MB**。
- 第三方依赖?就一个 **Newtonsoft.Json**。够克制吧。😎

## 🔨 自己编译

先装个 .NET SDK(8.x 就行)。进 `src/Toolkids/`,然后:

```bash
# 编译瞅一眼
dotnet build -c Release

# 正式打包(自包含 + 单文件 + 压缩,约 61MB)
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true \
  -o ../../dist/Toolkids
```

## 📁 里头都有啥

```
src/Toolkids/      主程序。Models / Services / UI 三层,不是一锅粥。🍲
  Services/Sandbox/  沙盒与扫描核心:快照、进程树等待、事务日志……
testapp/           配套小白鼠 🐭(往固定的注册表/目录写数据,专供测试)
docs/
  开发文档.md   架构、约定、怎么往下接
  需求.md            当初是咋想的
```

## 📖 想细看

- 需求与设计 👉 [`docs/需求.md`](docs/需求.md)
- 开发文档 👉 [`docs/开发文档.md`](docs/开发文档.md)

## 📜 许可证

MIT 📄 —— 详见 [LICENSE](LICENSE)。

## 🙏 免责声明

这玩意儿会**真的改你的注册表和文件**(便携化就指着这个)。

搞清楚它在干嘛,再动手。😅

拿不准?先拿 `testapp` 那只小白鼠试试水 —— 别一上来就对着重要配置软件开刀。
