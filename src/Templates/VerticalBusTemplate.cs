using System;
using PDX_CSharp.Core.Interfaces;
using PDX_CSharp.Core.Models;

namespace PDX_CSharp.Templates
{
    /// <summary>
    /// 竖向单母线图样模板（完整实现）。
    ///
    /// 图样结构：
    ///   ● 单条贯通竖向母线（左侧）
    ///   ● 主开关行（母线顶部）
    ///   ● 各支路从母线水平引出（向右）
    ///   ● 固定列排版（编号 / 相别 / 功率 / 断路器 / 电流 / 电缆 / 穿管）
    ///
    /// 严格两阶段：
    ///   BuildLayout → 坐标计算 → DiagramTopology
    ///   Render      → 按拓扑调用 ICadRenderer → 输出图形
    ///
    /// 禁止：在 Render 阶段做任何算术计算
    /// 禁止：在 BuildLayout 阶段调用 ICadRenderer
    /// 禁止：在本类中做任何电气计算（电流、断路器选型等）
    /// </summary>
    public class VerticalBusTemplate : IDiagramTemplate
    {
        // ── IDiagramTemplate 元数据 ──────────────────────────────────────
        public string TemplateId   { get { return "vertical_bus"; } }
        public string DisplayName  { get { return "配电箱系统图（竖向母线）"; } }

        // ────────────────────────────────────────────────────────────────
        // 第一阶段：BuildLayout
        // 职责：读取 DiagramModel + LayoutConfig，计算所有图元坐标，
        //       产出 DiagramTopology（纯数据，不含任何 CAD 对象）。
        // ────────────────────────────────────────────────────────────────
        public DiagramTopology BuildLayout(DiagramModel model)
        {
            if (model == null)
                throw new ArgumentNullException("model");
            if (model.Branches == null || model.Branches.Count == 0)
                throw new InvalidOperationException("DiagramModel.Branches 不能为空。");

            LayoutConfig cfg  = model.Layout ?? LayoutConfig.Default;
            int          n    = model.Branches.Count;

            // ── 母线长度计算（由 Template 负责，不在 CalculationEngine 中）──
            // 公式：Start(0,0) → End(0, -(TopOffset + BranchSpacing * Count + Extension))
            double busEndY = -(cfg.TopOffset
                               + cfg.BranchSpacing * n
                               + cfg.BranchSpacing * cfg.BusBarBottomExtensionFactor);

            var topology = new DiagramTopology
            {
                MainBus = new BusBarTopology
                {
                    Start = new Point2D(cfg.BusX, 0),
                    End   = new Point2D(cfg.BusX, busEndY)
                }
            };

            // ── 主开关行（如存在）───────────────────────────────────────
            if (model.MainSwitch != null)
            {
                var sw = model.MainSwitch;
                topology.MainSwitchRow = new MainSwitchTopology
                {
                    Y                = cfg.MainBreakerY,
                    TotalPowerText   = string.Format("{0:F1}kW", sw.TotalPower),
                    BreakerText      = sw.BreakerModel ?? "",
                    CurrentText      = string.Format("{0:F1}A",  sw.Current),
                    CableText        = sw.Cable    ?? "",
                    ConduitText      = sw.Conduit  ?? "",
                    DemandFactorText = string.Format("KD={0:F2}", sw.DemandFactor)
                };
            }

            // ── 逐支路构建拓扑 ───────────────────────────────────────────
            for (int i = 0; i < n; i++)
            {
                BranchCircuit branch = model.Branches[i];
                double y    = -(cfg.TopOffset + i * cfg.BranchSpacing);
                double textY = y + cfg.TextUpOffset;

                var bt = new BranchTopology
                {
                    Y              = y,
                    TextY          = textY,

                    // 线段坐标
                    LineStartX     = cfg.BusX,
                    LineEndX       = cfg.BranchLineLength,
                    BreakerCenterX = cfg.BreakerSymbolX,

                    // 固定列 X 坐标（来自 LayoutConfig）
                    ColNoX         = cfg.ColNo,
                    ColPhaseX      = cfg.ColPhase,
                    ColPowerX      = cfg.ColPower,
                    ColBreakerX    = cfg.ColBreaker,
                    ColCurrentX    = cfg.ColCurrent,
                    ColCableX      = cfg.ColCable,
                    ColPipeX       = cfg.ColPipe,

                    // 文字内容（在此阶段一次性展开，Render 直接使用）
                    LoopNoText     = branch.Index.ToString("D2"),
                    PhaseText      = branch.Phase       ?? "",
                    PowerText      = branch.FormatPower(),
                    BreakerText    = branch.BreakerModel ?? "",
                    CurrentText    = string.Format("{0:F1}A", branch.Current),
                    CableText      = branch.Cable       ?? "",
                    PipeText       = branch.Conduit     ?? "",
                    DeviceName     = branch.DeviceName  ?? ""
                };

                topology.Branches.Add(bt);
            }

            return topology;
        }

