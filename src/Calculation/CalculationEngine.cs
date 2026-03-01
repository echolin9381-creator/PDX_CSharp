using System;
using System.Collections.Generic;
using PDX_CSharp.Models;
using PDX_CSharp.Rules;

namespace PDX_CSharp.Calculation
{
    public class CalculationEngine
    {
        private const double CosPhiDefault = 0.85;
        private const double Sqrt3         = 1.7320508075688772;
        private const double SinglePhaseV  = 220.0;
        private const double ThreePhaseV   = 380.0;

        private readonly RuleEngine _rules;
        public CalculationEngine(RuleEngine rules) { _rules = rules; }

        // 单相电流: I = (P_kW × 1000) / (220 × cosφ)
        public double CalcSinglePhase(double kW, double cosPhi = CosPhiDefault)
        {
            if (kW <= 0) return 0;
            return (kW * 1000.0) / (SinglePhaseV * cosPhi);
        }

        // 三相主电流: I = (KD × TotalKW × 1000) / (√3 × 380)
        public double CalcMainCurrent(double totalKW, double kd)
        {
            if (totalKW <= 0) return 0;
            return (kd * totalKW * 1000.0) / (Sqrt3 * ThreePhaseV);
        }

        // 计算所有回路
        public List<LoopData> Calculate(
            List<double> unitKWList,
            List<int>    countList,
            List<string> nameList)
        {
            string[] phases = { "L1", "L2", "L3" };
            var loops = new List<LoopData>();

            for (int i = 0; i < unitKWList.Count; i++)
            {
                double unitKW = Math.Max(0, unitKWList[i]);
                int    count  = (countList != null && i < countList.Count && countList[i] > 1)
                                ? countList[i] : 1;
                double totalKW = unitKW * count;
                double current = CalcSinglePhase(totalKW);

                BreakerRule rule = _rules.MatchBreaker(current);

                string name = (nameList != null && i < nameList.Count &&
                               !string.IsNullOrEmpty(nameList[i]))
                              ? nameList[i]
                              : (totalKW <= 0 ? "备用" : string.Format("回路{0}", i + 1));

                loops.Add(new LoopData
                {
                    LoopNo      = i + 1,
                    Phase       = phases[i % 3],
                    LoadKW      = totalKW,
                    UnitLoadKW  = unitKW,
                    DeviceCount = count,
                    Current     = current,
                    Breaker     = rule.Model,
                    Cable       = rule.Cable,
                    Conduit     = rule.Conduit,
                    DeviceName  = name
                });
            }
            return loops;
        }

        // 计算主回路
        public MainCircuitData CalcMain(List<LoopData> loops, double kd)
        {
            double total = 0;
            foreach (var l in loops) total += l.LoadKW;
            double mainI = CalcMainCurrent(total, kd);
            BreakerRule r = _rules.MatchBreaker(mainI);
            return new MainCircuitData
            {
                TotalKW      = total,
                DemandFactor = kd,
                Current      = mainI,
                MainBreaker  = r.Model,
                MainCable    = r.Cable,
                MainConduit  = r.Conduit
            };
        }
    }
}
