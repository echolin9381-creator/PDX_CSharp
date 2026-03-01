@echo off
chcp 437 >nul
setlocal

set DOTNET=dotnet
set CSC=C:\Program Files\dotnet\sdk\8.0.416\Roslyn\bincore\csc.dll
set ACAD=D:\app\cad18\AutoCAD 2018
set OUT=bin\Release
set FX=C:\Windows\Microsoft.NET\Framework64\v4.0.30319

if not exist "%OUT%" mkdir "%OUT%"

"%DOTNET%" exec "%CSC%" ^
  /target:library ^
  /out:"%OUT%\PDX.dll" ^
  /platform:x64 ^
  /optimize+ ^
  /nostdlib ^
  /noconfig ^
  /r:"%FX%\mscorlib.dll" ^
  /r:"%FX%\System.dll" ^
  /r:"%FX%\System.Core.dll" ^
  /r:"%ACAD%\AcCoreMgd.dll" ^
  /r:"%ACAD%\AcDbMgd.dll" ^
  /r:"%ACAD%\AcMgd.dll" ^
  src\Models\BreakerRule.cs ^
  src\Models\LoopData.cs ^
  src\Models\MainCircuitData.cs ^
  src\Rules\RuleEngine.cs ^
  src\Calculation\CalculationEngine.cs ^
  src\Drawing\LayerHelper.cs ^
  src\Drawing\DrawingEngine.cs ^
  src\Services\PdxService.cs ^
  src\Commands\PdxCommands.cs

set BUILD_RESULT=%ERRORLEVEL%

if %BUILD_RESULT% equ 0 (
    copy /y "resources\pdx_rules.json" "%OUT%\pdx_rules.json" >nul
    echo BUILD_SUCCESS
    echo Output: %CD%\%OUT%\PDX.dll
) else (
    echo BUILD_FAILED with code %BUILD_RESULT%
)

endlocal
exit /b %BUILD_RESULT%
