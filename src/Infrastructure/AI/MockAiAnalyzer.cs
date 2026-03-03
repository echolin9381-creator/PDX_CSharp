using System;
using System.Collections.Generic;
using PDX_CSharp.Core.Interfaces;
using PDX_CSharp.Core.Models;

namespace PDX_CSharp.Infrastructure.AI
{
    /// <summary>
    /// Mock AI 分析器（离线实现）。
    ///
    /// 作用：在不联网、不配置 API Key 的情况下，
    ///       为各内置图样模板提供预设的 AiAnalysisResult。
    ///
    /// 生产环境替换方式：将 ITemplateAnalyzer 注入点换为 GptAnalyzer 即可，
    ///                   平台其余代码 0 改动。
    /// </summary>
    public class MockAiAnalyzer : ITemplateAnalyzer
    {
        /// <summary>
        /// 根据 TemplateId 返回预设分析结果。
        /// 未知模板返回通用默认结构（保证平台不崩溃）。
        /// </summary>
        public AiAnalysisResult Analyze(TemplateDefinition def)
        {
            if (def == null)
                throw new ArgumentNullException("def");

            switch (def.TemplateId.ToLowerInvariant())
            {
                case "vertical_bus":
                    return VerticalBusResult();

                case "dual_bus":
                    return DualBusResult();

                default:
                    return DefaultResult(def);
            }
        }

        // ── 预设结果 ────────────────────────────────────────────────────

        private static AiAnalysisResult VerticalBusResult()
        {
            return new AiAnalysisResult
            {
                RequiredFields = new List<string>
                {
                    "Phase", "BreakerModel", "UnitPower",
                    "DeviceCount", "DeviceName", "Cable", "Conduit"
                },
                CalculatedFields = new List<string>
                {
                    "TotalPower", "Current"
                },
                SupportsMainSwitch = true,
                HasTableArea       = false,
                LoopType           = "MultiBranch",
                RawJson            = "{\"source\":\"MockAiAnalyzer\",\"templateId\":\"vertical_bus\"}"
            };
        }

        private static AiAnalysisResult DualBusResult()
        {
            return new AiAnalysisResult
            {
                RequiredFields = new List<string>
                {
                    "Phase", "BreakerModel", "UnitPower"
                },
                CalculatedFields = new List<string>
                {
                    "Current"
                },
                SupportsMainSwitch = true,
                HasTableArea       = false,
                LoopType           = "DualBus",
                RawJson            = "{\"source\":\"MockAiAnalyzer\",\"templateId\":\"dual_bus\"}"
            };
        }

        private static AiAnalysisResult DefaultResult(TemplateDefinition def)
        {
            return new AiAnalysisResult
            {
                RequiredFields = new List<string> { "Phase", "UnitPower" },
                CalculatedFields = new List<string> { "Current" },
                SupportsMainSwitch = false,
                LoopType = "MultiBranch",
                RawJson  = string.Format(
                    "{{\"source\":\"MockAiAnalyzer\",\"templateId\":\"{0}\",\"fallback\":true}}",
                    def.TemplateId)
            };
        }
    }
}
