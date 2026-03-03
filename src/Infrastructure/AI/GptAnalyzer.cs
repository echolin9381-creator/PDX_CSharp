using System;
using PDX_CSharp.Core.Interfaces;
using PDX_CSharp.Core.Models;

namespace PDX_CSharp.Infrastructure.AI
{
    /// <summary>
    /// GPT AI 分析器（联网实现存根）。
    ///
    /// 激活步骤：
    ///   1. 设置 ApiKey 属性（从配置文件或环境变量读取）
    ///   2. 将 Analyze() 方法体替换为实际 HTTP 调用（System.Net.Http.HttpClient 或 WebClient）
    ///   3. 在 DiagramGenerationService 的构造函数中将 MockAiAnalyzer 替换为 GptAnalyzer
    ///
    /// 平台其余层代码 0 改动。
    /// </summary>
    public class GptAnalyzer : ITemplateAnalyzer
    {
        /// <summary>OpenAI / Azure OpenAI 的 API Key（从外部注入）</summary>
        public string ApiKey { get; set; }

        /// <summary>API 端点 URL（默认 OpenAI；可替换为 Azure / 本地 Ollama）</summary>
        public string EndpointUrl { get; set; }

        /// <summary>使用的模型名称（默认 gpt-4o）</summary>
        public string ModelName { get; set; }

        public GptAnalyzer()
        {
            EndpointUrl = "https://api.openai.com/v1/chat/completions";
            ModelName   = "gpt-4o";
        }

        /// <summary>
        /// [存根] 调用 GPT API 分析图样结构。
        /// 正式实现时替换本方法体，接口签名不变。
        /// </summary>
        public AiAnalysisResult Analyze(TemplateDefinition def)
        {
            if (def == null)
                throw new ArgumentNullException("def");

            if (string.IsNullOrEmpty(ApiKey))
                throw new InvalidOperationException(
                    "GptAnalyzer.ApiKey 未配置。请先设置 ApiKey 属性，" +
                    "或在离线场景下使用 MockAiAnalyzer。");

            // TODO: 实现真实 HTTP 调用
            // string prompt = BuildPrompt(def);
            // string json   = CallGptApi(prompt);
            // return ParseResponse(json);

            throw new NotImplementedException(
                "GptAnalyzer 尚未实现真实 API 调用。" +
                "请实现 CallGptApi() 方法或使用 MockAiAnalyzer。");
        }

        // ── 预留扩展点 ────────────────────────────────────────────────────

        /// <summary>构建发送给 GPT 的 Prompt（预留）</summary>
        private string BuildPrompt(TemplateDefinition def)
        {
            return string.Format(
                "You are an electrical diagram expert. Analyze the following diagram template " +
                "and return a JSON object with RequiredFields (list of non-calculated field names), " +
                "CalculatedFields (list of auto-calculated field names), " +
                "SupportsMainSwitch (bool), LoopType (string), HasTableArea (bool).\n" +
                "Template ID: {0}\nTemplate Name: {1}\nDiagram Type: {2}",
                def.TemplateId, def.DisplayName, def.DiagramType);
        }
    }
}
