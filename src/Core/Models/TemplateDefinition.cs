using System.Collections.Generic;

namespace PDX_CSharp.Core.Models
{
    /// <summary>
    /// 图样模板定义。
    /// 描述一种图样类型的元数据：ID、显示名称、字段集合。
    /// 可由 AI 分析器填充，也可写死作为内置模板定义。
    /// </summary>
    public class TemplateDefinition
    {
        /// <summary>模板唯一标识符（如 "vertical_bus"）</summary>
        public string TemplateId { get; set; }

        /// <summary>用户可见显示名称（如 "配电箱系统图"）</summary>
        public string DisplayName { get; set; }

        /// <summary>图样类型分类（如 "PowerDistribution"）</summary>
        public string DiagramType { get; set; }

        /// <summary>是否支持主开关（进线断路器）行</summary>
        public bool SupportsMainSwitch { get; set; }

        /// <summary>是否包含表格区域（如负荷统计表）</summary>
        public bool HasTableArea { get; set; }

        /// <summary>回路类型（"SingleBranch" / "MultiBranch" / "DualBus" 等）</summary>
        public string LoopType { get; set; }

        /// <summary>字段描述符列表（必填字段 + 计算字段）</summary>
        public List<FieldDescriptor> Fields { get; set; }

        public TemplateDefinition()
        {
            Fields = new List<FieldDescriptor>();
            SupportsMainSwitch = true;
            LoopType = "MultiBranch";
        }

        // ── 内置标准模板定义 ──────────────────────────────────────────

        /// <summary>配电箱系统图（竖向单母线）标准模板</summary>
        public static TemplateDefinition VerticalBus()
        {
            return new TemplateDefinition
            {
                TemplateId   = "vertical_bus",
                DisplayName  = "配电箱系统图（竖向母线）",
                DiagramType  = "PowerDistribution",
                SupportsMainSwitch = true,
                HasTableArea = false,
                LoopType     = "MultiBranch",
                Fields = new List<FieldDescriptor>
                {
                    new FieldDescriptor { Name = "Phase",       DisplayName = "相别",     IsCalculated = false, Unit = "" },
                    new FieldDescriptor { Name = "BreakerModel",DisplayName = "断路器型号", IsCalculated = false, Unit = "" },
                    new FieldDescriptor { Name = "UnitPower",   DisplayName = "功率",     IsCalculated = false, Unit = "kW" },
                    new FieldDescriptor { Name = "DeviceCount", DisplayName = "台数",     IsCalculated = false, Unit = "" },
                    new FieldDescriptor { Name = "DeviceName",  DisplayName = "设备名称", IsCalculated = false, Unit = "" },
                    new FieldDescriptor { Name = "Cable",       DisplayName = "电缆",     IsCalculated = false, Unit = "" },
                    new FieldDescriptor { Name = "Conduit",     DisplayName = "穿管",     IsCalculated = false, Unit = "" },
                    new FieldDescriptor { Name = "TotalPower",  DisplayName = "总功率",   IsCalculated = true,  Unit = "kW" },
                    new FieldDescriptor { Name = "Current",     DisplayName = "电流",     IsCalculated = true,  Unit = "A"  },
                }
            };
        }

        /// <summary>双母线系统图模板（存根）</summary>
        public static TemplateDefinition DualBus()
        {
            return new TemplateDefinition
            {
                TemplateId   = "dual_bus",
                DisplayName  = "双母线系统图",
                DiagramType  = "PowerDistribution",
                SupportsMainSwitch = true,
                LoopType     = "DualBus",
                Fields = new List<FieldDescriptor>
                {
                    new FieldDescriptor { Name = "Phase",       DisplayName = "相别",     IsCalculated = false },
                    new FieldDescriptor { Name = "BreakerModel",DisplayName = "断路器型号", IsCalculated = false },
                    new FieldDescriptor { Name = "UnitPower",   DisplayName = "功率",     IsCalculated = false, Unit = "kW" },
                    new FieldDescriptor { Name = "Current",     DisplayName = "电流",     IsCalculated = true,  Unit = "A"  },
                }
            };
        }
    }

    /// <summary>
    /// 字段描述符。描述图样中一个数据字段的元信息。
    /// </summary>
    public class FieldDescriptor
    {
        /// <summary>字段程序名（与 BranchCircuit 属性名对应）</summary>
        public string Name { get; set; }

        /// <summary>字段中文显示名</summary>
        public string DisplayName { get; set; }

        /// <summary>单位（kW / A / mm² 等；可为空）</summary>
        public string Unit { get; set; }

        /// <summary>
        /// true=计算字段（由 ICalculationEngine 填充，Excel中禁止编辑）；
        /// false=输入字段（用户必填）。
        /// </summary>
        public bool IsCalculated { get; set; }
    }

    /// <summary>
    /// AI 结构分析结果。AI 分析器将图样信息转换为此对象后返回。
    /// 平台根据此对象决定字段模型、Excel结构、计算映射。
    /// </summary>
    public class AiAnalysisResult
    {
        /// <summary>必填字段名称列表（非计算）</summary>
        public List<string> RequiredFields { get; set; }

        /// <summary>计算字段名称列表</summary>
        public List<string> CalculatedFields { get; set; }

        /// <summary>是否支持主开关</summary>
        public bool SupportsMainSwitch { get; set; }

        /// <summary>回路类型</summary>
        public string LoopType { get; set; }

        /// <summary>是否包含表格区域</summary>
        public bool HasTableArea { get; set; }

        /// <summary>AI 响应原始 JSON（调试用）</summary>
        public string RawJson { get; set; }

        public AiAnalysisResult()
        {
            RequiredFields   = new List<string>();
            CalculatedFields = new List<string>();
            LoopType         = "MultiBranch";
        }
    }
}
