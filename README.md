# PDX_CSharp — AI 驱动多图样 CAD 图样生成平台

AutoCAD .NET (C#) 插件，实现**AI分析 + 模板引擎 + 电气计算引擎 + CAD 渲染引擎**的工业级配电图自动生成平台。

---

## 系统要求

| 项目 | 要求 |
|------|------|
| AutoCAD | 2018（及以上） |
| .NET Framework | 4.7+ |
| 操作系统 | Windows x64 |

---

## 快速开始

```cmd
# 编译
do_build.bat          → bin\Release\PDX.dll

# 单元测试（无需 AutoCAD）
run_test.bat          → 46 passed, 0 failed
```

**加载插件（AutoCAD 内）：**
1. `NETLOAD` → 选择 `bin\Release\PDX.dll`
2. 执行命令

---

## AutoCAD 命令

| 命令 | 说明 |
|------|------|
| `PDX_CS` | 旧版配电箱系统图（兼容保留） |
| `PDXNEW` | **新** 直接录入路径：选模板 → AI分析 → 录入数据 → 自动计算 → 生成图样 |
| `PDXEXCEL` | **新** Excel路径第一步：生成 CSV 数据模板 |
| `PDXIMPORT` | **新** Excel路径第二步：导入填写好的 CSV → 自动计算 → 生成图样 |

---

## 平台架构（V2）

```
两条操作路径：
  路径 A (Excel): PDXEXCEL → 填写 CSV → PDXIMPORT
  路径 B (直接): PDXNEW → 在 CAD 内逐路录入

统一生成流程（两条路径共用）：
  AI 分析 → 电气计算 → 布局计算 → CAD 渲染
```

### 分层结构

```
src/
├── Core/
│   ├── Models/          DiagramModel, TemplateDefinition, LayoutConfig, TopologyModel
│   ├── Interfaces/      IDiagramTemplate, ICalculationEngine, ICadRenderer,
│   │                    IExcelTemplateBuilder, IExcelImporter, ITemplateAnalyzer
│   └── Calculation/     ElectricalCalculationEngine（纯电气，独立于布局）
│
├── Templates/
│   ├── VerticalBusTemplate.cs   配电箱系统图（完整实现）
│   ├── DualBusTemplate.cs       双母线图（架构存根）
│   └── TemplateRegistry.cs      插件化工厂注册表
│
├── Infrastructure/
│   ├── AI/      MockAiAnalyzer（离线）/ GptAnalyzer（存根，接入后0改动）
│   ├── Excel/   CsvExcelTemplateBuilder / CsvExcelImporter（RFC4180）
│   └── CAD/     AutoCadRenderer（唯一 AutoCAD 依赖）/ ConsoleCadRenderer（测试用）
│
├── Application/Services/
│   ├── DiagramGenerationService   主编排（AI→计算→布局→渲染）
│   ├── ExcelWorkflowService       路径 A 编排
│   └── DirectEntryWorkflowService 路径 B 编排
│
├── Commands/    PdxCommands（命令注册，调用 Application 层）
├── Services/    PdxService（旧版兼容入口）
├── Drawing/     DrawingEngine（旧版绘图，V1 保留）
├── Rules/       RuleEngine（断路器/电缆/穿管规则匹配）
├── Calculation/ CalculationEngine（旧版，V1 保留）
└── Models/      旧版数据模型（V1 保留）
```

---

## 电气计算能力

| 计算项 | 自动完成 |
|-------|---------|
| 单相/三相电流 | ✅ |
| 回路总功率 | ✅ |
| 主回路总负荷（KD 需求系数）| ✅ |
| 断路器型号自动选型（C6~C250）| ✅ |
| 电缆截面自动选型 | ✅ |
| 穿管规格自动选型 | ✅ |
| 三相平衡分配 | ✅ |

---

## AI 模块说明

当前使用 **MockAiAnalyzer**（离线，无需网络）。

接入真实 GPT 仅需修改 `GptAnalyzer.cs` 中的 `Analyze()` 方法并提供 ApiKey，**平台其余代码 0 改动**。

> AI 在本平台中只负责「解析图样字段结构」，电气计算和 CAD 绘制均由平台自身完成。

---

## 测试

```cmd
run_test.bat
```

测试分两组，无需启动 AutoCAD：

| 测试组 | 内容 |
|-------|------|
| T1–T8（旧引擎） | 列间距、Y坐标、断路器规则匹配、三相平衡 |
| T-P1–T-P10（平台层）| 电气公式、模板注册、AI分析、CSV读写、ConsoleCad、端对端路径A/B |

---

## 扩展新图样模板

1. 新建 `src/Templates/YourTemplate.cs`，实现 `IDiagramTemplate` 接口
2. 在 `TemplateRegistry.cs` 中注册工厂函数
3. 在 `TemplateDefinition.cs` 中添加静态工厂方法
4. 在 `MockAiAnalyzer.cs` 中添加对应预设（或接入真实 AI）

无需修改命令层、计算层或渲染层。

---

## V3 规划（预留命令）

- `PDX_RECALC` — 局部修改后快速重算
- `PDX_EXPORT` — 图纸数据导出 Excel
- `PDX_BALANCE` — 精确三相平衡优化
- `PDX_EDIT` — 可视化图面编辑面板
