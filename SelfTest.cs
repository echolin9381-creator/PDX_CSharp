// PDX Layout Self-Test (no AutoCAD dependency)
// Tests: column spacing, Y positions, load formatting, rule matching
using System;
using System.Collections.Generic;
using System.IO;

class SelfTest
{
    // ── Mirror of DrawingEngine constants ─────────────────────────
    const double BusbarEndX  = 200.0;
    const double LoopStartY  = -20.0;
    const double LoopYStep   = -12.0;
    const double BrkSymX     =  57.0;
    const double BrkSymHalf  =   4.0;
    const double BrkSymHW    =   3.5;
    const double ColNo       =   5.0;
    const double ColPhase    =  20.0;
    const double ColLoad     =  35.0;
    const double ColBreaker  =  65.0;
    const double ColCurrent  =  93.0;
    const double ColCable    = 118.0;
    const double ColConduit  = 148.0;
    const double TextH       =   2.5;
    const double CharW       =   1.5;  // estimated char width at H=2.5
    const double CircuitEndX = 185.0;

    static int pass = 0, fail = 0;

    static void Check(string name, bool ok, string detail = "")
    {
        if (ok) { pass++; Console.WriteLine("[PASS] " + name); }
        else    { fail++; Console.WriteLine("[FAIL] " + name + (detail != "" ? " | " + detail : "")); }
    }

    static void Main()
    {
        Console.WriteLine("=== PDX DrawingEngine Self-Test ===\n");

        // ── T1: Column spacing >= 12mm ─────────────────────────────
        double[] cols = { ColNo, ColPhase, ColLoad, ColBreaker, ColCurrent, ColCable, ColConduit };
        for (int i = 1; i < cols.Length; i++)
        {
            double gap = cols[i] - cols[i - 1];
            Check(string.Format("ColSpacing[{0}->{1}] >= 12mm", i, i+1), gap >= 12.0,
                string.Format("gap={0:F1}mm", gap));
        }

        // ── T2: Last column + max text within BusbarEndX ──────────
        double maxConduitW = "SC80".Length * CharW;
        Check("ColConduit right edge <= 200mm",
            ColConduit + maxConduitW <= BusbarEndX,
            string.Format("right={0:F1}mm", ColConduit + maxConduitW));

        // ── T3: Breaker symbol does not overlap load text ──────────
        double maxLoadW = "10x99.9kW".Length * CharW;
        Check("BreakerSym left edge > load text right edge",
            BrkSymX - BrkSymHW > ColLoad + maxLoadW,
            string.Format("brkLeft={0:F1} loadRight={0:F1}", BrkSymX - BrkSymHW, ColLoad + maxLoadW));

        // ── T4: Y = -20 - 12*index formula ────────────────────────
        int[] loopCounts = { 1, 5, 12, 30, 60 };
        foreach (int n in loopCounts)
        {
            double firstY = LoopStartY;
            double lastY  = LoopStartY + (n - 1) * LoopYStep;
            double capY   = LoopStartY + n * LoopYStep;

            Check(string.Format("Y-formula n={0}: firstY={1:F0}", n, firstY),
                firstY == -20.0);
            Check(string.Format("Y-formula n={0}: lastY correct", n),
                Math.Abs(lastY - (-20.0 - 12.0 * (n - 1))) < 0.001);
            Check(string.Format("Y-formula n={0}: 12mm rows no overlap", n),
                Math.Abs(LoopYStep) >= TextH * 2.0,
                string.Format("step={0}, textH={1}", Math.Abs(LoopYStep), TextH));
        }

        // ── T5: Load formatting ────────────────────────────────────
        Check("Single device: '1.5kW'",   FormatLoad(1.5, 1, 1.5) == "1.5kW");
        Check("Multi device: '3x2.5kW'",  FormatLoad(7.5, 3, 2.5) == "3x2.5kW");
        Check("Spare: '备用'",              FormatLoad(0,   1, 0)   == "备用");

        // ── T6: Phase cycling L1/L2/L3 ────────────────────────────
        string[] phases = { "L1", "L2", "L3" };
        Check("Phase[0]=L1", phases[0 % 3] == "L1");
        Check("Phase[3]=L1", phases[3 % 3] == "L1");
        Check("Phase[11]=L3", phases[11 % 3] == "L3");

        // ── T7: Rule matching (mirrors GetBuiltinRules) ────────────
        Check("C6   <= 6A",   MatchBreaker(5.0)   == "C6");
        Check("C10  <= 10A",  MatchBreaker(9.0)   == "C10");
        Check("C16  <= 16A",  MatchBreaker(14.0)  == "C16");
        Check("C63  <= 63A",  MatchBreaker(60.0)  == "C63");
        Check("C250 <= 250A", MatchBreaker(240.0) == "C250");
        // Overflow
        bool threw = false;
        try { MatchBreaker(500.0); } catch { threw = true; }
        Check("Rule overflow throws exception", threw);

        // ── T8: Circuit line does not exceed BusbarEndX ───────────
        Check("CircuitEndX <= BusbarEndX", CircuitEndX <= BusbarEndX);

        // ══════════════════════════════════════════════════════════════
        // ── T-Platform 组：平台层架构测试（无 AutoCAD 依赖）──────────
        // ══════════════════════════════════════════════════════════════
        Console.WriteLine("\n--- Platform Layer Tests ---");

        // T-P1: ElectricalCalculationEngine 单相电流公式
        // 1 kW / (220V × 0.85) = 5.347A
        {
            double expectedA = (1.0 * 1000.0) / (220.0 * 0.85);
            double actualA   = CalcSinglePhase(1.0);
            Check("T-P1: SinglePhase 1kW ≈ 5.35A",
                Math.Abs(actualA - expectedA) < 0.01,
                string.Format("expected={0:F4} actual={1:F4}", expectedA, actualA));
        }

        // T-P2: 母线长度公式由 Template 负责（TopOffset + BranchSpacing × N）
        {
            double topOffset = 20.0, spacing = 12.0;
            int    n         = 5;
            double busLen    = topOffset + spacing * n + spacing * 0.5;
            Check("T-P2: BusLen formula (topOffset+spacing*5+ext) = 86.0",
                Math.Abs(busLen - 86.0) < 0.001,
                string.Format("busLen={0:F1}", busLen));
        }

        // T-P3: TemplateRegistry 能解析 "vertical_bus" 返回非空
        {
            bool resolved = TemplateRegistryCanResolve("vertical_bus");
            Check("T-P3: TemplateRegistry resolves 'vertical_bus'", resolved);
        }

        // T-P4: MockAiAnalyzer 对 vertical_bus 返回 RequiredFields 含 "Phase"
        {
            bool hasPhase = MockAiHasRequiredField("vertical_bus", "Phase");
            Check("T-P4: MockAI vertical_bus RequiredFields contains 'Phase'", hasPhase);
        }

        // T-P5: CsvExcelTemplateBuilder 生成文件含表头行（至少包含"功率"列）
        {
            string tmpPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "pdx_selftest_template.csv");
            bool ok = TestCsvTemplateGeneration(tmpPath);
            Check("T-P5: CsvExcelTemplateBuilder generates CSV with header", ok);
            try { if (System.IO.File.Exists(tmpPath)) System.IO.File.Delete(tmpPath); } catch { }
        }

