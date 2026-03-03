using PDX_CSharp.Core.Models;

namespace PDX_CSharp.Core.Interfaces
{
    /// <summary>
    /// 图样模板接口（模板引擎核心契约）。
    ///
    /// BuildLayout 与 Render 严格两阶段：
    ///   1. BuildLayout — 读取 DiagramModel，产出 DiagramTopology（纯坐标计算）
    ///   2. Render      — 读取 DiagramTopology，调用 ICadRenderer 输出图形
    ///
    /// 禁止：在 BuildLayout 中调用 ICadRenderer
    /// 禁止：在 Render 中做任何坐标计算或电气计算
    /// </summary>
    public interface IDiagramTemplate
    {
        /// <summary>模板的唯一标识符（与 TemplateDefinition.TemplateId 对应）</summary>
        string TemplateId { get; }

        /// <summary>用户可见名称</summary>
        string DisplayName { get; }

        /// <summary>
        /// 第一阶段：布局计算。
        /// 读取 DiagramModel 中的 BranchCircuit 数据和 LayoutConfig 参数，
        /// 计算所有图元的绝对坐标，返回 DiagramTopology（纯数据，不含 AutoCAD 对象）。
        /// </summary>
        /// <param name="model">已计算完毕的图样数据模型（CalculationEngine 已填写计算字段）</param>
        /// <returns>完整坐标拓扑模型</returns>
        DiagramTopology BuildLayout(DiagramModel model);

        /// <summary>
        /// 第二阶段：渲染输出。
        /// 读取 BuildLayout 产出的 DiagramTopology，逐图元调用 ICadRenderer。
        /// 此阶段不得做任何坐标计算。
        /// </summary>
        /// <param name="topology">第一阶段产出的拓扑模型</param>
        /// <param name="renderer">CAD 渲染器（AutoCadRenderer 或 ConsoleCadRenderer）</param>
        void Render(DiagramTopology topology, ICadRenderer renderer);
    }

    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 计算引擎接口（纯电气参数计算契约）。
    ///
    /// 职责范围：
    ///   ✔ 单/三相电流计算
    ///   ✔ 总功率统计
    ///   ✔ 负载率计算
    ///   ✔ 断路器 / 电缆 / 穿管自动选型（通过 RuleEngine）
    ///
    /// 禁止：母线长度计算（归 IDiagramTemplate.BuildLayout）
    /// 禁止：引用任何 LayoutConfig / TopologyModel 字段
    /// 禁止：引用 AutoCAD / Excel / AI 任何 Infrastructure
    /// </summary>
    public interface ICalculationEngine
    {
        /// <summary>
        /// 对 DiagramModel 中所有 BranchCircuit 执行电气计算，
        /// 就地填写所有 IsCalculated=true 的字段。
        /// 同时计算并填写 MainSwitch 数据（如 SupportsMainSwitch）。
        /// </summary>
        void Calculate(DiagramModel model);
    }

    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// CAD 渲染器接口（绘图输出契约）。
    ///
    /// 仅定义基础图元操作。上层模板通过此接口输出图形，
    /// 与具体 CAD SDK 完全解耦（可替换为控制台渲染器用于测试）。
    /// </summary>
    public interface ICadRenderer
    {
        /// <summary>开始绘图会话（如 AutoCAD 中开启 Transaction）</summary>
        void BeginDraw();

        /// <summary>结束绘图会话（如 AutoCAD 中提交 Transaction）</summary>
        void EndDraw();

        /// <summary>绘制直线</summary>
        void DrawLine(double x1, double y1, double x2, double y2, bool thick = false);

        /// <summary>绘制文字</summary>
        void DrawText(double x, double y, string text, double height = 0);

        /// <summary>绘制命名符号（如 "breaker"，具体由实现者解释）</summary>
        void DrawSymbol(string symbolType, double cx, double cy);
    }

    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Excel 模板构建器接口（Excel Infrastructure 层契约）。
    /// </summary>
    public interface IExcelTemplateBuilder
    {
        /// <summary>
        /// 根据 TemplateDefinition 生成 Excel（CSV）模板文件。
        /// 计算字段标注为只读区域，必填字段留空供用户填写。
        /// </summary>
        /// <param name="def">模板定义（含字段描述符）</param>
        /// <param name="outputPath">输出文件路径（含扩展名）</param>
        void GenerateTemplate(TemplateDefinition def, string outputPath);
    }

    /// <summary>
    /// Excel 数据导入器接口（Excel Infrastructure 层契约）。
    /// </summary>
    public interface IExcelImporter
    {
        /// <summary>
        /// 从 Excel（CSV）文件读取数据，映射到 DiagramModel。
        /// 计算字段留空，由后续 ICalculationEngine.Calculate 填写。
        /// </summary>
        /// <param name="filePath">Excel / CSV 文件路径</param>
        /// <param name="def">模板定义（用于字段名映射）</param>
        /// <returns>含分支回路数据的图样模型（未计算）</returns>
        DiagramModel Import(string filePath, TemplateDefinition def);
    }

    // ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// AI 图样结构分析器接口（AI Infrastructure 层契约）。
    /// </summary>
    public interface ITemplateAnalyzer
    {
        /// <summary>
        /// 分析图样定义，返回字段结构描述。
        /// MockAiAnalyzer 离线实现；GptAnalyzer 联网实现。
        /// </summary>
        /// <param name="def">待分析的图样模板元数据</param>
        /// <returns>AI 解析结果</returns>
        AiAnalysisResult Analyze(TemplateDefinition def);
    }
}