        // ────────────────────────────────────────────────────────────────
        // 第二阶段：Render
        // 职责：按照 DiagramTopology 中已计算好的坐标，
        //       逐图元调用 ICadRenderer。此阶段不做任何算术。
        // ────────────────────────────────────────────────────────────────
        public void Render(DiagramTopology topology, ICadRenderer renderer)
        {
            if (topology  == null) throw new ArgumentNullException("topology");
            if (renderer  == null) throw new ArgumentNullException("renderer");

            renderer.BeginDraw();

            // 1. 绘制主母线
            RenderMainBus(topology.MainBus, renderer);

            // 2. 绘制主开关行（如存在）
            if (topology.MainSwitchRow != null)
                RenderMainSwitchRow(topology.MainSwitchRow, renderer);

            // 3. 逐支路绘制
            foreach (var branch in topology.Branches)
                RenderBranch(branch, renderer);

            renderer.EndDraw();
        }

        // ── 私有 Render 辅助（每个方法只调用 renderer，不含算术）──────────

        private static void RenderMainBus(BusBarTopology bus, ICadRenderer r)
        {
            r.DrawLine(bus.Start.X, bus.Start.Y, bus.End.X, bus.End.Y, thick: true);
        }

        private static void RenderMainSwitchRow(MainSwitchTopology sw, ICadRenderer r)
        {
            double y = sw.Y;
            r.DrawSymbol("breaker", 0, y);
            r.DrawText(0,    y, sw.TotalPowerText);
            r.DrawText(0,    y, sw.BreakerText);
            r.DrawText(0,    y, sw.CurrentText);
            r.DrawText(0,    y, sw.CableText);
            r.DrawText(0,    y, sw.ConduitText);
            r.DrawText(0, y - 4, sw.DemandFactorText);
        }

        private static void RenderBranch(BranchTopology b, ICadRenderer r)
        {
            // 水平线（左段：母线 → 断路器左）
            r.DrawLine(b.LineStartX, b.Y, b.BreakerCenterX - 4.5, b.Y);

            // 断路器符号
            r.DrawSymbol("breaker", b.BreakerCenterX, b.Y);

            // 水平线（右段：断路器右 → 回路末端）
            r.DrawLine(b.BreakerCenterX + 4.5, b.Y, b.LineEndX, b.Y);

            // 固定列文字（Render 直接读取 BranchTopology 中已展开的坐标和文字）
            r.DrawText(b.ColNoX,      b.TextY, b.LoopNoText);
            r.DrawText(b.ColPhaseX,   b.TextY, b.PhaseText);
            r.DrawText(b.ColPowerX,   b.TextY, b.PowerText);
            r.DrawText(b.ColBreakerX, b.TextY, b.BreakerText);
            r.DrawText(b.ColCurrentX, b.TextY, b.CurrentText);
            r.DrawText(b.ColCableX,   b.TextY, b.CableText);
            r.DrawText(b.ColPipeX,    b.TextY, b.PipeText);

            if (!string.IsNullOrEmpty(b.DeviceName))
                r.DrawText(b.ColPipeX + 20, b.TextY, b.DeviceName);
        }
    }
}
