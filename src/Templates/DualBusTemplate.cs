using System;
using PDX_CSharp.Core.Interfaces;
using PDX_CSharp.Core.Models;

namespace PDX_CSharp.Templates
{
    /// <summary>
    /// 双母线系统图模板（存根实现）。
    /// 实现 IDiagramTemplate 接口以保证平台可扩展性。
    /// 当前版本仅输出占位日志；正式实现时替换 BuildLayout 和 Render 方法体。
    /// </summary>
    public class DualBusTemplate : IDiagramTemplate
    {
        public string TemplateId  { get { return "dual_bus"; } }
        public string DisplayName { get { return "双母线系统图"; } }

        /// <summary>
        /// [存根] 返回最小化拓扑模型以保证不抛异常。
        /// 正式实现时替换为完整的双母线布局计算。
        /// </summary>
        public DiagramTopology BuildLayout(DiagramModel model)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            // 存根：返回空拓扑（包含母线起止点以免 Render 报 NullReferenceException）
            var topology = new DiagramTopology
            {
                MainBus = new BusBarTopology
                {
                    Start = new Point2D(0, 0),
                    End   = new Point2D(0, -100)
                }
            };

            // 将每个分支原样写入（占位）
            if (model.Branches != null)
            {
                for (int i = 0; i < model.Branches.Count; i++)
                {
                    BranchCircuit b = model.Branches[i];
                    topology.Branches.Add(new BranchTopology
                    {
                        Y           = -(20 + i * 12.0),
                        TextY       = -(20 + i * 12.0) + 1.5,
                        LineStartX  = 0,
                        LineEndX    = 185,
                        BreakerCenterX = 57,
                        LoopNoText  = b.Index.ToString("D2"),
                        PhaseText   = b.Phase ?? "",
                        PowerText   = b.FormatPower(),
                        BreakerText = b.BreakerModel ?? "",
                        CurrentText = string.Format("{0:F1}A", b.Current),
                        ColNoX = 5, ColPhaseX = 20, ColPowerX = 35,
                        ColBreakerX = 65, ColCurrentX = 93,
                        ColCableX = 118, ColPipeX = 148
                    });
                }
            }

            return topology;
        }

        /// <summary>
        /// [存根] 仅绘制提示文字。正式实现时替换为双母线完整绘图逻辑。
        /// </summary>
        public void Render(DiagramTopology topology, ICadRenderer renderer)
        {
            if (topology == null) throw new ArgumentNullException("topology");
            if (renderer == null) throw new ArgumentNullException("renderer");

            renderer.BeginDraw();
            renderer.DrawText(0, 0, "[DualBusTemplate] 双母线图样（存根）待实现");
            renderer.EndDraw();
        }
    }
}
