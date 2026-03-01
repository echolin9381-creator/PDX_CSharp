using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using PDX_CSharp.Models;

namespace PDX_CSharp.Rules
{
    /// <summary>
    /// 规则引擎：从 JSON 文件读取断路器/电缆规则，提供匹配方法。
    /// 若 JSON 文件缺失或解析失败，自动使用内置规则，保证插件不崩溃。
    /// 不依赖 System.Web.Extensions（兼容所有 .NET Framework 4.x 环境）。
    /// </summary>
    public class RuleEngine
    {
        private readonly List<BreakerRule> _rules;

        public RuleEngine()
        {
            _rules = LoadRules();
        }

        // ─── 规则加载 ──────────────────────────────────────────────────────────

        private List<BreakerRule> LoadRules()
        {
            try
            {
                // 优先从插件同目录加载 pdx_rules.json
                string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string jsonPath  = Path.Combine(pluginDir, "pdx_rules.json");

                if (File.Exists(jsonPath))
                {
                    string json  = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);
                    var    rules = ParseJson(json);
                    if (rules != null && rules.Count > 0)
                        return rules.OrderBy(r => r.CurrentLimit).ToList();
                }
            }
            catch
            {
                // JSON 读取/解析失败，回退到内置规则
            }

            return GetBuiltinRules();
        }

        /// <summary>
        /// 极简 JSON 解析：提取 breakers 数组中每个对象的 4 个字段。
        /// 格式严格按照 pdx_rules.json 约定，无需完整 JSON 解析器。
        /// </summary>
        private static List<BreakerRule> ParseJson(string json)
        {
            var rules = new List<BreakerRule>();

            // 用正则提取每个 {...} 对象块
            var blockPattern = new Regex(@"\{[^}]+\}", RegexOptions.Singleline);
            var dblPattern   = new Regex("\"currentLimit\"\\s*:\\s*([0-9.]+)");
            var strPattern   = new Regex("\"model\"\\s*:\\s*\"([^\"]+)\"");
            var cblPattern   = new Regex("\"cable\"\\s*:\\s*\"([^\"]+)\"");
            var cndPattern   = new Regex("\"conduit\"\\s*:\\s*\"([^\"]+)\"");

            foreach (Match block in blockPattern.Matches(json))
            {
                string b = block.Value;
                var mCur = dblPattern.Match(b);
                var mMod = strPattern.Match(b);
                var mCbl = cblPattern.Match(b);
                var mCnd = cndPattern.Match(b);

                if (!mCur.Success || !mMod.Success) continue;

                double limit;
                if (!double.TryParse(mCur.Groups[1].Value,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out limit))
                    continue;

                rules.Add(new BreakerRule
                {
                    CurrentLimit = limit,
                    Model        = mMod.Groups[1].Value,
                    Cable        = mCbl.Success ? mCbl.Groups[1].Value : "未知",
                    Conduit      = mCnd.Success ? mCnd.Groups[1].Value : "未知"
                });
            }

            return rules;
        }

        private static List<BreakerRule> GetBuiltinRules()
        {
            return new List<BreakerRule>
            {
                new BreakerRule { CurrentLimit =   6, Model = "C6",   Cable = "1.5mm²",  Conduit = "SC15" },
                new BreakerRule { CurrentLimit =  10, Model = "C10",  Cable = "1.5mm²",  Conduit = "SC15" },
                new BreakerRule { CurrentLimit =  16, Model = "C16",  Cable = "2.5mm²",  Conduit = "SC20" },
                new BreakerRule { CurrentLimit =  20, Model = "C20",  Cable = "4mm²",    Conduit = "SC20" },
                new BreakerRule { CurrentLimit =  25, Model = "C25",  Cable = "4mm²",    Conduit = "SC25" },
                new BreakerRule { CurrentLimit =  32, Model = "C32",  Cable = "6mm²",    Conduit = "SC25" },
                new BreakerRule { CurrentLimit =  40, Model = "C40",  Cable = "10mm²",   Conduit = "SC32" },
                new BreakerRule { CurrentLimit =  50, Model = "C50",  Cable = "16mm²",   Conduit = "SC40" },
                new BreakerRule { CurrentLimit =  63, Model = "C63",  Cable = "25mm²",   Conduit = "SC40" },
                new BreakerRule { CurrentLimit = 100, Model = "C100", Cable = "35mm²",   Conduit = "SC50" },
                new BreakerRule { CurrentLimit = 160, Model = "C160", Cable = "70mm²",   Conduit = "SC65" },
                new BreakerRule { CurrentLimit = 250, Model = "C250", Cable = "120mm²",  Conduit = "SC80" },
            };
        }

        // ─── 断路器匹配 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 返回第一个 CurrentLimit >= current 的断路器规则。
        /// 零/负值始终返回最小断路器；超出上限则抛业务异常。
        /// </summary>
        public BreakerRule MatchBreaker(double current)
        {
            if (current <= 0)
                return _rules.First();

            var match = _rules
                .OrderBy(r => r.CurrentLimit)
                .FirstOrDefault(r => r.CurrentLimit >= current);

            if (match == null)
                throw new InvalidOperationException(
                    string.Format("计算电流 {0:F1}A 超出规则上限（最大 {1}A），请扩展 pdx_rules.json。",
                        current, _rules.Max(r => r.CurrentLimit)));

            return match;
        }

        /// <summary>
        /// 按断路器型号精确匹配规则，找不到时返回默认值。
        /// </summary>
        public BreakerRule MatchByModel(string model)
        {
            return _rules.FirstOrDefault(r => r.Model == model)
                   ?? new BreakerRule { Model = model, Cable = "未知", Conduit = "未知" };
        }
    }
}
