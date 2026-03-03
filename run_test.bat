@echo off
chcp 437 >nul
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set FX=C:\Windows\Microsoft.NET\Framework64\v4.0.30319

REM Compile SelfTest.exe with non-AutoCAD platform source files directly.
REM Excluded: Drawing\*, Commands\*, Infrastructure\CAD\AutoCadRenderer.cs, Services\PdxService.cs
REM (All excluded files depend on AutoCAD SDK which is not needed for unit tests)

"%CSC%" /target:exe /out:SelfTest.exe ^
    /nostdlib /noconfig ^
    /r:"%FX%\mscorlib.dll" ^
    /r:"%FX%\System.dll" ^
    /r:"%FX%\System.Core.dll" ^
    src\Models\BreakerRule.cs ^
    src\Models\LoopData.cs ^
    src\Models\MainCircuitData.cs ^
    src\Models\TopologyModels.cs ^
    src\Rules\RuleEngine.cs ^
    src\Calculation\CalculationEngine.cs ^
    src\Calculation\TopologyBuilder.cs ^
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
    src\Infrastructure\CAD\ConsoleCadRenderer.cs ^
    src\Application\Services\DiagramGenerationService.cs ^
    src\Application\Services\ExcelWorkflowService.cs ^
    src\Application\Services\DirectEntryWorkflowService.cs ^
    SelfTest.cs > test_compile.txt 2>&1

if %ERRORLEVEL% equ 0 (
    echo [SELFTEST COMPILE OK]
    SelfTest.exe
) else (
    echo Compile failed - see test_compile.txt
    type test_compile.txt
)

