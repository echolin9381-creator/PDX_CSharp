using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PDX_CSharp.Models;

namespace PDX_CSharp.Drawing
{
    /// <summary>
    /// 列式排版绘图引擎 - 严格固定列坐标，符合工业配电系统图标准。
    ///
    /// 坐标体系 (单位 mm):
    ///   (0,0) = 主母线左端
    ///   X 向右, Y 向下为负
    ///
    /// 固定列 X 坐标:
    ///   ColNo=5  ColPhase=20  ColLoad=35  ColBreaker=60
    ///   ColCurrent=85  ColCable=110  ColConduit=140
    ///
    /// 回路 Y = -20 - 12 * index
    /// </summary>
    public class DrawingEngine
    {
        // ── 主控尺寸 ──────────────────────────────────────────────────
        private const double BusX        =   0.0;  // 竖向主母线 X = 0
        private const double LoopStartY  = -20.0;  // 第一条回路 Y
        private const double LoopYStep   = -12.0;  // 每条回路步进
        private const double MainBrkY    =  -8.0;  // 主断路器行 Y

        private const double CircuitEndX = 200.0;  // 回路水平线右端 (向右 200mm，容纳所有文字)
        private const double BrkSymX     =  40.0;  // 断路器符号中心 X (调整位置以适应无左边框)
        private const double BrkSymHalf  =   4.0;  // 断路器符号半高 (8mm)
        private const double BrkSymHW    =   3.5;  // 断路器 X 半宽

        // ── 固定列坐标 (规范强制) ─────────────────────────────────────
        private const double ColNo      =   5.0;
        private const double ColPhase   =  20.0;
        private const double ColLoad    =  35.0;
        private const double ColBreaker =  65.0;  // 65: clear of breaker symbol right edge (60.5)
        private const double ColCurrent =  93.0;
        private const double ColCable   = 118.0;
        private const double ColConduit = 148.0;

        private const double TextH      =   2.5;   // 文字高度
        private const double TextUp     =   1.5;   // 文字在线上方偏移
        private const string LAYER      = "PDX_LAYER";
        private const string STYLE_NAME = "PDX_STYLE";

        // ── 公开入口 ──────────────────────────────────────────────────
        public void Draw(Database db, List<LoopData> loops, MainCircuitData main)
        {
            ValidateLayout(loops);

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    LayerHelper.EnsureLayer(tr, db, LAYER);
                    ObjectId styleId = GetOrCreateTextStyle(tr, db);

                    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                    BlockTableRecord ms = tr.GetObject(
                        bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    DrawMainBusbar(tr, ms, loops.Count);
                    DrawMainBreakerRow(tr, ms, main, styleId);

                    for (int i = 0; i < loops.Count; i++)
                        DrawLoop(tr, ms, loops[i], i, styleId);

                    tr.Commit();
                }
                catch { throw; }
            }
        }

        // ── 自检机制 ──────────────────────────────────────────────────
        private static void ValidateLayout(List<LoopData> loops)
        {
            if (loops == null || loops.Count == 0)
                throw new InvalidOperationException("回路列表不能为空。");
            if (loops.Count > 60)
                throw new InvalidOperationException(
                    string.Format("回路数 {0} 超出上限 60。", loops.Count));

            // 检查列坐标：各列间距是否足够（最小 12mm）
            double[] cols = { ColNo, ColPhase, ColLoad, ColBreaker, ColCurrent, ColCable, ColConduit };
            for (int i = 1; i < cols.Length; i++)
            {
                if (cols[i] - cols[i - 1] < 12.0)
                    throw new InvalidOperationException(
                        string.Format("列 {0} 与列 {1} 间距不足 12mm，请检查坐标配置。", i, i + 1));
            }

            // 检查最右列是否超出支路线范围
            if (ColConduit + 20.0 > CircuitEndX)
                throw new InvalidOperationException("最右列文字超出主母线引出的 200mm 水平线范围。");
        }

        // ── 主母线 ────────────────────────────────────────────────────
        private void DrawMainBusbar(Transaction tr, BlockTableRecord ms, int loopCount)
        {
            double capY = LoopStartY + (loopCount - 1) * LoopYStep; // 最后一条回路的下方一点
            
            // 唯独一条贯通竖向母线
            AddLine(tr, ms,
                new Point3d(BusX, 0, 0),
                new Point3d(BusX, capY - 6.0, 0),
                LineWeight.LineWeight050);
        }

