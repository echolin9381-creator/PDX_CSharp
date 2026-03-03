using System.Collections.Generic;
using PDX_CSharp.Models;

namespace PDX_CSharp.Calculation
{
    /// <summary>
    /// 拓扑模型构建器 (Topology Builder)
    /// 负责计算所有组件的具体位置并生成结构化数据模型。
    /// 这个模型随后交给绘图引擎直接输出到 CAD
    /// </summary>
    public class TopologyBuilder
    {
        private readonly LayoutConfig _config;

        public TopologyBuilder(LayoutConfig config = null)
        {
            _config = config ?? LayoutConfig.Default;
        }

        public BusBar BuildModel(List<LoopData> loops)
        {
            if (loops == null || loops.Count == 0)
            {
                return new BusBar(); // or handle appropriately
            }

            var busBar = new BusBar
            {
                // 主母线向下绘制 (TotalHeight = TopOffset + BranchSpacing * Count + Extension)
                Start = new Point(_config.BusX, 0),
                End = new Point(_config.BusX, -_config.TopOffset - _config.BranchSpacing * loops.Count - _config.BranchSpacing * _config.BusBarBottomExtensionFactor)
            };

            for (int i = 0; i < loops.Count; i++)
            {
                var loop = loops[i];
                double yPos = -_config.TopOffset - (i * _config.BranchSpacing);

                var branch = new Branch
                {
                    Index = i,
                    Y = yPos,
                    LoopNo = loop.LoopNo.ToString("D2"),
                    Phase = string.IsNullOrEmpty(loop.Phase) ? "" : loop.Phase,
                    Breaker = string.IsNullOrEmpty(loop.Breaker) ? "" : loop.Breaker,
                    Power = FormatLoad(loop),
                    Current = string.Format("{0:F1}A", loop.Current),
                    Cable = string.IsNullOrEmpty(loop.Cable) ? "" : loop.Cable,
                    Pipe = string.IsNullOrEmpty(loop.Conduit) ? "" : loop.Conduit,
                    DeviceName = string.IsNullOrEmpty(loop.DeviceName) ? "" : loop.DeviceName
                };

                busBar.Branches.Add(branch);
            }

            return busBar;
        }

        /// <summary>
        /// 格式化负荷文字
        /// </summary>
        private static string FormatLoad(LoopData loop)
        {
            if (loop.LoadKW <= 0)
                return "备用";
            if (loop.DeviceCount > 1)
                return string.Format("{0}x{1:F1}kW", loop.DeviceCount, loop.UnitLoadKW);
            return string.Format("{0:F1}kW", loop.LoadKW);
        }
    }
}
