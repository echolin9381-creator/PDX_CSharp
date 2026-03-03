using System;
using System.Collections.Generic;
using System.Text;
using PDX_CSharp.Core.Interfaces;

namespace PDX_CSharp.Infrastructure.CAD
{
    /// <summary>
    /// 控制台渲染器（实现 ICadRenderer）。
    ///
    /// 用途：
    ///   ✔ 脱离 AutoCAD 的单元测试（SelfTest.cs 平台测试组使用）
    ///   ✔ CI/CD 环境验证
    ///   ✔ 调试：将图元输出为可读的文字日志
    ///
    /// 无任何 AutoCAD 依赖，可在任何 .NET 环境中运行。
    /// </summary>
    public class ConsoleCadRenderer : ICadRenderer
    {
        /// <summary>是否输出详细日志（默认 false，仅记录统计）</summary>
        public bool Verbose { get; set; }

        // ── 统计计数（供测试断言使用）──────────────────────────────────
        public int LineCount    { get; private set; }
        public int TextCount    { get; private set; }
        public int SymbolCount  { get; private set; }

        /// <summary>所有绘制的文字内容（测试断言可检查）</summary>
        public List<string> DrawnTexts { get; private set; }

        public ConsoleCadRenderer()
        {
            DrawnTexts = new List<string>();
        }

        // ── ICadRenderer ─────────────────────────────────────────────────

        public void BeginDraw()
        {
            LineCount   = 0;
            TextCount   = 0;
            SymbolCount = 0;
            DrawnTexts.Clear();

            if (Verbose)
                Console.WriteLine("[ConsoleCadRenderer] BeginDraw");
        }

        public void EndDraw()
        {
            if (Verbose)
                Console.WriteLine(string.Format(
                    "[ConsoleCadRenderer] EndDraw → Lines:{0} Texts:{1} Symbols:{2}",
                    LineCount, TextCount, SymbolCount));
        }

        public void DrawLine(double x1, double y1, double x2, double y2, bool thick = false)
        {
            LineCount++;
            if (Verbose)
                Console.WriteLine(string.Format(
                    "  LINE ({0:F1},{1:F1}) → ({2:F1},{3:F1}){4}",
                    x1, y1, x2, y2, thick ? " [thick]" : ""));
        }

        public void DrawText(double x, double y, string text, double height = 0)
        {
            if (string.IsNullOrEmpty(text)) return;
            TextCount++;
            DrawnTexts.Add(text);
            if (Verbose)
                Console.WriteLine(string.Format(
                    "  TEXT ({0:F1},{1:F1}) \"{2}\"", x, y, text));
        }

        public void DrawSymbol(string symbolType, double cx, double cy)
        {
            SymbolCount++;
            if (Verbose)
                Console.WriteLine(string.Format(
                    "  SYMBOL [{0}] @ ({1:F1},{2:F1})", symbolType, cx, cy));
        }

        // ── 测试辅助 ──────────────────────────────────────────────────────

        /// <summary>返回本次绘制的综合摘要字符串</summary>
        public string GetSummary()
        {
            return string.Format(
                "Lines={0} Texts={1} Symbols={2}",
                LineCount, TextCount, SymbolCount);
        }

        /// <summary>检查 DrawnTexts 中是否包含指定文字（精确匹配）</summary>
        public bool HasText(string text)
        {
            return DrawnTexts.Contains(text);
        }

        /// <summary>检查 DrawnTexts 中是否有文字包含指定子串</summary>
        public bool HasTextContaining(string substring)
        {
            foreach (var t in DrawnTexts)
                if (t.IndexOf(substring, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }
    }
}
