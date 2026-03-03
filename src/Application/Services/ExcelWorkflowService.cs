using System;
using System.IO;
using PDX_CSharp.Core.Interfaces;
using PDX_CSharp.Core.Models;
using PDX_CSharp.Infrastructure.Excel;

namespace PDX_CSharp.Application.Services
{
    /// <summary>
    /// Excel 路径工作流服务（路径 A 编排器）。
    ///
    /// 完整流程：
    ///   GenerateTemplate()  → AI分析 → 生成 CSV 模板 → 用户填写
    ///   ImportAndGenerate() → 导入 CSV → 计算 → 布局 → 渲染
    ///
    /// 本服务不含任何电气公式或坐标逻辑，仅负责阶段编排。
    /// </summary>
    public class ExcelWorkflowService
    {
        private readonly DiagramGenerationService _genService;
        private readonly IExcelTemplateBuilder    _builder;
        private readonly IExcelImporter           _importer;

        public ExcelWorkflowService(
            DiagramGenerationService genService,
            IExcelTemplateBuilder    builder  = null,
            IExcelImporter           importer = null)
        {
            if (genService == null) throw new ArgumentNullException("genService");
            _genService = genService;
            _builder    = builder  ?? new CsvExcelTemplateBuilder();
            _importer   = importer ?? new CsvExcelImporter();
        }

        // ── 公开 API ──────────────────────────────────────────────────────

        /// <summary>
        /// 步骤 A1：AI 分析 + 生成 Excel 模板文件。
        /// 操作人员打开此文件，填写必填列，保存。
        /// </summary>
        /// <param name="def">图样模板定义</param>
        /// <param name="outputCsvPath">生成的 CSV 路径（如 "D:\project\template.csv"）</param>
        /// <returns>AI 分析结果（供调用方展示字段信息）</returns>
        public AiAnalysisResult GenerateTemplate(TemplateDefinition def, string outputCsvPath)
        {
            if (def == null)             throw new ArgumentNullException("def");
            if (string.IsNullOrEmpty(outputCsvPath))
                throw new ArgumentNullException("outputCsvPath");

            // AI 分析图样结构
            AiAnalysisResult aiResult = _genService.Analyze(def);

            // 生成 CSV 模板
            _builder.GenerateTemplate(def, outputCsvPath);

            return aiResult;
        }

        /// <summary>
        /// 步骤 A2：导入用户填写后的 CSV，执行完整生成流程。
        /// </summary>
        /// <param name="csvPath">用户填写完毕的 CSV 路径</param>
        /// <param name="def">图样模板定义</param>
        /// <param name="renderer">CAD 渲染器</param>
        /// <returns>已完成计算的 DiagramModel（可供后续检查）</returns>
        public DiagramModel ImportAndGenerate(
            string          csvPath,
            TemplateDefinition def,
            ICadRenderer    renderer)
        {
            if (string.IsNullOrEmpty(csvPath)) throw new ArgumentNullException("csvPath");
            if (def      == null) throw new ArgumentNullException("def");
            if (renderer == null) throw new ArgumentNullException("renderer");

            // 导入 CSV → DiagramModel（计算字段为初始值 0）
            DiagramModel model = _importer.Import(csvPath, def);

            // 编排：计算 + 布局 + 渲染
            _genService.Generate(model, renderer);

            return model;
        }
    }
}
