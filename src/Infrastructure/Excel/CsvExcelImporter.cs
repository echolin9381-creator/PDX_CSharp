using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PDX_CSharp.Core.Interfaces;
using PDX_CSharp.Core.Models;

namespace PDX_CSharp.Infrastructure.Excel
{
    /// <summary>
    /// CSV Excel 数据导入器（实现 IExcelImporter）。
    ///
    /// 读取由 CsvExcelTemplateBuilder 生成的 CSV 文件，
    /// 将用户填写的数据映射到 DiagramModel.Branches。
    /// 计算字段（IsCalculated=true）的值忽略，
    /// 后续交由 ICalculationEngine.Calculate 填写。
    /// </summary>
    public class CsvExcelImporter : IExcelImporter
    {
        /// <summary>
        /// 从 CSV 文件导入数据，返回未计算的 DiagramModel。
        /// </summary>
        /// <param name="filePath">CSV 文件路径</param>
        /// <param name="def">图样模板定义（用于字段名映射）</param>
        /// <returns>含 BranchCircuit 列表的 DiagramModel（计算字段为初始值）</returns>
        public DiagramModel Import(string filePath, TemplateDefinition def)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException("filePath");
            if (!File.Exists(filePath))
                throw new FileNotFoundException("CSV 文件不存在：" + filePath);
            if (def == null)
                throw new ArgumentNullException("def");

            // 读取全部行（UTF-8 BOM 兼容）
            string[] allLines = File.ReadAllLines(filePath, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            // 跳过注释行（# 开头）和空行，找表头行和数据行
            var dataLines = new List<string[]>();
            string[] headerCells = null;
            int lineIdx = 0;

            foreach (string rawLine in allLines)
            {
                string line = rawLine.TrimStart('\uFEFF').Trim(); // 去除 BOM 和空白
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;

                string[] cells = SplitCsvLine(line);

                if (lineIdx == 0)
                {
                    // 第一个有效行 = 表头
                    headerCells = cells;
                }
                else if (lineIdx == 1)
                {
                    // 第二行 = 字段类型注释行（[INPUT] / [AUTO]），跳过
                }
                else
                {
                    // 数据行：跳过全空行
                    bool hasData = false;
                    foreach (string c in cells) if (!string.IsNullOrEmpty(c.Trim())) { hasData = true; break; }
                    if (hasData)
                        dataLines.Add(cells);
                }
                lineIdx++;
            }

            if (headerCells == null)
                throw new InvalidOperationException("CSV 文件不含表头行，请检查文件格式。");

            // 构建字段名 → 列索引映射（按 DisplayName 匹配）
            var colIndex = BuildColumnIndex(headerCells, def);

            // ── 构建 DiagramModel ─────────────────────────────────────
            var model = new DiagramModel
            {
                Template   = def,
                Layout     = LayoutConfig.Default,
                MainSwitch = new MainSwitchData { DemandFactor = 0.8 }
            };

            for (int i = 0; i < dataLines.Count; i++)
            {
                string[] row = dataLines[i];
                var branch = new BranchCircuit { Index = i + 1 };

                branch.Phase        = GetCell(row, colIndex, "相别");
                branch.BreakerModel = GetCell(row, colIndex, "断路器型号");
                branch.DeviceName   = GetCell(row, colIndex, "设备名称");
                branch.Cable        = GetCell(row, colIndex, "电缆");
                branch.Conduit      = GetCell(row, colIndex, "穿管");

                double power;
                string powerStr = GetCell(row, colIndex, "功率");
                if (double.TryParse(powerStr, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out power))
                    branch.UnitPower = power;

                int count;
                string countStr = GetCell(row, colIndex, "台数");
                branch.DeviceCount = (int.TryParse(countStr, out count) && count > 0) ? count : 1;

                model.Branches.Add(branch);
            }

            return model;
        }

        // ── 私有辅助 ─────────────────────────────────────────────────────

        /// <summary>按字段 DisplayName 建立 列名→列索引 映射</summary>
        private Dictionary<string, int> BuildColumnIndex(string[] headers, TemplateDefinition def)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                // 去除单位（如 "功率(kW)" → "功率"）
                string h = headers[i].Trim().Trim('"');
                int paren = h.IndexOf('(');
                if (paren > 0) h = h.Substring(0, paren).Trim();
                if (!map.ContainsKey(h))
                    map[h] = i;
            }
            return map;
        }

        /// <summary>按列名读取单元格值（找不到列名时返回空字符串）</summary>
        private static string GetCell(string[] row, Dictionary<string, int> colIndex, string colName)
        {
            int idx;
            if (!colIndex.TryGetValue(colName, out idx)) return string.Empty;
            if (row == null || idx >= row.Length) return string.Empty;
            return row[idx].Trim().Trim('"');
        }

        /// <summary>
        /// 按 RFC4180 标准拆分 CSV 行（处理带引号的字段）。
        /// </summary>
        private static string[] SplitCsvLine(string line)
        {
            var parts = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"'); // 转义的双引号
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            parts.Add(current.ToString());
            return parts.ToArray();
        }
    }
}
