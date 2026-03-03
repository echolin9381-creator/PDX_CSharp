using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PDX_CSharp.Models;
using PDX_CSharp.Calculation;

namespace PDX_CSharp.Drawing
{
    /// <summary>
    /// 列式排版绘图引擎 - 采用拓扑驱动模型绘制配电系统图。
    /// 所有坐标与排版尺寸均受控于 LayoutConfig 配置。
    /// </summary>
    public class DrawingEngine
    {
        private const string LAYER      = "PDX_LAYER";
        private const string STYLE_NAME = "PDX_STYLE";

        private readonly LayoutConfig _config;

        public DrawingEngine(LayoutConfig config = null)
        {
            _config = config ?? LayoutConfig.Default;
        }

        // ── 公开入口 ──────────────────────────────────────────────────
        public void Draw(Database db, List<LoopData> loops, MainCircuitData main)
        {
            // 1. 采用拓扑驱动模型构建绘图数据
            TopologyBuilder builder = new TopologyBuilder(_config);
            BusBar busBarModel = builder.BuildModel(loops);
            
            ValidateLayout(_config, loops.Count);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerHelper.EnsureLayer(tr, db, LAYER);
                    ObjectId styleId = GetOrCreateTextStyle(tr, db);

                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord ms = tr.GetObject(
                        bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // 2. 绘制主母线
                    DrawMainBusbar(tr, ms, busBarModel);
                    
                    // 3. 绘制主断路器行信息
                    DrawMainBreakerRow(tr, ms, main, styleId);

                    // 4. 遍历 Branch 生成支路
                    foreach (var branch in busBarModel.Branches)
                    {
                        DrawBranch(tr, ms, branch, styleId);
                    }

                    tr.Commit();
                }
                catch { throw; }
            }
        }

        // ── 自检机制 ──────────────────────────────────────────────────
        private static void ValidateLayout(LayoutConfig cfg, int loopCount)
        {
            if (loopCount == 0)
                throw new InvalidOperationException("回路列表不能为空。");
            if (loopCount > 60)
                throw new InvalidOperationException(
                    string.Format("回路数 {0} 超出上限 60。", loopCount));

            // 检查列坐标：各列间距是否足够（最小 12mm）
            double[] cols = { cfg.ColNo, cfg.ColPhase, cfg.ColPower, cfg.ColBreaker, cfg.ColCurrent, cfg.ColCable, cfg.ColPipe };
            for (int i = 1; i < cols.Length; i++)
            {
                if (cols[i] - cols[i - 1] < 12.0)
                    throw new InvalidOperationException(
                        string.Format("列 {0} 与列 {1} 间距不足 12mm，请检查坐标配置。", i, i + 1));
            }

            // 检查最右列是否超出支路线范围
            if (cfg.ColPipe + 20.0 > cfg.BranchLineLength)
                throw new InvalidOperationException("最右列文字超出主母线引出的水平线范围。");
        }

        // ── 统一绘制母线 ──────────────────────────────────────────────
        private void DrawMainBusbar(Transaction tr, BlockTableRecord ms, BusBar busBar)
        {
            // 唯独一条贯通竖向母线 (从 Start 到 End，ToplogyBuilder 已经计算好长度)
            AddLine(tr, ms,
                new Point3d(busBar.Start.X, busBar.Start.Y, 0),
                new Point3d(busBar.End.X, busBar.End.Y, 0),
                LineWeight.LineWeight050);
        }

        // ── 主断路器行 ────────────────────────────────────────────────
        private void DrawMainBreakerRow(Transaction tr, BlockTableRecord ms, MainCircuitData main, ObjectId styleId)
        {
            double my = _config.MainBreakerY;
            double ty = my + _config.TextUpOffset;

            // 进线从左侧引入竖母线
            AddLine(tr, ms, new Point3d(_config.BusX - 15, my, 0), new Point3d(_config.BusX - _config.BreakerSymbolHalfWidth - 1, my, 0));
            AddLine(tr, ms, new Point3d(_config.BusX + _config.BreakerSymbolHalfWidth + 1, my, 0), new Point3d(_config.BusX + 15, my, 0));
            DrawBreakerSymbol(tr, ms, _config.BusX, my);

            // 主回路信息（固定列对齐）
            AddText(tr, ms,
                string.Format("{0:F1}kW", main.TotalKW),
                new Point3d(_config.ColPower, ty, 0), styleId);
            AddText(tr, ms, main.MainBreaker,
                new Point3d(_config.ColBreaker, ty, 0), styleId);
            AddText(tr, ms,
                string.Format("{0:F1}A", main.Current),
                new Point3d(_config.ColCurrent, ty, 0), styleId);
            AddText(tr, ms, main.MainCable,
                new Point3d(_config.ColCable, ty, 0), styleId);
            AddText(tr, ms, main.MainConduit,
                new Point3d(_config.ColPipe, ty, 0), styleId);

            // 标注 KD
            AddText(tr, ms,
                string.Format("KD={0:F2}", main.DemandFactor),
                new Point3d(_config.ColNo, my - _config.TextUpOffset - _config.TextHeight, 0), styleId);
        }