        // T-P6: CsvExcelImporter 导入自生成的 CSV → BranchCount 正确
        {
            string tmpPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "pdx_selftest_import.csv");
            int branchCount = TestCsvImport(tmpPath, 3);
            Check("T-P6: CsvExcelImporter imports 3 data rows correctly",
                branchCount == 3,
                string.Format("branchCount={0}", branchCount));
            try { if (System.IO.File.Exists(tmpPath)) System.IO.File.Delete(tmpPath); } catch { }
        }

        // T-P7: ConsoleCadRenderer DrawLine/DrawText/DrawSymbol 不抛异常
        {
            bool noException = TestConsoleCadRenderer();
            Check("T-P7: ConsoleCadRenderer draws without exception", noException);
        }

        // T-P8: 完整 Excel 路径 A 端到端流程（MockAI + CSV + ConsoleCadRenderer）
        {
            string tmpCsv = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "pdx_selftest_e2e_a.csv");
            bool ok = TestEndToEndPathA(tmpCsv);
            Check("T-P8: End-to-end Path-A (Excel) completes without exception", ok);
            try { if (System.IO.File.Exists(tmpCsv)) System.IO.File.Delete(tmpCsv); } catch { }
        }

        // T-P9: 完整直接录入路径 B 端到端流程（MockAI + Console renderer）
        {
            bool ok = TestEndToEndPathB();
            Check("T-P9: End-to-end Path-B (Direct Entry) completes without exception", ok);
        }

        // T-P10: MockAI → DualBusTemplate → BuildLayout + Render 不抛异常
        {
            bool ok = TestDualBusTemplate();
            Check("T-P10: DualBusTemplate BuildLayout+Render does not throw", ok);
        }

        // ── Summary ───────────────────────────────────────────────
        Console.WriteLine(string.Format(
            "\n{0} passed, {1} failed out of {2} total.",
            pass, fail, pass + fail));

        if (fail > 0) Environment.Exit(1);
    }

    static string FormatLoad(double totalKW, int count, double unitKW)
    {
        if (totalKW <= 0) return "备用";
        if (count > 1)    return string.Format("{0}x{1:F1}kW", count, unitKW);
        return string.Format("{0:F1}kW", totalKW);
    }

    // Mirrors RuleEngine.GetBuiltinRules + MatchBreaker
    static string MatchBreaker(double current)
    {
        var rules = new[]
        {
            new {L=6.0,   M="C6"},
            new {L=10.0,  M="C10"},
            new {L=16.0,  M="C16"},
            new {L=20.0,  M="C20"},
            new {L=25.0,  M="C25"},
            new {L=32.0,  M="C32"},
            new {L=40.0,  M="C40"},
            new {L=50.0,  M="C50"},
            new {L=63.0,  M="C63"},
            new {L=100.0, M="C100"},
            new {L=160.0, M="C160"},
            new {L=250.0, M="C250"},
        };
        foreach (var r in rules)
            if (r.L >= current) return r.M;
        throw new Exception("超出规则上限");
    }

    // ══════════════════════════════════════════════════════════════════
    // 平台测试辅助方法（T-P1 ~ T-P10）
    // ══════════════════════════════════════════════════════════════════

    // T-P1 辅助: 镜像 ElectricalCalculationEngine.CalcSinglePhase（无 AutoCAD 依赖）
    static double CalcSinglePhase(double kW, double cosPhi = 0.85)
    {
        if (kW <= 0) return 0;
        return (kW * 1000.0) / (220.0 * cosPhi);
    }

    // T-P3 辅助: 测试 TemplateRegistry 解析
    static bool TemplateRegistryCanResolve(string templateId)
    {
        try
        {
            var reg = PDX_CSharp.Templates.TemplateRegistry.Default;
            var t   = reg.Resolve(templateId);
            return t != null && t.TemplateId == templateId;
        }
        catch { return false; }
    }

    // T-P4 辅助: 测试 MockAiAnalyzer 返回字段
    static bool MockAiHasRequiredField(string templateId, string fieldName)
    {
        try
        {
            var def      = templateId == "dual_bus"
                           ? PDX_CSharp.Core.Models.TemplateDefinition.DualBus()
                           : PDX_CSharp.Core.Models.TemplateDefinition.VerticalBus();
            var analyzer = new PDX_CSharp.Infrastructure.AI.MockAiAnalyzer();
            var result   = analyzer.Analyze(def);
            return result.RequiredFields.Contains(fieldName);
        }
        catch { return false; }
    }

    // T-P5 辅助: 生成 CSV 模板并检查表头
    static bool TestCsvTemplateGeneration(string outputPath)
    {
        try
        {
            var def     = PDX_CSharp.Core.Models.TemplateDefinition.VerticalBus();
            var builder = new PDX_CSharp.Infrastructure.Excel.CsvExcelTemplateBuilder();
            builder.GenerateTemplate(def, outputPath);
            if (!System.IO.File.Exists(outputPath)) return false;
            string content = System.IO.File.ReadAllText(outputPath, System.Text.Encoding.UTF8);
            return content.Contains("功率") && content.Contains("[INPUT-请填写]");
        }
        catch (Exception ex)
        {
            Console.WriteLine("  T-P5 exception: " + ex.Message);
            return false;
        }
    }

    // T-P6 辅助: 写3行数据到 CSV，导回验证 BranchCount
    static int TestCsvImport(string csvPath, int rowCount)
    {
        try
        {
            var def     = PDX_CSharp.Core.Models.TemplateDefinition.VerticalBus();
            var builder = new PDX_CSharp.Infrastructure.Excel.CsvExcelTemplateBuilder();
            builder.MaxDataRows = 0; // 不预留空行
            builder.GenerateTemplate(def, csvPath);

            // 追加指定数量的数据行
            var lines = new System.Text.StringBuilder();
            lines.AppendLine("L1,C16,1.5,1,照明1,2.5mm²,SC20,,");
            lines.AppendLine("L2,C16,1.5,1,照明2,2.5mm²,SC20,,");
            lines.AppendLine("L3,C10,0.0,1,备用,,,," );
            System.IO.File.AppendAllText(csvPath, lines.ToString(), System.Text.Encoding.UTF8);

            var importer = new PDX_CSharp.Infrastructure.Excel.CsvExcelImporter();
            var model    = importer.Import(csvPath, def);
            return model.Branches.Count;
        }
        catch (Exception ex)
        {
            Console.WriteLine("  T-P6 exception: " + ex.Message);
            return -1;
        }
    }

    // T-P7 辅助: ConsoleCadRenderer 基本调用
    static bool TestConsoleCadRenderer()
    {
        try
        {
            var r = new PDX_CSharp.Infrastructure.CAD.ConsoleCadRenderer { Verbose = false };
            r.BeginDraw();
            r.DrawLine(0, 0, 100, 100);
            r.DrawText(10, 10, "TEST");
            r.DrawSymbol("breaker", 50, 50);
            r.EndDraw();
            return r.LineCount == 1 && r.TextCount == 1 && r.SymbolCount == 1;
        }
        catch { return false; }
    }

    // T-P8 辅助: Excel 路径 A 端到端
    static bool TestEndToEndPathA(string csvPath)
    {
        try
        {
            var def      = PDX_CSharp.Core.Models.TemplateDefinition.VerticalBus();
            var analyzer = new PDX_CSharp.Infrastructure.AI.MockAiAnalyzer();
            var rules    = new PDX_CSharp.Rules.RuleEngine();
            var calc     = new PDX_CSharp.Core.Calculation.ElectricalCalculationEngine(rules);
            var gen      = new PDX_CSharp.Application.Services.DiagramGenerationService(analyzer, calc);
            var workflow = new PDX_CSharp.Application.Services.ExcelWorkflowService(gen);

            // Step A1: 生成模板
            workflow.GenerateTemplate(def, csvPath);

            // 追加3行数据
            System.IO.File.AppendAllText(csvPath,
                "L1,C16,2.0,1,空调A,2.5mm²,SC20,,\r\n" +
                "L2,C10,1.0,1,照明,1.5mm²,SC15,,\r\n" +
                "L3,,0,1,备用,,,\r\n",
                System.Text.Encoding.UTF8);

            // Step A2: 导入 + 生成
            var renderer = new PDX_CSharp.Infrastructure.CAD.ConsoleCadRenderer { Verbose = false };
            var model    = workflow.ImportAndGenerate(csvPath, def, renderer);
            return model.Branches.Count >= 2 && renderer.LineCount > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("  T-P8 exception: " + ex.Message);
            return false;
        }
    }

    // T-P9 辅助: 直接录入路径 B 端到端
    static bool TestEndToEndPathB()
    {
        try
        {
            var def      = PDX_CSharp.Core.Models.TemplateDefinition.VerticalBus();
            var analyzer = new PDX_CSharp.Infrastructure.AI.MockAiAnalyzer();
            var rules    = new PDX_CSharp.Rules.RuleEngine();
            var calc     = new PDX_CSharp.Core.Calculation.ElectricalCalculationEngine(rules);
            var gen      = new PDX_CSharp.Application.Services.DiagramGenerationService(analyzer, calc);
            var workflow = new PDX_CSharp.Application.Services.DirectEntryWorkflowService(gen);

            var powers   = new System.Collections.Generic.List<double> { 2.0, 1.5, 0.0, 3.0 };
            var names    = new System.Collections.Generic.List<string>  { "空调A", "照明", "备用", "插座" };
            var renderer = new PDX_CSharp.Infrastructure.CAD.ConsoleCadRenderer { Verbose = false };
            var model    = workflow.QuickGenerate(def, powers, names, renderer, 0.8);

            // 验证：计算字段已填写，母线有线段输出
            bool calcOk = model.Branches[0].Current > 0 && model.MainSwitch != null && model.MainSwitch.Current > 0;
            return calcOk && renderer.LineCount > 0 && renderer.SymbolCount > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("  T-P9 exception: " + ex.Message);
            return false;
        }
    }

    // T-P10 辅助: DualBusTemplate BuildLayout+Render 不抛异常
    static bool TestDualBusTemplate()
    {
        try
        {
            var def      = PDX_CSharp.Core.Models.TemplateDefinition.DualBus();
            var analyzer = new PDX_CSharp.Infrastructure.AI.MockAiAnalyzer();
            var rules    = new PDX_CSharp.Rules.RuleEngine();
            var calc     = new PDX_CSharp.Core.Calculation.ElectricalCalculationEngine(rules);
            var gen      = new PDX_CSharp.Application.Services.DiagramGenerationService(analyzer, calc);
            var workflow = new PDX_CSharp.Application.Services.DirectEntryWorkflowService(gen);

            var powers   = new System.Collections.Generic.List<double> { 2.0, 1.5 };
            var renderer = new PDX_CSharp.Infrastructure.CAD.ConsoleCadRenderer { Verbose = false };
            workflow.QuickGenerate(def, powers, null, renderer);
            return true; // 不抛异常即通过
        }
        catch (Exception ex)
        {
            Console.WriteLine("  T-P10 exception: " + ex.Message);
            return false;
        }
    }
}
