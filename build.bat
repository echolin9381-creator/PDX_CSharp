@echo off
chcp 65001 >nul
REM ================================================================
REM  PDX ?????????????????MSBuild/NuGet??
REM  ??? dotnet SDK ?????Roslyn csc.dll ??????
REM  ???: bin\Release\PDX.dll
REM ================================================================
setlocal

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set ACAD_DIR=D:\app\cad18\AutoCAD 2018
set OUT_DIR=bin\Release
set NETFX_DIR=C:\Windows\Microsoft.NET\Framework64\v4.0.30319

if not exist "%OUT_DIR%" mkdir "%OUT_DIR%"

echo [INFO] ??? csc.exe: "%CSC%"
echo [INFO] AutoCAD SDK: "%ACAD_DIR%"
echo [INFO] ???????..

"%CSC%" ^
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
  src\Models\TopologyModels.cs ^
  src\Rules\RuleEngine.cs ^
  src\Calculation\CalculationEngine.cs ^
  src\Calculation\TopologyBuilder.cs ^
  src\Drawing\LayerHelper.cs ^
  src\Drawing\DrawingEngine.cs ^
  src\Services\PdxService.cs ^
  src\Core\Models\LayoutConfig.cs ^
  src\Core\Models\DiagramModel.cs ^
  src\Core\Models\TemplateDefinition.cs ^
  src\Core\Models\TopologyModel.cs ^
  src\Core\Interfaces\ICoreInterfaces.cs ^
  src\Core\Calculation\ElectricalCalculationEngine.cs ^
  src\Templates\VerticalBusTemplate.cs ^
  src\Templates\DualBusTemplate.cs ^
  src\Templates\TemplateRegistry.cs ^
  src\Infrastructure\AI\MockAiAnalyzer.cs ^
  src\Infrastructure\AI\GptAnalyzer.cs ^
  src\Infrastructure\Excel\CsvExcelTemplateBuilder.cs ^
  src\Infrastructure\Excel\CsvExcelImporter.cs ^
  src\Infrastructure\CAD\AutoCadRenderer.cs ^
  src\Infrastructure\CAD\ConsoleCadRenderer.cs ^
  src\Application\Services\DiagramGenerationService.cs ^
  src\Application\Services\ExcelWorkflowService.cs ^
  src\Application\Services\DirectEntryWorkflowService.cs ^
  src\Commands\PdxCommands.cs

if %ERRORLEVEL% equ 0 (
    REM ?????????????????
    copy /y "resources\pdx_rules.json" "%OUT_DIR%\pdx_rules.json" >nul
    echo.
    echo ============================================================
    echo  [???] PDX.dll ??????
    echo  ??????: %OUT_DIR%\
    echo    - PDX.dll
    echo    - pdx_rules.json
    echo.
    echo  AutoCAD ??????:
    echo  1. ??AutoCAD ???????? NETLOAD
    echo  2. ??? %CD%\%OUT_DIR%\PDX.dll
    echo  3. ??????: PDX
    echo ============================================================
) else (
    echo.
    echo ============================================================
    echo  [???] ????????????????????????
    echo ============================================================
)

endlocal
pause
