using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;

namespace PDX_CSharp.Drawing
{
    /// <summary>
    /// 图层辅助工具：自动检测并创建 PDX 专用图层，处理图层锁定情况。
    /// </summary>
    public static class LayerHelper
    {
        /// <summary>
        /// 确保指定图层存在。若不存在则创建；若已存在且被锁定，则自动解锁。
        /// </summary>
        /// <param name="tr">当前事务</param>
        /// <param name="db">当前数据库</param>
        /// <param name="layerName">图层名称</param>
        public static void EnsureLayer(Transaction tr, Database db, string layerName)
        {
            var layerTable = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
            if (layerTable == null) return;

            if (layerTable.Has(layerName))
            {
                // 图层已存在：检查是否被锁定，若锁定则解锁
                var existingLayer = tr.GetObject(layerTable[layerName], OpenMode.ForWrite) as LayerTableRecord;
                if (existingLayer != null && existingLayer.IsLocked)
                {
                    existingLayer.IsLocked = false;
                }
            }
            else
            {
                // 创建新图层
                layerTable.UpgradeOpen();
                var newLayer = new LayerTableRecord
                {
                    Name     = layerName,
                    IsLocked = false,
                    IsOff    = false,
                    IsFrozen = false,
                    Color    = Color.FromColorIndex(ColorMethod.ByAci, 5) // 蓝色
                };
                layerTable.Add(newLayer);
                tr.AddNewlyCreatedDBObject(newLayer, true);
            }
        }
    }
}
