using System;
using System.Collections.Generic;
using PDX_CSharp.Core.Interfaces;
using PDX_CSharp.Core.Models;

namespace PDX_CSharp.Application.Services
{
    /// <summary>
    /// 直接录入路径工作流服务（路径 B 编排器）。
    ///
    /// 完整流程：
    ///   CreateEmptyModel()  → 生成空结构 DiagramModel
    ///   Generate()          → 计算 → 布局 → 渲染
    ///
    /// 适用场景：操作员不使用 Excel，直接在 CAD 内或对话框中录入数据。
    /// </summary>
    public class DirectEntryWorkflowService
    {
        private readonly DiagramGenerationService _genService;

        public DirectEntryWorkflowService(DiagramGenerationService genService)
        {
            if (genService == null) throw new ArgumentNullException("genService");
            _genService = genService;
        }

        // ── 公开 API ──────────────────────────────────────────────────────

        /// <summary>
        /// 步骤 B1：创建空结构 DiagramModel。
        /// 调用方（PdxService / PdxCommands）向返回模型填写回路数据后，
        /// 调用 Generate() 完成绘图。
        /// </summary>
        /// <param name="def">图样模板定义</param>
        /// <param name="branchCount">出线回路数量</param>
        /// <param name="demandFactor">主开关需求系数 KD（默认 0.8）</param>
        /// <returns>骨架 DiagramModel（所有字段为初始值，等待填写）</returns>
        public DiagramModel CreateEmptyModel(
            TemplateDefinition def,
            int    branchCount,
            double demandFactor = 0.8)
        {
            if (def == null)        throw new ArgumentNullException("def");
            if (branchCount <= 0)   throw new ArgumentOutOfRangeException("branchCount", "回路数量必须 > 0。");

            string[] phases = { "L1", "L2", "L3" };

            var model = new DiagramModel
            {
                Template   = def,
                Layout     = LayoutConfig.Default,
                MainSwitch = new MainSwitchData { DemandFactor = Math.Max(0.1, demandFactor) }
            };

            for (int i = 0; i < branchCount; i++)
            {
                model.Branches.Add(new BranchCircuit
                {
                    Index      = i + 1,
                    Phase      = phases[i % 3],
                    DeviceCount = 1
                    // UnitPower / BreakerModel / Cable / Conduit 等待调用方填写
                });
            }

            return model;
        }

        /// <summary>
        /// 步骤 B2：调用方填写回路数据后，执行完整生成流程。
        /// </summary>
        /// <param name="model">已填写输入字段的 DiagramModel</param>
        /// <param name="renderer">CAD 渲染器</param>
        public void Generate(DiagramModel model, ICadRenderer renderer)
        {
            if (model    == null) throw new ArgumentNullException("model");
            if (renderer == null) throw new ArgumentNullException("renderer");

            _genService.Generate(model, renderer);
        }

        /// <summary>
        /// 快捷方法：一步完成空模型创建 + 批量数据填写 + 生成。
        /// 适合脚本/自动化场景（SelfTest 端对端测试调用此方法）。
        /// </summary>
        /// <param name="def">模板定义</param>
        /// <param name="powerList">各回路单台功率（kW）列表，长度决定回路数</param>
        /// <param name="nameList">各回路设备名称（可空）</param>
        /// <param name="renderer">CAD 渲染器</param>
        /// <param name="demandFactor">需求系数 KD</param>
        /// <returns>已完成计算的 DiagramModel</returns>
        public DiagramModel QuickGenerate(
            TemplateDefinition    def,
            List<double>          powerList,
            List<string>          nameList,
            ICadRenderer          renderer,
            double                demandFactor = 0.8)
        {
            if (def == null || powerList == null || powerList.Count == 0)
                throw new ArgumentNullException("def / powerList");

            DiagramModel model = CreateEmptyModel(def, powerList.Count, demandFactor);

            for (int i = 0; i < powerList.Count; i++)
            {
                model.Branches[i].UnitPower  = Math.Max(0, powerList[i]);
                if (nameList != null && i < nameList.Count)
                    model.Branches[i].DeviceName = nameList[i] ?? "";
            }

            Generate(model, renderer);
            return model;
        }
    }
}
