using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using PDX_CSharp.Calculation;
using PDX_CSharp.Drawing;
using PDX_CSharp.Models;
using PDX_CSharp.Rules;

namespace PDX_CSharp.Services
{
    public class PdxService
    {
        private const int MaxLoops = 60;

        public void Execute()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Editor   ed = doc.Editor;
            Database db = doc.Database;

            ed.WriteMessage("\n=== PDX 配电箱系统图 V1.0 ===\n");
            try
            {
                // 1. 回路数量
                var r1 = ed.GetInteger(new PromptIntegerOptions("\n出线回路数 (1-60):")
                    { LowerLimit = 1, UpperLimit = MaxLoops, AllowNone = false });
                if (r1.Status != PromptStatus.OK) return;
                int n = r1.Value;

                var unitKWList = new List<double>();
                var countList  = new List<int>();
                var nameList   = new List<string>();

                for (int i = 0; i < n; i++)
                {
                    // 单台容量
                    var r2 = ed.GetDouble(new PromptDoubleOptions(
                        string.Format("\n回路 {0} — 单台容量 kW (0=备用):", i + 1))
                        { AllowNone = false, AllowNegative = false });
                    if (r2.Status != PromptStatus.OK) return;
                    double unitKW = r2.Value;

                    // 台数（默认1）
                    int count = 1;
                    if (unitKW > 0)
                    {
                        var r3 = ed.GetInteger(new PromptIntegerOptions(
                            string.Format("\n回路 {0} — 台数 (1=单台, 回车=1):", i + 1))
                            { LowerLimit = 1, UpperLimit = 99,
                              AllowNone = true, DefaultValue = 1 });
                        if (r3.Status == PromptStatus.OK) count = r3.Value;
                        else if (r3.Status != PromptStatus.None) return;
                    }

                    // 设备名称
                    var r4 = ed.GetString(new PromptStringOptions(
                        string.Format("\n回路 {0} — 设备名称 (回车跳过):", i + 1))
                        { AllowSpaces = true });
                    string name = (r4.Status == PromptStatus.OK)
                                  ? r4.StringResult.Trim() : string.Empty;

                    unitKWList.Add(unitKW);
                    countList.Add(count);
                    nameList.Add(name);
                }

                // 2. 需求系数 KD
                var r5 = ed.GetDouble(new PromptDoubleOptions("\nKD 需求系数 (例如 0.8):")
                    { AllowNone = false, AllowNegative = false });
                if (r5.Status != PromptStatus.OK) return;
                double kd = Math.Max(0.1, Math.Min(2.0, r5.Value));

                // 3. 计算
                ed.WriteMessage("\n计算中...");
                var engine = new RuleEngine();
                var calc   = new CalculationEngine(engine);
                var loops  = calc.Calculate(unitKWList, countList, nameList);
                var main   = calc.CalcMain(loops, kd);

                ed.WriteMessage(string.Format(
                    "\n总负荷:{0:F1}kW  Ic:{1:F1}A  主开关:{2}",
                    main.TotalKW, main.Current, main.MainBreaker));

                // 4. 绘图
                ed.WriteMessage("\n生成系统图...");
                new DrawingEngine().Draw(db, loops, main);

                ed.WriteMessage(string.Format(
                    "\n完成！{0} 路。执行 ZOOM Extents 查看。\n", n));
            }
            catch (InvalidOperationException ex)
            {
                ed.WriteMessage(string.Format("\n[规则错误] {0}\n", ex.Message));
            }
            catch (Exception ex)
            {
                ed.WriteMessage(string.Format("\n[错误] {0}\n", ex.Message));
            }
        }
    }
}
