using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PDX_CSharp.Core.Interfaces;
using PDX_CSharp.Drawing;

namespace PDX_CSharp.Infrastructure.CAD
{
    /// <summary>
    /// AutoCAD 渲染器（实现 ICadRenderer）。
    ///
    /// 这是系统中**唯一**引用 AutoCAD SDK 命名空间的渲染器。
    /// 所有 AutoCAD 绘图操作集中在此处，模板层和应用层完全不知道 AutoCAD 的存在。
    ///
    /// 生命周期：
    ///   BeginDraw  → 开启 Transaction
    ///   Draw*      → 向 ModelSpace 追加图元
    ///   EndDraw    → Commit Transaction
    ///
    /// 使用方式：
    ///   var renderer = new AutoCadRenderer(db);
    ///   template.Render(topology, renderer);
    /// </summary>
    public class AutoCadRenderer : ICadRenderer
    {
        private const string LAYER      = "PDX_LAYER";
        private const string STYLE_NAME = "PDX_STYLE";

        private readonly Database _db;
        private Transaction       _tr;
        private BlockTableRecord  _ms;
        private ObjectId          _styleId;
        private double            _defaultTextHeight = 2.5;

        public AutoCadRenderer(Database db)
        {
            if (db == null) throw new ArgumentNullException("db");
            _db = db;
        }

        // ── ICadRenderer ────────────────────────────────────────────────

        public void BeginDraw()
        {
            LayerHelper.EnsureLayer(_tr == null
                ? _db.TransactionManager.StartTransaction()
                : _tr, _db, LAYER);

            _tr      = _db.TransactionManager.StartTransaction();
            _styleId = GetOrCreateTextStyle(_tr, _db);

            BlockTable bt = _tr.GetObject(_db.BlockTableId, OpenMode.ForRead) as BlockTable;
            _ms = _tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
        }

        public void EndDraw()
        {
            if (_tr != null)
            {
                _tr.Commit();
                _tr.Dispose();
                _tr = null;
            }
        }

        public void DrawLine(double x1, double y1, double x2, double y2, bool thick = false)
        {
            var line = new Line(new Point3d(x1, y1, 0), new Point3d(x2, y2, 0))
            {
                Layer      = LAYER,
                LineWeight = thick ? LineWeight.LineWeight050 : LineWeight.ByLayer
            };
            _ms.AppendEntity(line);
            _tr.AddNewlyCreatedDBObject(line, true);
        }

        public void DrawText(double x, double y, string text, double height = 0)
        {
            if (string.IsNullOrEmpty(text)) return;
            text = text.Replace("²", "2");

            var dbt = new DBText
            {
                TextString     = text,
                Height         = height > 0 ? height : _defaultTextHeight,
                Layer          = LAYER,
                TextStyleId    = _styleId,
                HorizontalMode = TextHorizontalMode.TextLeft,
                VerticalMode   = TextVerticalMode.TextBase,
                Position       = new Point3d(x, y, 0)
            };
            _ms.AppendEntity(dbt);
            _tr.AddNewlyCreatedDBObject(dbt, true);
        }

        public void DrawSymbol(string symbolType, double cx, double cy)
        {
            // 断路器符号：竖线 + X 型斜线
            if (symbolType == "breaker")
            {
                const double h = 4.0, w = 3.5;
                DrawLine(cx, cy + h, cx, cy - h);                       // 竖线
                DrawLine(cx - w, cy + w, cx + w, cy - w);               // \斜线
                DrawLine(cx - w, cy - w, cx + w, cy + w);               // /斜线
            }
            // 预留：其他符号类型按 symbolType 扩展
        }

        // ── 私有辅助 ─────────────────────────────────────────────────────

        private static ObjectId GetOrCreateTextStyle(Transaction tr, Database db)
        {
            var tst = tr.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
            if (tst.Has(STYLE_NAME)) return tst[STYLE_NAME];

            tst.UpgradeOpen();
            var tstr = new TextStyleTableRecord();
            tstr.Name = STYLE_NAME;
            tstr.Font = new Autodesk.AutoCAD.GraphicsInterface.FontDescriptor(
                "宋体", false, false, 134, 32);
            ObjectId id = tst.Add(tstr);
            tr.AddNewlyCreatedDBObject(tstr, true);
            return id;
        }
    }
}
