@echo off
chcp 65001 >nul
echo ========================================
echo  CCWater Controller Player 安装包构建
echo ========================================
echo.

echo [1/2] 编译项目...
dotnet publish "..\CCWaterControllerPlayer\CCWaterControllerPlayer.csproj" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
if %ERRORLEVEL% neq 0 (
    echo 编译失败！
    pause
    exit /b 1
)
echo 编译完成。
echo.

echo [2/2] 生成安装程序...
set "ISCC=C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if not exist "%ISCC%" (
    set "ISCC=C:\Program Files\Inno Setup 6\ISCC.exe"
)
if not exist "%ISCC%" (
    echo 错误: 未找到 Inno Setup 6
    echo 请从 https://jrsoftware.org/isdl.php 下载安装
    pause
    exit /b 1
)

"%ISCC%" setup.iss
if %ERRORLEVEL% neq 0 (
    echo 安装包生成失败！
    pause
    exit /b 1
)

echo.
echo ========================================
echo 安装包已生成: installer\Output\
echo ========================================
echo.
pause
