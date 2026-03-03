using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using PDX_CSharp.Application.Services;
using PDX_CSharp.Core.Models;
using PDX_CSharp.Infrastructure.AI;
using PDX_CSharp.Infrastructure.CAD;
using PDX_CSharp.Rules;
using PDX_CSharp.Core.Calculation;
using PDX_CSharp.Services;
using PDX_CSharp.Templates;
// Alias to disambiguate: 'PDX_CSharp.Application' namespace vs AutoCAD Application class
using AcadApp = Autodesk.AutoCAD.ApplicationServices.Application;

// 注册 AutoCAD 命令
[assembly: CommandClass(typeof(PDX_CSharp.Commands.PdxCommands))]

namespace PDX_CSharp.Commands
{
    /// <summary>
    /// PDX 命令入口层（平台 V2）。
    ///
    /// 已有命令（保持不变）：
    ///   PDX_CS    — 旧版配电箱系统图（兼容存量用户）
    ///
    /// 新增平台命令：
    ///   PDXNEW    — 直接录入路径（路径 B）：AI分析 + CAD内录入 + 自动计算 + 生成图样
    ///   PDXEXCEL  — Excel路径A第一步：AI分析 + 生成 CSV 模板
    ///   PDXIMPORT — Excel路径A第二步：导入CSV + 计算 + 生成 CAD 图样
    /// </summary>
    public class PdxCommands
    {
        // ═══════════════════════════════════════════════════════════════
        // 旧命令（原封不动保留，0 破坏性变更）
        // ═══════════════════════════════════════════════════════════════

        /// <summary>PDX_CS — 旧版配电箱系统图（兼容命令）</summary>
        [CommandMethod("PDX_CS", CommandFlags.Modal)]
        public void RunPDX()
        {
            new PdxService().Execute();
        }

        // ═══════════════════════════════════════════════════════════════
        // 新平台命令
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// PDXNEW — 平台直接录入路径（路径 B）。
        /// 流程：选择图样模板 → AI分析 → 输入回路数 → 逐路录入功率/名称 → 自动计算 → 生成 CAD 图样。
        /// </summary>
        [CommandMethod("PDXNEW", CommandFlags.Modal)]
        public void RunPdxNew()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor   ed  = doc.Editor;
            Database db  = doc.Database;

            ed.WriteMessage("\n=== PDX 平台 V2 — 直接录入路径 ===\n");
            try
            {
                // 1. 选择图样模板
                TemplateDefinition def = PromptTemplateSelection(ed);
                if (def == null) return;

                // 2. AI 分析图样结构（使用离线 MockAiAnalyzer）
                var analyzer   = new MockAiAnalyzer();
                var aiResult   = analyzer.Analyze(def);
                ed.WriteMessage(string.Format(
                    "\n[AI] 必填字段: {0}　计算字段: {1}",
                    string.Join(",", aiResult.RequiredFields.ToArray()),
                    string.Join(",", aiResult.CalculatedFields.ToArray())));

                // 3. 输入回路数
                var rn = ed.GetInteger(new PromptIntegerOptions("\n出线回路数 (1-60):")
                    { LowerLimit = 1, UpperLimit = 60, AllowNone = false });
                if (rn.Status != PromptStatus.OK) return;
                int n = rn.Value;

                // 4. 构建服务
                var ruleEngine  = new RuleEngine();
                var calcEngine  = new ElectricalCalculationEngine(ruleEngine);
                var genService  = new DiagramGenerationService(analyzer, calcEngine);
                var workflow    = new DirectEntryWorkflowService(genService);

                // 5. 创建骨架模型
                var rd = ed.GetDouble(new PromptDoubleOptions("\nKD 需求系数 (如 0.8):")
                    { AllowNone = false, AllowNegative = false, DefaultValue = 0.8 });
                if (rd.Status != PromptStatus.OK) return;
                DiagramModel model = workflow.CreateEmptyModel(def, n, rd.Value);

                // 6. 逐路录入
                for (int i = 0; i < n; i++)
                {
                    var rp = ed.GetDouble(new PromptDoubleOptions(
                        string.Format("\n回路 {0} — 单台功率 kW (0=备用):", i + 1))
                        { AllowNone = false, AllowNegative = false });
                    if (rp.Status != PromptStatus.OK) return;
                    model.Branches[i].UnitPower = rp.Value;

                    if (rp.Value > 0)
                    {
                        var rc = ed.GetInteger(new PromptIntegerOptions(
                            string.Format("\n回路 {0} — 台数 (回车=1):", i + 1))
                            { LowerLimit = 1, UpperLimit = 99, AllowNone = true, DefaultValue = 1 });
                        if (rc.Status == PromptStatus.OK) model.Branches[i].DeviceCount = rc.Value;
                    }

                    var rs = ed.GetString(new PromptStringOptions(
                        string.Format("\n回路 {0} — 设备名称 (回车跳过):", i + 1))
                        { AllowSpaces = true });
                    if (rs.Status == PromptStatus.OK)
                        model.Branches[i].DeviceName = rs.StringResult.Trim();
                }

                // 7. 渲染
                ed.WriteMessage("\n生成系统图...");
                var renderer = new AutoCadRenderer(db);
                workflow.Generate(model, renderer);

                ed.WriteMessage(string.Format(
                    "\n完成！{0} 路回路，总负荷 {1:F1} kW。执行 ZOOM Extents 查看。\n",
                    n, model.MainSwitch != null ? model.MainSwitch.TotalPower : 0));
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage(string.Format("\n[错误] {0}\n", ex.Message));
            }
        }

