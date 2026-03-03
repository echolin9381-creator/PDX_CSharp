using System.Collections.Generic;

namespace PDX_CSharp.Core.Models
{
    /// <summary>
    /// 平台统一图样数据模型（平台层核心数据容器）。
    /// 贯穿 AI解析 → 计算引擎 → 模板引擎 → CAD渲染 全链路。
    /// 禁止在此类中引入 AutoCAD / Excel / AI 任何 Infrastructure 依赖。
    /// </summary>
    public class DiagramModel
    {
        /// <summary>所使用的图样模板定义（含字段描述符）</summary>
        public TemplateDefinition Template { get; set; }

        /// <summary>所有出线回路（分支电路）列表</summary>
        public List<BranchCircuit> Branches { get; set; }

        /// <summary>主开关数据（如图样支持则有值）</summary>
        public MainSwitchData MainSwitch { get; set; }

        /// <summary>布局配置（坐标/间距参数）</summary>
        public LayoutConfig Layout { get; set; }

        public DiagramModel()
        {
            Branches = new List<BranchCircuit>();
            Layout   = LayoutConfig.Default;
        }
    }

    /// <summary>
    /// 分支回路（出线回路）数据模型。
    /// 包含用户输入字段（必填）和计算引擎填写字段（计算字段）。
    /// 禁止在此处写任何电气公式。
    /// </summary>
    public class BranchCircuit
    {
        /// <summary>回路逻辑序号（1-based）</summary>
        public int Index { get; set; }

        // ── 用户输入字段（RequiredFields）───────────────────────────
        /// <summary>相别（L1 / L2 / L3）</summary>
        public string Phase { get; set; }

        /// <summary>断路器型号（如 C16）</summary>
        public string BreakerModel { get; set; }

        /// <summary>负荷功率kW（单台）</summary>
        public double UnitPower { get; set; }

        /// <summary>设备台数</summary>
        public int    DeviceCount { get; set; }

        /// <summary>设备名称</summary>
        public string DeviceName { get; set; }

        /// <summary>电缆规格</summary>
        public string Cable { get; set; }

        /// <summary>穿管规格</summary>
        public string Conduit { get; set; }

        // ── 计算字段（CalculatedFields，由 ICalculationEngine 填写）──
        /// <summary>总功率 kW（= UnitPower × DeviceCount）</summary>
        public double TotalPower { get; set; }

        /// <summary>计算电流 A</summary>
        public double Current { get; set; }

        // ── 便捷属性 ───────────────────────────────────────────────
        /// <summary>是否备用回路（UnitPower == 0）</summary>
        public bool IsSpare { get { return UnitPower <= 0; } }

        /// <summary>格式化功率字符串（供模板 Render 使用）</summary>
        public string FormatPower()
        {
            if (IsSpare)       return "备用";
            if (DeviceCount > 1)
                return string.Format("{0}x{1:F1}kW", DeviceCount, UnitPower);
            return string.Format("{0:F1}kW", UnitPower);
        }

        public BranchCircuit()
        {
            DeviceCount = 1;
        }
    }

    /// <summary>
    /// 主开关（进线断路器）数据模型。
    /// </summary>
    public class MainSwitchData
    {
        /// <summary>需求系数 KD</summary>
        public double DemandFactor { get; set; }

        /// <summary>主开关断路器型号</summary>
        public string BreakerModel { get; set; }

        /// <summary>总功率 kW</summary>
        public double TotalPower { get; set; }

        /// <summary>主回路计算电流 A</summary>
        public double Current { get; set; }

        /// <summary>主回路电缆</summary>
        public string Cable { get; set; }

        /// <summary>主回路穿管</summary>
        public string Conduit { get; set; }
    }
}
