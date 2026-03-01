@echo off
REM ================================================================
REM  PDX 插件直接编译脚本（绕过 MSBuild/NuGet）
REM  使用 dotnet SDK 内置的 Roslyn csc.dll 直接编译
REM  输出: bin\Release\PDX.dll
REM ================================================================
setlocal

set DOTNET=dotnet
set CSC=C:\Program Files\dotnet\sdk\8.0.416\Roslyn\bincore\csc.dll
set ACAD_DIR=D:\app\cad18\AutoCAD 2018
set OUT_DIR=bin\Release
set NETFX_DIR=C:\Windows\Microsoft.NET\Framework64\v4.0.30319

if not exist "%OUT_DIR%" mkdir "%OUT_DIR%"

echo [INFO] 使用 Roslyn csc.dll: %CSC%
echo [INFO] AutoCAD SDK: %ACAD_DIR%
echo [INFO] 开始编译...

"%DOTNET%" exec "%CSC%" ^
  /target:library ^
  /out:"%OUT_DIR%\PDX.dll" ^
  /platform:x64 ^
  /optimize+ ^
  /nostdlib ^
  /noconfig ^
  /r:"%NETFX_DIR%\mscorlib.dll" ^
  /r:"%NETFX_DIR%\System.dll" ^
  /r:"%NETFX_DIR%\System.Core.dll" ^
  /r:"%ACAD_DIR%\AcCoreMgd.dll" ^
  /r:"%ACAD_DIR%\AcDbMgd.dll" ^
  /r:"%ACAD_DIR%\AcMgd.dll" ^
  src\Models\BreakerRule.cs ^
  src\Models\LoopData.cs ^
  src\Models\MainCircuitData.cs ^
  src\Rules\RuleEngine.cs ^
  src\Calculation\CalculationEngine.cs ^
  src\Drawing\LayerHelper.cs ^
  src\Drawing\DrawingEngine.cs ^
  src\Services\PdxService.cs ^
  src\Commands\PdxCommands.cs

if %ERRORLEVEL% equ 0 (
    REM 复制规则文件到输出目录
    copy /y "resources\pdx_rules.json" "%OUT_DIR%\pdx_rules.json" >nul
    echo.
    echo ============================================================
    echo  [成功] PDX.dll 已生成！
    echo  输出目录: %OUT_DIR%\
    echo    - PDX.dll
    echo    - pdx_rules.json
    echo.
    echo  AutoCAD 加载步骤:
    echo  1. 在 AutoCAD 命令行输入: NETLOAD
    echo  2. 选择 %CD%\%OUT_DIR%\PDX.dll
    echo  3. 输入命令: PDX
    echo ============================================================
) else (
    echo.
    echo ============================================================
    echo  [失败] 编译出错，请检查以上错误信息。
    echo ============================================================
)

endlocal
pause
