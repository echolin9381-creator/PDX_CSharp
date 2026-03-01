namespace PDX_CSharp.Models
{
    /// <summary>
    /// 主回路（进线）数据
    /// </summary>
    public class MainCircuitData
    {
        /// <summary>所有回路总负荷，单位：kW</summary>
        public double TotalKW { get; set; }

        /// <summary>需求系数 KD（0 ~ 1.5 一般取 0.5 ~ 1.0）</summary>
        public double DemandFactor { get; set; }

        /// <summary>需求负荷（TotalKW × KD），单位：kW</summary>
        public double DemandKW
        {
            get { return TotalKW * DemandFactor; }
        }

        /// <summary>计算主电流，单位：A</summary>
        public double Current { get; set; }

        /// <summary>主断路器型号</summary>
        public string MainBreaker { get; set; }

        /// <summary>主电缆规格</summary>
        public string MainCable { get; set; }

        /// <summary>主穿管规格</summary>
        public string MainConduit { get; set; }
    }
}
