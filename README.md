# PDX_CSharp (配电箱系统图自动生成)

PDX_CSharp 是一个用于 AutoCAD 的 .NET (C#) 插件。该插件能够根据内置或外部规则，在 AutoCAD 中自动绘制标准的配电箱电气系统图表。

## 核心功能

*   **自动生成系统图**：输入 `PDX_CS` 命令即可启动绘制。
*   **智能规则匹配**：内置断路器选型、主电缆截面和穿管规则，根据用电设备功率自动计算负载电流、选择合适的断路器及线缆。
*   **三相平衡分布**：根据回路序号自动对 L1、L2、L3 进行均摊和相序分配。
*   **支持自测试**：附带脱离 AutoCAD 环境的 `SelfTest` 程序，通过控制台即可快速验证布局坐标、数学计算和规则匹配的正确性。

## 未来版本 (V2 预留功能)

规划中的 V2 版本包含以下增强命令：
*   `PDX_RECALC`：支持局部修改后系统图的快速重新计算。
*   `PDX_EXPORT`：支持提取图纸数据并导出至 Excel 报表。
*   `PDX_BALANCE`：基于更精确的负荷数据执行三相平衡智能优化。
*   `PDX_EDIT`：提供可视化的图面编辑面板。

## 系统要求

*   **AutoCAD 版本**：推荐使用 AutoCAD 2018 (由于引用了 AutoCAD 2018 SDK)。
*   **.NET 运行环境**：.NET Framework 4.7 或以上版本。
*   **操作系统**：Windows x64。

## 项目结构

```
PDX_CSharp/
├── src/
│   ├── Commands/   # AutoCAD 命令注册入口层，如 PDX_CS
│   ├── Services/   # 核心生成服务逻辑
│   ├── Drawing/    # AutoCAD 坐标计算及图元绘制层
│   ├── Rules/      # 电气设计规则引擎
│   ├── Models/     # 数据模型和实体对象
│   └── Calculation/# 负荷、电流匹配及管线计算
├── resources/
│   └── pdx_rules.json # 外部可配置的断路器/电缆规则
├── PDX_CSharp.csproj  # 项目文件
├── SelfTest.cs        # 独立运行的规则与布局单元测试文件
├── build.bat          # 编译脚本
└── run_test.bat       # 测试执行脚本
```

## 编译及使用

### 构建项目
可以直接使用提供的 bat 脚本编译项目，或者在拥有 MSBuild 的环境中运行：
```cmd
build.bat
```
编译成功后，生成的类库 (例如 `PDX.dll`) 将存放于 `bin/Debug` 或 `bin/Release` 目录中。

### 加载插件（AutoCAD）
1. 打开 AutoCAD。
2. 输入 `NETLOAD` 命令。
3. 在弹出的对话框中，找到在 `bin` 目录下生成的 `PDX.dll` 文件并加载。

### 运行主要命令
加载成功后，在命令行中输入：
```
PDX_CS
```
即可开始自动生成配电箱系统图。

### 运行单元测试
无需启动 AutoCAD，可以直接运行自测试来验证数学逻辑：
```cmd
run_test.bat
```
或直接运行编译出的 `SelfTest.exe`。
