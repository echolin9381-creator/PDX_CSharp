namespace PDX_CSharp.Models
{
    /// <summary>
    /// 单条断路器匹配规则
    /// </summary>
    public class BreakerRule
    {
        /// <summary>额定电流上限（A）。计算电流 ≤ 此值时选用本型号。</summary>
        public double CurrentLimit { get; set; }

        /// <summary>断路器型号，例如 C16</summary>
        public string Model { get; set; }

        /// <summary>对应电缆规格，例如 2.5mm²</summary>
        public string Cable { get; set; }

        /// <summary>对应穿管规格，例如 SC20</summary>
        public string Conduit { get; set; }
    }
}
