using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PDX_CSharp.Core.Interfaces;
using PDX_CSharp.Core.Models;

namespace PDX_CSharp.Infrastructure.Excel
{
    /// <summary>
    /// CSV Excel 模板生成器（实现 IExcelTemplateBuilder）。
    ///
    /// 在 .NET Framework 4.7 + csc.exe 直接构建的约束下，
    /// 以 CSV 作为 Excel 模板载体（Excel 可无损读写 CSV）。
    ///
    /// 生成规则：
    ///   - 第1行：字段 DisplayName 表头（列标题）
    ///   - 第2行（注释行）：标注字段类型 [INPUT] / [AUTO]
    ///   - 第3行起：供用户填写的数据行（预留 MaxDataRows 行空行）
    ///
    /// 将来升级至 EPPlus / NPOI 时，仅替换本类，接口不变。
    /// </summary>
    public class CsvExcelTemplateBuilder : IExcelTemplateBuilder
    {
        /// <summary>预留数据行数（默认30行）</summary>
        public int MaxDataRows { get; set; }

        public CsvExcelTemplateBuilder() { MaxDataRows = 30; }

        /// <summary>
        /// 生成 CSV 格式的 Excel 模板。
        /// </summary>
        /// <param name="def">图样模板定义</param>
        /// <param name="outputPath">输出文件路径（.csv）</param>
        public void GenerateTemplate(TemplateDefinition def, string outputPath)
        {
            if (def == null)
                throw new ArgumentNullException("def");
            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentNullException("outputPath");

            string dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var sb = new StringBuilder();

            // ── 第0行：模板信息注释 ─────────────────────────────────────
            sb.AppendLine(string.Format("# 模板:{0}  生成时间:{1}",
                def.DisplayName, DateTime.Now.ToString("yyyy-MM-dd HH:mm")));

            // ── 第1行：表头（字段 DisplayName + 单位）─────────────────
            var headerParts = new List<string>();
            foreach (var field in def.Fields)
            {
                string colLabel = string.IsNullOrEmpty(field.Unit)
                    ? field.DisplayName
                    : string.Format("{0}({1})", field.DisplayName, field.Unit);
                headerParts.Add(EscapeCsv(colLabel));
            }
            sb.AppendLine(string.Join(",", headerParts.ToArray()));

            // ── 第2行：字段类型标注（INPUT=用户填写 / AUTO=自动计算）──
            var typeParts = new List<string>();
            foreach (var field in def.Fields)
                typeParts.Add(EscapeCsv(field.IsCalculated ? "[AUTO-自动计算]" : "[INPUT-请填写]"));
            sb.AppendLine(string.Join(",", typeParts.ToArray()));

            // ── 第3～N行：空数据行 ──────────────────────────────────────
            int colCount = def.Fields.Count;
            for (int row = 0; row < MaxDataRows; row++)
            {
                var cells = new List<string>();
                for (int col = 0; col < colCount; col++)
                    cells.Add(string.Empty);
                sb.AppendLine(string.Join(",", cells.ToArray()));
            }

            // UTF-8 BOM（确保 Excel 直接双击打开时中文不乱码）
            File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        /// <summary>CSV 字段转义（含逗号/双引号/换行时加引号包裹）</summary>
        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.IndexOf(',') >= 0 || value.IndexOf('"') >= 0 || value.IndexOf('\n') >= 0)
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }
}
