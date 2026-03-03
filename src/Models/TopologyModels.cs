using System.Collections.Generic;

namespace PDX_CSharp.Models
{
    /// <summary>
    /// 布局坐标配置中心
    /// 所有静态尺寸与排版参数必须从此读取，禁止在代码中 hardcode
    /// </summary>
    public class LayoutConfig
    {
        // ── 纵向排布参数 ──
        /// <summary>第一条回路距离顶部的偏移 (mm)</summary>
        public double TopOffset { get; set; }
        /// <summary>回路间距 (mm)</summary>
        public double BranchSpacing { get; set; }
        /// <summary>主断路器行位置 (Y)</summary>
        public double MainBreakerY { get; set; }
        /// <summary>主母线底部延伸量系数 (BranchSpacing 的倍数)</summary>
        public double BusBarBottomExtensionFactor { get; set; }

        // ── 横向排布参数 ──
        /// <summary>竖向主母线 X 坐标</summary>
        public double BusX { get; set; }
        /// <summary>回路水平线向右延伸总长度</summary>
        public double BranchLineLength { get; set; }
        /// <summary>断路器符号中心 X 坐标</summary>
        public double BreakerSymbolX { get; set; }

        // ── 列 X 坐标定义 ──
        public double ColNo { get; set; }
        public double ColPhase { get; set; }
        public double ColPower { get; set; }
        public double ColBreaker { get; set; }
        public double ColCurrent { get; set; }
        public double ColCable { get; set; }
        public double ColPipe { get; set; }

        // ── 符号与文字参数 ──
        public double BreakerSymbolHalfHeight { get; set; }
        public double BreakerSymbolHalfWidth { get; set; }
        public double TextHeight { get; set; }
        public double TextUpOffset { get; set; }

        public LayoutConfig()
        {
            TopOffset                  = 20.0;
            BranchSpacing              = 12.0;
            MainBreakerY               = -8.0;
            BusBarBottomExtensionFactor= 0.5;

            BusX                       = 0.0;
            BranchLineLength           = 185.0;
            BreakerSymbolX             = 57.0;

            ColNo                      = 5.0;
            ColPhase                   = 20.0;
            ColPower                   = 35.0;
            ColBreaker                 = 65.0;
            ColCurrent                 = 93.0;
            ColCable                   = 118.0;
            ColPipe                    = 148.0;

            BreakerSymbolHalfHeight    = 4.0;
            BreakerSymbolHalfWidth     = 3.5;
            TextHeight                 = 2.5;
            TextUpOffset               = 1.5;
        }

        // 默认实例
        public static LayoutConfig Default { get { return new LayoutConfig(); } }
    }

    /// <summary>
    /// 基础二维坐标点
    /// </summary>
    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// 主母线拓扑模型 (单一竖向母线)
    /// </summary>
    public class BusBar
    {
        /// <summary>起点 (通常为 0,0 附近)</summary>
        public Point Start { get; set; }
        
        /// <summary>终点 (决定主母线总长度)</summary>
        public Point End { get; set; }

        /// <summary>挂载在该母线上的所有支路</summary>
        public List<Branch> Branches { get; set; }

        public BusBar()
        {
            Branches = new List<Branch>();
        }
    }

    /// <summary>
    /// 支路拓扑模型 (统一从母线水平引出)
    /// </summary>
    public class Branch
    {
        /// <summary>回路逻辑索引 (0-based)</summary>
        public int Index { get; set; }
        
        /// <summary>支路所在的绝对 Y 坐标 (基于母线原点)</summary>
        public double Y { get; set; }

        /// <summary>回路编号</summary>
        public string LoopNo { get; set; }
        
        /// <summary>相序 (L1/L2/L3)</summary>
        public string Phase { get; set; }
        
        /// <summary>断路器型号</summary>
        public string Breaker { get; set; }
        
        /// <summary>负荷容量 / 设备描述</summary>
        public string Power { get; set; }
        
        /// <summary>计算电流</summary>
        public string Current { get; set; }
        
        /// <summary>电缆规格</summary>
        public string Cable { get; set; }
        
        /// <summary>穿管规格</summary>
        public string Pipe { get; set; }

        /// <summary>设备名称 (如有)</summary>
        public string DeviceName { get; set; }
    }
}
