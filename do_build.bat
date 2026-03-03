@echo off
chcp 437 >nul
setlocal

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set ACAD=D:\app\cad18\AutoCAD 2018
set OUT=bin\Release
set FX=C:\Windows\Microsoft.NET\Framework64\v4.0.30319

if not exist "%OUT%" mkdir "%OUT%"

"%CSC%" ^
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
  src\Commands\PdxCommands.cs ^
  > build_log.txt 2>&1

set BUILD_RESULT=%ERRORLEVEL%

if %BUILD_RESULT% equ 0 (
    copy /y "resources\pdx_rules.json" "%OUT%\pdx_rules.json" >nul
    echo BUILD_SUCCESS
    echo Output: %CD%\%OUT%\PDX.dll
) else (
    echo BUILD_FAILED with code %BUILD_RESULT%
    echo See build_log.txt for details
    type build_log.txt
)

endlocal
exit /b %BUILD_RESULT%