        /// <summary>
        /// PDXEXCEL — Excel路径A第一步：生成 CSV 数据模板。
        /// 流程：选择图样模板 → AI分析 → 生成 CSV 文件 → 提示用户填写后执行 PDXIMPORT。
        /// </summary>
        [CommandMethod("PDXEXCEL", CommandFlags.Modal)]
        public void RunPdxExcel()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor ed = doc.Editor;

            ed.WriteMessage("\n=== PDX 平台 V2 — 生成 Excel 模板 ===\n");
            try
            {
                TemplateDefinition def = PromptTemplateSelection(ed);
                if (def == null) return;

                // 输出路径
                var rp = ed.GetString(new PromptStringOptions("\nCSV 模板输出路径 (如 D:\\pdx_template.csv):")
                    { AllowSpaces = true });
                if (rp.Status != PromptStatus.OK) return;
                string csvPath = rp.StringResult.Trim();

                var analyzer = new MockAiAnalyzer();
                var ruleEngine  = new RuleEngine();
                var calcEngine  = new ElectricalCalculationEngine(ruleEngine);
                var genService  = new DiagramGenerationService(analyzer, calcEngine);
                var workflow    = new ExcelWorkflowService(genService);

                var aiResult = workflow.GenerateTemplate(def, csvPath);

                ed.WriteMessage(string.Format(
                    "\n[完成] CSV 模板已生成: {0}\n" +
                    "       必填列: {1}\n" +
                    "       自动计算列: {2}\n" +
                    "请用 Excel 填写后，执行 PDXIMPORT 导入生成图样。\n",
                    csvPath,
                    string.Join(",", aiResult.RequiredFields.ToArray()),
                    string.Join(",", aiResult.CalculatedFields.ToArray())));
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage(string.Format("\n[错误] {0}\n", ex.Message));
            }
        }

        /// <summary>
        /// PDXIMPORT — Excel路径A第二步：导入填写完毕的 CSV，计算并生成 CAD 图样。
        /// </summary>
        [CommandMethod("PDXIMPORT", CommandFlags.Modal)]
        public void RunPdxImport()
        {
            Document doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor   ed  = doc.Editor;
            Database db  = doc.Database;

            ed.WriteMessage("\n=== PDX 平台 V2 — 导入 CSV 生成图样 ===\n");
            try
            {
                TemplateDefinition def = PromptTemplateSelection(ed);
                if (def == null) return;

                var rp = ed.GetString(new PromptStringOptions("\nCSV 文件路径:")
                    { AllowSpaces = true });
                if (rp.Status != PromptStatus.OK) return;
                string csvPath = rp.StringResult.Trim();

                var analyzer   = new MockAiAnalyzer();
                var ruleEngine = new RuleEngine();
                var calcEngine = new ElectricalCalculationEngine(ruleEngine);
                var genService = new DiagramGenerationService(analyzer, calcEngine);
                var workflow   = new ExcelWorkflowService(genService);
                var renderer   = new AutoCadRenderer(db);

                ed.WriteMessage("\n计算并生成系统图...");
                DiagramModel model = workflow.ImportAndGenerate(csvPath, def, renderer);

                ed.WriteMessage(string.Format(
                    "\n完成！{0} 路回路已导入生成。执行 ZOOM Extents 查看。\n",
                    model.Branches.Count));
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage(string.Format("\n[错误] {0}\n", ex.Message));
            }
        }

        // ─── V2 预留命令 ───────────────────────────────────────────────────

        [CommandMethod("PDX_RECALC",  CommandFlags.Modal)]
        public void RunRecalc()
        {
            WriteMsg("PDX_RECALC");
        }

        [CommandMethod("PDX_EXPORT",  CommandFlags.Modal)]
        public void RunExport()
        {
            WriteMsg("PDX_EXPORT");
        }

        [CommandMethod("PDX_BALANCE", CommandFlags.Modal)]
        public void RunBalance()
        {
            WriteMsg("PDX_BALANCE");
        }

        [CommandMethod("PDX_EDIT",    CommandFlags.Modal)]
        public void RunEdit()
        {
            WriteMsg("PDX_EDIT");
        }

        // ── 私有辅助 ──────────────────────────────────────────────────────

        /// <summary>提示用户选择图样模板，返回对应 TemplateDefinition（取消返回 null）</summary>
        private static TemplateDefinition PromptTemplateSelection(Editor ed)
        {
            ed.WriteMessage(
                "\n图样模板选择：\n" +
                "  1 — 配电箱系统图（竖向母线）\n" +
                "  2 — 双母线系统图（存根）\n");

            var r = ed.GetInteger(new PromptIntegerOptions("\n请输入编号 [1/2]:")
                { LowerLimit = 1, UpperLimit = 2, AllowNone = false });
            if (r.Status != PromptStatus.OK) return null;

            switch (r.Value)
            {
                case 2:  return TemplateDefinition.DualBus();
                default: return TemplateDefinition.VerticalBus();
            }
        }

        private static void WriteMsg(string cmdName)
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc != null)
                doc.Editor.WriteMessage(string.Format(
                    "\n[{0}] 此命令在 V3 版本实现，敬请期待。\n", cmdName));
        }
    }
}
