namespace PDX_CSharp.Core.Models
{
    /// <summary>
    /// 布局坐标配置中心（平台层版本）。
    /// 归属 Core.Models，所有组件统一读取此配置，禁止 hardcode 坐标。
    ///
    /// 注：旧 PDX_CSharp.Models.LayoutConfig 保持原位供旧命令使用。
    ///     新平台代码统一使用本类（PDX_CSharp.Core.Models.LayoutConfig）。
    /// </summary>
    public class LayoutConfig
    {
        // ── 纵向排布参数 ─────────────────────────────────────────────
        /// <summary>第一条回路距离母线起点的向下偏移 (mm)</summary>
        public double TopOffset { get; set; }

        /// <summary>回路间距 (mm)</summary>
        public double BranchSpacing { get; set; }

        /// <summary>主断路器行 Y 坐标（相对母线原点，通常为负值）</summary>
        public double MainBreakerY { get; set; }

        /// <summary>主母线底部延伸系数（BranchSpacing 的倍数）</summary>
        public double BusBarBottomExtensionFactor { get; set; }

        // ── 横向排布参数 ─────────────────────────────────────────────
        /// <summary>竖向主母线 X 坐标</summary>
        public double BusX { get; set; }

        /// <summary>回路水平线向右延伸总长度 (mm)</summary>
        public double BranchLineLength { get; set; }

        /// <summary>断路器符号中心 X 坐标</summary>
        public double BreakerSymbolX { get; set; }

        // ── 固定列 X 坐标 ────────────────────────────────────────────
        public double ColNo      { get; set; }
        public double ColPhase   { get; set; }
        public double ColPower   { get; set; }
        public double ColBreaker { get; set; }
        public double ColCurrent { get; set; }
        public double ColCable   { get; set; }
        public double ColPipe    { get; set; }

        // ── 符号与文字参数 ───────────────────────────────────────────
        public double BreakerSymbolHalfHeight { get; set; }
        public double BreakerSymbolHalfWidth  { get; set; }
        public double TextHeight              { get; set; }
        public double TextUpOffset            { get; set; }

        public LayoutConfig()
        {
            TopOffset                   = 20.0;
            BranchSpacing               = 12.0;
            MainBreakerY                = -8.0;
            BusBarBottomExtensionFactor = 0.5;

            BusX                        = 0.0;
            BranchLineLength            = 185.0;
            BreakerSymbolX              = 57.0;

            ColNo                       =   5.0;
            ColPhase                    =  20.0;
            ColPower                    =  35.0;
            ColBreaker                  =  65.0;
            ColCurrent                  =  93.0;
            ColCable                    = 118.0;
            ColPipe                     = 148.0;

            BreakerSymbolHalfHeight     = 4.0;
            BreakerSymbolHalfWidth      = 3.5;
            TextHeight                  = 2.5;
            TextUpOffset                = 1.5;
        }

        /// <summary>默认配置实例（工厂方法）</summary>
        public static LayoutConfig Default { get { return new LayoutConfig(); } }
    }
}
