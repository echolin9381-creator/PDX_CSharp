using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using PDX_CSharp.Services;

// 注册 AutoCAD 命令
[assembly: CommandClass(typeof(PDX_CSharp.Commands.PdxCommands))]

namespace PDX_CSharp.Commands
{
    /// <summary>
    /// PDX 命令入口层。
    /// 所有 AutoCAD 命令在此注册，职责单一：验证当前状态后委托给 Service 层。
    /// </summary>
    public class PdxCommands
    {
        // ─── 主命令 ────────────────────────────────────────────────────────────

        /// <summary>
        /// PDX — 配电箱系统图自动生成
        /// </summary>
        [CommandMethod("PDX_CS", CommandFlags.Modal)]
        public void RunPDX()
        {
            var service = new PdxService();
            service.Execute();
        }

        // ─── 预留命令（V2 扩展）────────────────────────────────────────────────

        /// <summary>
        /// PDX_RECALC — 局部重新计算（V2 预留）
        /// </summary>
        [CommandMethod("PDX_RECALC", CommandFlags.Modal)]
        public void RunRecalc()
        {
            var ed = Application.DocumentManager.MdiActiveDocument?.Editor;
            ed?.WriteMessage("\n[PDX_RECALC] 此命令在 V2 版本实现，敬请期待。\n");
        }

        /// <summary>
        /// PDX_EXPORT — 导出 Excel（V2 预留）
        /// </summary>
        [CommandMethod("PDX_EXPORT", CommandFlags.Modal)]
        public void RunExport()
        {
            var ed = Application.DocumentManager.MdiActiveDocument?.Editor;
            ed?.WriteMessage("\n[PDX_EXPORT] 此命令在 V2 版本实现，敬请期待。\n");
        }

        /// <summary>
        /// PDX_BALANCE — 三相平衡优化（V2 预留）
        /// </summary>
        [CommandMethod("PDX_BALANCE", CommandFlags.Modal)]
        public void RunBalance()
        {
            var ed = Application.DocumentManager.MdiActiveDocument?.Editor;
            ed?.WriteMessage("\n[PDX_BALANCE] 此命令在 V2 版本实现，敬请期待。\n");
        }

        /// <summary>
        /// PDX_EDIT — 图面编辑（V2 预留）
        /// </summary>
        [CommandMethod("PDX_EDIT", CommandFlags.Modal)]
        public void RunEdit()
        {
            var ed = Application.DocumentManager.MdiActiveDocument?.Editor;
            ed?.WriteMessage("\n[PDX_EDIT] 此命令在 V2 版本实现，敬请期待。\n");
        }
    }
}
