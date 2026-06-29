@echo off
chcp 65001 >nul
cd /d "%~dp0"

echo ============================================
echo   编译 Toolkids PE 测试程序（x64 自包含）
echo ============================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
  echo *** 没找到 dotnet，说明还没装 .NET SDK ***
  echo 请先安装 .NET SDK（x64），再运行本脚本。
  pause
  exit /b 1
)

echo [1/2] 生成“单文件版”...
dotnet publish PeTest.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "dist\singlefile"
if errorlevel 1 goto fail

echo.
echo [2/2] 生成“文件夹版”（备用，万一单文件在 PE 里释放失败）...
dotnet publish PeTest.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o "dist\folder"
if errorlevel 1 goto fail

echo.
echo ================= 完成 =================
echo 单文件版 : petest\dist\singlefile\PeTest.exe   （只有一个 exe，优先用这个）
echo 文件夹版 : petest\dist\folder\                  （整个文件夹拷过去，运行里面的 PeTest.exe）
echo.
echo 拿到 WinPE / Win7 x64 上：先双击单文件版；若不弹窗，再试文件夹版。
echo 把结果发我：哪个弹了窗、截图或那份 Toolkids_PEtest.txt；都没弹就把报错发我。
echo =======================================
pause
exit /b 0

:fail
echo.
echo *** 编译失败，请把上面整段错误复制发我 ***
pause
exit /b 1
