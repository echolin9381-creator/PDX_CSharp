// PDX Layout Self-Test (no AutoCAD dependency)
// Tests: column spacing, Y positions, load formatting, rule matching
using System;
using System.Collections.Generic;
using System.IO;

class SelfTest
{
    // ── Mirror of DrawingEngine constants ─────────────────────────
    const double BusbarEndX  = 200.0;
    const double LoopStartY  = -20.0;
    const double LoopYStep   = -12.0;
    const double BrkSymX     =  57.0;
    const double BrkSymHalf  =   4.0;
    const double BrkSymHW    =   3.5;
    const double ColNo       =   5.0;
    const double ColPhase    =  20.0;
    const double ColLoad     =  35.0;
    const double ColBreaker  =  65.0;
    const double ColCurrent  =  93.0;
    const double ColCable    = 118.0;
    const double ColConduit  = 148.0;
    const double TextH       =   2.5;
    const double CharW       =   1.5;  // estimated char width at H=2.5
    const double CircuitEndX = 185.0;

    static int pass = 0, fail = 0;

    static void Check(string name, bool ok, string detail = "")
    {
        if (ok) { pass++; Console.WriteLine("[PASS] " + name); }
        else    { fail++; Console.WriteLine("[FAIL] " + name + (detail != "" ? " | " + detail : "")); }
    }

    static void Main()
    {
        Console.WriteLine("=== PDX DrawingEngine Self-Test ===\n");

        // ── T1: Column spacing >= 12mm ─────────────────────────────
        double[] cols = { ColNo, ColPhase, ColLoad, ColBreaker, ColCurrent, ColCable, ColConduit };
        for (int i = 1; i < cols.Length; i++)
        {
            double gap = cols[i] - cols[i - 1];
            Check(string.Format("ColSpacing[{0}->{1}] >= 12mm", i, i+1), gap >= 12.0,
                string.Format("gap={0:F1}mm", gap));
        }

        // ── T2: Last column + max text within BusbarEndX ──────────
        double maxConduitW = "SC80".Length * CharW;
        Check("ColConduit right edge <= 200mm",
            ColConduit + maxConduitW <= BusbarEndX,
            string.Format("right={0:F1}mm", ColConduit + maxConduitW));

        // ── T3: Breaker symbol does not overlap load text ──────────
        double maxLoadW = "10x99.9kW".Length * CharW;
        Check("BreakerSym left edge > load text right edge",
            BrkSymX - BrkSymHW > ColLoad + maxLoadW,
            string.Format("brkLeft={0:F1} loadRight={0:F1}", BrkSymX - BrkSymHW, ColLoad + maxLoadW));

        // ── T4: Y = -20 - 12*index formula ────────────────────────
        int[] loopCounts = { 1, 5, 12, 30, 60 };
        foreach (int n in loopCounts)
        {
            double firstY = LoopStartY;
            double lastY  = LoopStartY + (n - 1) * LoopYStep;
            double capY   = LoopStartY + n * LoopYStep;

            Check(string.Format("Y-formula n={0}: firstY={1:F0}", n, firstY),
                firstY == -20.0);
            Check(string.Format("Y-formula n={0}: lastY correct", n),
                Math.Abs(lastY - (-20.0 - 12.0 * (n - 1))) < 0.001);
            Check(string.Format("Y-formula n={0}: 12mm rows no overlap", n),
                Math.Abs(LoopYStep) >= TextH * 2.0,
                string.Format("step={0}, textH={1}", Math.Abs(LoopYStep), TextH));
        }

        // ── T5: Load formatting ────────────────────────────────────
        Check("Single device: '1.5kW'",   FormatLoad(1.5, 1, 1.5) == "1.5kW");
        Check("Multi device: '3x2.5kW'",  FormatLoad(7.5, 3, 2.5) == "3x2.5kW");
        Check("Spare: '备用'",              FormatLoad(0,   1, 0)   == "备用");

        // ── T6: Phase cycling L1/L2/L3 ────────────────────────────
        string[] phases = { "L1", "L2", "L3" };
        Check("Phase[0]=L1", phases[0 % 3] == "L1");
        Check("Phase[3]=L1", phases[3 % 3] == "L1");
        Check("Phase[11]=L3", phases[11 % 3] == "L3");

        // ── T7: Rule matching (mirrors GetBuiltinRules) ────────────
        Check("C6   <= 6A",   MatchBreaker(5.0)   == "C6");
        Check("C10  <= 10A",  MatchBreaker(9.0)   == "C10");
        Check("C16  <= 16A",  MatchBreaker(14.0)  == "C16");
        Check("C63  <= 63A",  MatchBreaker(60.0)  == "C63");
        Check("C250 <= 250A", MatchBreaker(240.0) == "C250");
        // Overflow
        bool threw = false;
        try { MatchBreaker(500.0); } catch { threw = true; }
        Check("Rule overflow throws exception", threw);

        // ── T8: Circuit line does not exceed BusbarEndX ───────────
        Check("CircuitEndX <= BusbarEndX", CircuitEndX <= BusbarEndX);

        // ── Summary ───────────────────────────────────────────────
        Console.WriteLine(string.Format(
            "\n{0} passed, {1} failed out of {2} total.",
            pass, fail, pass + fail));

        if (fail > 0) Environment.Exit(1);
    }

    static string FormatLoad(double totalKW, int count, double unitKW)
    {
        if (totalKW <= 0) return "备用";
        if (count > 1)    return string.Format("{0}x{1:F1}kW", count, unitKW);
        return string.Format("{0:F1}kW", totalKW);
    }

    // Mirrors RuleEngine.GetBuiltinRules + MatchBreaker
    static string MatchBreaker(double current)
    {
        var rules = new[]
        {
            new {L=6.0,   M="C6"},
            new {L=10.0,  M="C10"},
            new {L=16.0,  M="C16"},
            new {L=20.0,  M="C20"},
            new {L=25.0,  M="C25"},
            new {L=32.0,  M="C32"},
            new {L=40.0,  M="C40"},
            new {L=50.0,  M="C50"},
            new {L=63.0,  M="C63"},
            new {L=100.0, M="C100"},
            new {L=160.0, M="C160"},
            new {L=250.0, M="C250"},
        };
        foreach (var r in rules)
            if (r.L >= current) return r.M;
        throw new Exception("超出规则上限");
    }
}