        // ── 单条支路 ──────────────────────────────────────────────────
        private void DrawBranch(Transaction tr, BlockTableRecord ms, Branch branch, ObjectId styleId)
        {
            double by = branch.Y;
            double ty = by + _config.TextUpOffset;
            double brkX = _config.BreakerSymbolX;

            // 回路水平线：左段（母线至断路器左侧）
            AddLine(tr, ms,
                new Point3d(_config.BusX, by, 0),
                new Point3d(brkX - _config.BreakerSymbolHalfWidth - 1, by, 0));

            // 回路水平线：右段（断路器右侧至回路终点）
            AddLine(tr, ms,
                new Point3d(brkX + _config.BreakerSymbolHalfWidth + 1, by, 0),
                new Point3d(_config.BranchLineLength, by, 0));

            // 断路器符号
            DrawBreakerSymbol(tr, ms, brkX, by);

            // ── 固定列文字 (严格按规范 X 坐标，绝对定位) ──────────
            AddText(tr, ms, branch.LoopNo,
                new Point3d(_config.ColNo, ty, 0), styleId);

            AddText(tr, ms, branch.Phase,
                new Point3d(_config.ColPhase, ty, 0), styleId);
                
            AddText(tr, ms, branch.Breaker,
                new Point3d(_config.ColBreaker, ty, 0), styleId);

            AddText(tr, ms, branch.Power,
                new Point3d(_config.ColPower, ty, 0), styleId);

            AddText(tr, ms, branch.Current,
                new Point3d(_config.ColCurrent, ty, 0), styleId);

            AddText(tr, ms, branch.Cable,
                new Point3d(_config.ColCable, ty, 0), styleId);

            AddText(tr, ms, branch.Pipe,
                new Point3d(_config.ColPipe, ty, 0), styleId);

            // 设备名称 (放置在最右侧，不再占用固定宽度)
            if (!string.IsNullOrEmpty(branch.DeviceName))
            {
               AddText(tr, ms, branch.DeviceName, new Point3d(_config.ColPipe + 20, ty, 0), styleId);
            }
        }

        // ── 断路器符号（X 型 + 竖向母线，高度动态按配置）────────────────
        private void DrawBreakerSymbol(Transaction tr, BlockTableRecord ms, double cx, double cy)
        {
            double h = _config.BreakerSymbolHalfHeight;
            double w = _config.BreakerSymbolHalfWidth;

            // 竖向线
            AddLine(tr, ms,
                new Point3d(cx, cy + h, 0),
                new Point3d(cx, cy - h, 0));

            // X 型斜线
            AddLine(tr, ms,
                new Point3d(cx - w, cy + w, 0),
                new Point3d(cx + w, cy - w, 0));
            AddLine(tr, ms,
                new Point3d(cx - w, cy - w, 0),
                new Point3d(cx + w, cy + w, 0));
        }

        // ── 基础绘图原语 ──────────────────────────────────────────────
        private ObjectId GetOrCreateTextStyle(Transaction tr, Database db)
        {
            var tst = tr.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
            if (tst.Has(STYLE_NAME)) return tst[STYLE_NAME];

            tst.UpgradeOpen();
            var tstr = new TextStyleTableRecord();
            tstr.Name = STYLE_NAME;
            // 使用宋体规避 ???? 乱码
            tstr.Font = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor("宋体", false, false, 134, 32);
            ObjectId id = tst.Add(tstr);
            tr.AddNewlyCreatedDBObject(tstr, true);
            return id;
        }

        private void AddLine(Transaction tr, BlockTableRecord ms,
            Point3d start, Point3d end,
            LineWeight lw = LineWeight.ByLayer)
        {
            var line = new Line(start, end)
            {
                Layer      = LAYER,
                LineWeight = lw
            };
            ms.AppendEntity(line);
            tr.AddNewlyCreatedDBObject(line, true);
        }

        private void AddText(Transaction tr, BlockTableRecord ms,
            string content, Point3d pos, ObjectId styleId)
        {
            if (string.IsNullOrEmpty(content)) return;

            // 替换平方符号，部分字体可能不支持²
            content = content.Replace("²", "2").Replace("?", "2");

            var text = new DBText
            {
                TextString     = content,
                Height         = _config.TextHeight,
                Layer          = LAYER,
                TextStyleId    = styleId,
                HorizontalMode = TextHorizontalMode.TextLeft,
                VerticalMode   = TextVerticalMode.TextBase,
                Position       = pos
            };
            ms.AppendEntity(text);
            tr.AddNewlyCreatedDBObject(text, true);
        }
    }
}
