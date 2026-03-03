using System;
using System.Collections.Generic;
using PDX_CSharp.Core.Interfaces;
using PDX_CSharp.Core.Models;
using PDX_CSharp.Models;
using PDX_CSharp.Rules;

namespace PDX_CSharp.Core.Calculation
{
    /// <summary>
    /// 电气参数计算引擎（实现 ICalculationEngine）。
    ///
    /// 职责：
    ///   ✔ 单相电流公式：I = (P_kW × 1000) / (220 × cosφ)
    ///   ✔ 三相主电流公式：I = (KD × ΣP × 1000) / (√3 × 380)
    ///   ✔ 总功率统计（TotalPower = UnitPower × DeviceCount）
    ///   ✔ 通过 RuleEngine 自动选型断路器、电缆、穿管
    ///
    /// 禁止：
    ///   ✗ 母线长度计算（归 IDiagramTemplate.BuildLayout）
    ///   ✗ 引用 LayoutConfig 任何字段
    ///   ✗ 引用 AutoCAD / Excel / AI 命名空间
    /// </summary>
    public class ElectricalCalculationEngine : ICalculationEngine
    {
        // ── 电气常量 ─────────────────────────────────────────────────────
        private const double DefaultCosPhi   = 0.85;
        private const double Sqrt3           = 1.7320508075688772;
        private const double SinglePhaseVolt = 220.0;
        private const double ThreePhaseVolt  = 380.0;

        private readonly RuleEngine _ruleEngine;

        /// <summary>
        /// 构造函数。注入规则引擎（断路器/电缆规则）。
        /// </summary>
        public ElectricalCalculationEngine(RuleEngine ruleEngine)
        {
            if (ruleEngine == null)
                throw new ArgumentNullException("ruleEngine");
            _ruleEngine = ruleEngine;
        }

        // ── ICalculationEngine 实现 ───────────────────────────────────────

        /// <summary>
        /// 对 DiagramModel 中所有 BranchCircuit 执行电气计算，
        /// 就地填写所有 IsCalculated=true 的字段（TotalPower, Current, BreakerModel, Cable, Conduit）。
        /// 最后计算并填写 MainSwitch 数据（如果 model.MainSwitch != null）。
        /// </summary>
        public void Calculate(DiagramModel model)
        {
            if (model == null)
                throw new ArgumentNullException("model");
            if (model.Branches == null)
                return;

            string[] phases = { "L1", "L2", "L3" };

            // ── 第一步：计算每条分支回路 ─────────────────────────────────
            for (int i = 0; i < model.Branches.Count; i++)
            {
                BranchCircuit branch = model.Branches[i];

                // 自动补全相别（如未设置）
                if (string.IsNullOrEmpty(branch.Phase))
                    branch.Phase = phases[i % 3];

                // 计算总功率
                int count = Math.Max(1, branch.DeviceCount);
                branch.DeviceCount = count;
                branch.TotalPower  = branch.UnitPower * count;

                // 电流计算（空路/备用 → 0A）
                branch.Current = CalcSinglePhase(branch.TotalPower);

                // 通过 RuleEngine 自动选型（仅当用户未填写时）
                if (string.IsNullOrEmpty(branch.BreakerModel) ||
                    string.IsNullOrEmpty(branch.Cable)        ||
                    string.IsNullOrEmpty(branch.Conduit))
                {
                    BreakerRule rule = _ruleEngine.MatchBreaker(branch.Current);
                    if (string.IsNullOrEmpty(branch.BreakerModel)) branch.BreakerModel = rule.Model;
                    if (string.IsNullOrEmpty(branch.Cable))        branch.Cable        = rule.Cable;
                    if (string.IsNullOrEmpty(branch.Conduit))      branch.Conduit      = rule.Conduit;
                }
            }

            // ── 第二步：计算主开关（如存在）────────────────────────────
            if (model.MainSwitch != null)
                CalculateMainSwitch(model.MainSwitch, model.Branches);
        }

        // ── 公开单项计算方法（供测试和外部直接调用）────────────────────────

        /// <summary>单相电流计算：I = (P_kW × 1000) / (V × cosφ)</summary>
        public double CalcSinglePhase(double powerKw, double cosPhi = DefaultCosPhi)
        {
            if (powerKw <= 0) return 0;
            return (powerKw * 1000.0) / (SinglePhaseVolt * cosPhi);
        }

        /// <summary>三相主电流计算：I = (KD × ΣP × 1000) / (√3 × 380)</summary>
        public double CalcThreePhaseMain(double totalKw, double demandFactor)
        {
            if (totalKw <= 0) return 0;
            return (demandFactor * totalKw * 1000.0) / (Sqrt3 * ThreePhaseVolt);
        }

        /// <summary>统计回路列表的总功率（kW）</summary>
        public static double SumTotalPower(List<BranchCircuit> branches)
        {
            double total = 0;
            if (branches == null) return total;
            foreach (var b in branches) total += b.TotalPower;
            return total;
        }

        /// <summary>负载率计算：LoadRate = ΣI_branch / I_main</summary>
        public static double CalcLoadRate(double branchCurrentSum, double mainCurrent)
        {
            if (mainCurrent <= 0) return 0;
            return branchCurrentSum / mainCurrent;
        }

        // ── 私有辅助 ─────────────────────────────────────────────────────

        private void CalculateMainSwitch(MainSwitchData sw, List<BranchCircuit> branches)
        {
            sw.TotalPower   = SumTotalPower(branches);
            sw.Current      = CalcThreePhaseMain(sw.TotalPower, Math.Max(0.1, sw.DemandFactor));

            BreakerRule rule = _ruleEngine.MatchBreaker(sw.Current);
            if (string.IsNullOrEmpty(sw.BreakerModel)) sw.BreakerModel = rule.Model;
            if (string.IsNullOrEmpty(sw.Cable))        sw.Cable        = rule.Cable;
            if (string.IsNullOrEmpty(sw.Conduit))      sw.Conduit      = rule.Conduit;
        }
    }
}
