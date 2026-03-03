using System;
using PDX_CSharp.Core.Interfaces;
using PDX_CSharp.Core.Models;
using PDX_CSharp.Templates;

namespace PDX_CSharp.Application.Services
{
    /// <summary>
    /// 图样生成服务（平台主编排器）。
    ///
    /// 职责：将 AI分析 → 计算引擎 → 模板引擎 → CAD渲染 四个阶段串联。
    ///
    /// 数据流：
    ///   1. Analyze()      → AI分析 → AiAnalysisResult
    ///   2. Generate()     → ICalculationEngine.Calculate
    ///                     → IDiagramTemplate.BuildLayout
    ///                     → IDiagramTemplate.Render
    ///
    /// 本服务只做编排，不含业务逻辑，不含图形代码，不含 AutoCAD 引用。
    /// </summary>
    public class DiagramGenerationService
    {
        private readonly ITemplateAnalyzer  _analyzer;
        private readonly ICalculationEngine _calcEngine;
        private readonly TemplateRegistry   _registry;

        /// <summary>
        /// 构造函数（依赖注入入口）。
        /// </summary>
        /// <param name="analyzer">AI 分析器（MockAiAnalyzer 或 GptAnalyzer）</param>
        /// <param name="calcEngine">电气计算引擎</param>
        /// <param name="registry">模板注册表</param>
        public DiagramGenerationService(
            ITemplateAnalyzer  analyzer,
            ICalculationEngine calcEngine,
            TemplateRegistry   registry = null)
        {
            if (analyzer   == null) throw new ArgumentNullException("analyzer");
            if (calcEngine == null) throw new ArgumentNullException("calcEngine");
            _analyzer   = analyzer;
            _calcEngine = calcEngine;
            _registry   = registry ?? TemplateRegistry.Default;
        }

        // ── 公开 API ──────────────────────────────────────────────────────

        /// <summary>
        /// 步骤1：AI 分析图样结构。
        /// 返回字段定义，供后续生成 Excel 模板或初始化空模型使用。
        /// </summary>
        public AiAnalysisResult Analyze(TemplateDefinition def)
        {
            if (def == null) throw new ArgumentNullException("def");
            return _analyzer.Analyze(def);
        }

        /// <summary>
        /// 步骤2：完整生成流程。
        ///   a. 电气计算（填写所有 CalculatedField）
        ///   b. 布局计算（BuildLayout → DiagramTopology）
        ///   c. CAD 渲染（Render → ICadRenderer）
        /// </summary>
        /// <param name="model">已填写输入字段的图样数据模型</param>
        /// <param name="renderer">CAD 渲染器（AutoCadRenderer 或 ConsoleCadRenderer）</param>
        public void Generate(DiagramModel model, ICadRenderer renderer)
        {
            if (model    == null) throw new ArgumentNullException("model");
            if (renderer == null) throw new ArgumentNullException("renderer");

            // 1. 解析模板
            IDiagramTemplate template = ResolveTemplate(model);

            // 2. 电气计算（纯数值，不涉及任何坐标）
            _calcEngine.Calculate(model);

            // 3. 布局计算（坐标计算，不调用 CAD API）
            DiagramTopology topology = template.BuildLayout(model);

            // 4. CAD 渲染（按坐标输出图元，不做任何计算）
            template.Render(topology, renderer);
        }

        // ── 私有辅助 ─────────────────────────────────────────────────────

        private IDiagramTemplate ResolveTemplate(DiagramModel model)
        {
            string id = (model.Template != null)
                ? model.Template.TemplateId
                : string.Empty;

            IDiagramTemplate template = _registry.Resolve(id);

            if (template == null)
                throw new InvalidOperationException(string.Format(
                    "未找到 TemplateId='{0}' 的模板。请先在 TemplateRegistry 中注册。", id));

            return template;
        }
    }
}