        // ── 主断路器行 ────────────────────────────────────────────────
        private void DrawMainBreakerRow(Transaction tr, BlockTableRecord ms, MainCircuitData main, ObjectId styleId)
        {
            double ty = MainBrkY + TextUp;

            // 进线从左侧引入竖母线
            AddLine(tr, ms, new Point3d(BusX - 15, MainBrkY, 0), new Point3d(BusX - BrkSymHW - 1, MainBrkY, 0));
            AddLine(tr, ms, new Point3d(BusX + BrkSymHW + 1, MainBrkY, 0), new Point3d(BusX + 15, MainBrkY, 0));
            DrawBreakerSymbol(tr, ms, BusX, MainBrkY);

            // 主回路信息（固定列对齐）
            AddText(tr, ms,
                string.Format("{0:F1}kW", main.TotalKW),
                new Point3d(ColLoad, ty, 0), styleId);
            AddText(tr, ms, main.MainBreaker,
                new Point3d(ColBreaker, ty, 0), styleId);
            AddText(tr, ms,
                string.Format("{0:F1}A", main.Current),
                new Point3d(ColCurrent, ty, 0), styleId);
            AddText(tr, ms, main.MainCable,
                new Point3d(ColCable, ty, 0), styleId);
            AddText(tr, ms, main.MainConduit,
                new Point3d(ColConduit, ty, 0), styleId);

            // 标注 KD
            AddText(tr, ms,
                string.Format("KD={0:F2}", main.DemandFactor),
                new Point3d(ColNo, MainBrkY - TextUp - TextH, 0), styleId);
        }

        // ── 单条回路 ──────────────────────────────────────────────────
        private void DrawLoop(Transaction tr, BlockTableRecord ms, LoopData loop, int index, ObjectId styleId)
        {
            double loopY = LoopStartY + index * LoopYStep;  // Y = -20 - 12*index
            double ty    = loopY + TextUp;                   // 文字上移偏移

            // 回路水平线：左段（母线至断路器左侧）
            AddLine(tr, ms,
                new Point3d(BusX, loopY, 0),
                new Point3d(BrkSymX - BrkSymHW - 1, loopY, 0));

            // 回路水平线：右段（断路器右侧至回路终点）
            AddLine(tr, ms,
                new Point3d(BrkSymX + BrkSymHW + 1, loopY, 0),
                new Point3d(CircuitEndX, loopY, 0));

            // 断路器符号
            DrawBreakerSymbol(tr, ms, BrkSymX, loopY);

            // ── 固定列文字 (严格按规范 X 坐标，不允许漂移) ──────────
            // Col 1: 回路编号
            AddText(tr, ms, loop.LoopNo.ToString("D2"),
                new Point3d(ColNo, ty, 0), styleId);

            // Col 2: 相序
            AddText(tr, ms, loop.Phase,
                new Point3d(ColPhase, ty, 0), styleId);

            // Col 3: 负荷（单设备 or 多设备）
            AddText(tr, ms, FormatLoad(loop),
                new Point3d(ColLoad, ty, 0), styleId);

            // Col 4: 断路器型号
            AddText(tr, ms, loop.Breaker,
                new Point3d(ColBreaker, ty, 0), styleId);

            // Col 5: 计算电流
            AddText(tr, ms,
                string.Format("{0:F1}A", loop.Current),
                new Point3d(ColCurrent, ty, 0), styleId);

            // Col 6: 电缆规格
            AddText(tr, ms, loop.Cable,
                new Point3d(ColCable, ty, 0), styleId);

            // Col 7: 穿管规格
            AddText(tr, ms, loop.Conduit,
                new Point3d(ColConduit, ty, 0), styleId);

            // 将设备名称放在最后，不占用前面的宽度
            if (!string.IsNullOrEmpty(loop.DeviceName))
            {
               AddText(tr, ms, loop.DeviceName, new Point3d(ColConduit + 20, ty, 0), styleId); // 放电缆后面
            }
        }

        // ── 断路器符号（X 型 + 竖向母线，8mm 高）────────────────────
        private void DrawBreakerSymbol(Transaction tr, BlockTableRecord ms, double cx, double cy)
        {
            // 竖向线（8mm 高）
            AddLine(tr, ms,
                new Point3d(cx, cy + BrkSymHalf, 0),
                new Point3d(cx, cy - BrkSymHalf, 0));

            // X 型斜线
            AddLine(tr, ms,
                new Point3d(cx - BrkSymHW, cy + BrkSymHW, 0),
                new Point3d(cx + BrkSymHW, cy - BrkSymHW, 0));
            AddLine(tr, ms,
                new Point3d(cx - BrkSymHW, cy - BrkSymHW, 0),
                new Point3d(cx + BrkSymHW, cy + BrkSymHW, 0));
        }

        // ── 负荷格式化：单设备 / 多设备 / 备用 ──────────────────────
        private static string FormatLoad(LoopData loop)
        {
            if (loop.LoadKW <= 0)
                return "备用";
            if (loop.DeviceCount > 1)
                return string.Format("{0}x{1:F1}kW", loop.DeviceCount, loop.UnitLoadKW);
            return string.Format("{0:F1}kW", loop.LoadKW);
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
                Height         = TextH,
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
