using System.Collections.Generic;

namespace PDX_CSharp.Core.Models
{
    /// <summary>
    /// 平台拓扑模型——独立文件，不依赖任何模板类。
    /// BuildLayout 生成此模型，Render 读取此模型，两阶段严格分离。
    /// 禁止在此处引入 AutoCAD / AI / Excel 任何 Infrastructure 依赖。
    /// </summary>

    // ── 坐标基元 ──────────────────────────────────────────────────────────
    /// <summary>
    /// 二维坐标点（平台层版本，独立于 AutoCAD Geometry）。
    /// </summary>
    public class Point2D
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point2D(double x, double y) { X = x; Y = y; }
    }

    // ── 拓扑模型 ──────────────────────────────────────────────────────────
    /// <summary>
    /// 图样拓扑模型根对象。
    /// 由 IDiagramTemplate.BuildLayout 生成，由 IDiagramTemplate.Render 消费。
    /// 内含所有图元的绝对坐标，Render 仅按坐标调用 ICadRenderer，不再做任何计算。
    /// </summary>
    public class DiagramTopology
    {
        /// <summary>主母线拓扑</summary>
        public BusBarTopology MainBus { get; set; }

        /// <summary>主开关行拓扑（可为 null）</summary>
        public MainSwitchTopology MainSwitchRow { get; set; }

        /// <summary>所有出线支路拓扑（已排好绝对坐标）</summary>
        public List<BranchTopology> Branches { get; set; }

        public DiagramTopology()
        {
            Branches = new List<BranchTopology>();
        }
    }

    /// <summary>
    /// 主母线拓扑（含起止坐标，由 Template.BuildLayout 计算）。
    /// </summary>
    public class BusBarTopology
    {
        /// <summary>母线起点（通常为图样原点附近）</summary>
        public Point2D Start { get; set; }

        /// <summary>母线终点（由回路数量 + LayoutConfig 计算得出）</summary>
        public Point2D End { get; set; }
    }

    /// <summary>
    /// 主开关行拓扑（坐标 + 显示文字）。
    /// </summary>
    public class MainSwitchTopology
    {
        /// <summary>行 Y 坐标</summary>
        public double Y { get; set; }

        public string TotalPowerText { get; set; }
        public string BreakerText    { get; set; }
        public string CurrentText    { get; set; }
        public string CableText      { get; set; }
        public string ConduitText    { get; set; }
        public string DemandFactorText { get; set; }
    }

    /// <summary>
    /// 单条出线支路拓扑（所有绘图需要的坐标与文字已全部展开）。
    /// </summary>
    public class BranchTopology
    {
        /// <summary>支路在图中的绝对 Y 坐标</summary>
        public double Y { get; set; }

        // ── 线段坐标 ──────────────────────────────────────────────
        /// <summary>水平线左起点 X（从母线出发）</summary>
        public double LineStartX { get; set; }

        /// <summary>水平线右端点 X</summary>
        public double LineEndX { get; set; }

        /// <summary>断路器符号中心 X</summary>
        public double BreakerCenterX { get; set; }

        // ── 文字内容（固定列，已按 LayoutConfig 定好 X 坐标）──────
        public string LoopNoText   { get; set; }
        public string PhaseText    { get; set; }
        public string PowerText    { get; set; }
        public string BreakerText  { get; set; }
        public string CurrentText  { get; set; }
        public string CableText    { get; set; }
        public string PipeText     { get; set; }
        public string DeviceName   { get; set; }

        // ── 列 X 坐标（来自 LayoutConfig，Render 时直接使用）──────
        public double ColNoX      { get; set; }
        public double ColPhaseX   { get; set; }
        public double ColPowerX   { get; set; }
        public double ColBreakerX { get; set; }
        public double ColCurrentX { get; set; }
        public double ColCableX   { get; set; }
        public double ColPipeX    { get; set; }
        public double TextY       { get; set; }
    }
}
