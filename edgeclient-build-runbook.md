# IIoT.EdgeClient Build Runbook

## 目标

1) 先完成源码级排障，保证非 WPF 层可稳定构建。  
2) 将 WPF 层标记为可跳过构建，便于环境异常时隔离。  
3) 当你允许环境修复后再执行全量恢复与联调启动。  

## 一、先做只读核查

- 运行核查脚本：
  - `./scripts/edgeclient-graph-and-conflict-check.ps1 -OutputPath ./edgeclient-graph-check-report.md`
  - 输出内容：项目引用图、TargetFramework、UseWPF 项目、冲突标记扫描、同名符号扫描。
- 只读核查用于确认：
  - `csproj` 是否齐全、引用是否闭环；
  - 是否还残留 `<<<<<<<` / `=======` / `>>>>>>>`；
  - 是否出现明显同名符号风险（后续可人工核对）。

## 二、非 WPF 基线编译顺序（无环境改动路径）

在 `IIoT.EdgeClient` 目录运行（按你当前约束，不改机器 SDK）：

1. `dotnet restore IIoT.EdgeClient.slnx`
2. `dotnet build src/Core/IIoT.Edge.Domain/IIoT.Edge.Domain.csproj --no-restore`
3. `dotnet build src/Shared/IIoT.Edge.SharedKernel/IIoT.Edge.SharedKernel.csproj --no-restore`
4. `dotnet build src/Infrastructure/IIoT.Edge.Infrastructure.Integration/IIoT.Edge.Infrastructure.Integration.csproj --no-restore`
5. `dotnet build src/Infrastructure/IIoT.Edge.Infrastructure.Persistence.Dapper/IIoT.Edge.Infrastructure.Persistence.Dapper.csproj --no-restore`
6. `dotnet build src/Infrastructure/IIoT.Edge.Infrastructure.Persistence.EfCore/IIoT.Edge.Infrastructure.Persistence.EfCore.csproj --no-restore`
7. `dotnet build src/Infrastructure/IIoT.Edge.Infrastructure.DeviceComm/IIoT.Edge.Infrastructure.DeviceComm.csproj --no-restore`
8. `dotnet build src/Runtime/IIoT.Edge.Runtime/IIoT.Edge.Runtime.csproj --no-restore`
9. `dotnet build src/Runtime/IIoT.Edge.Runtime.DataPipeline/IIoT.Edge.Runtime.DataPipeline.csproj --no-restore`
10. `dotnet build src/Runtime/IIoT.Edge.Runtime.Scan/IIoT.Edge.Runtime.Scan.csproj --no-restore`
11. `dotnet build src/Application/IIoT.Edge.Application/IIoT.Edge.Application.csproj --no-restore`

如需更快验证，可先做 `dotnet build IIoT.EdgeClient.slnx --no-restore -p:SKIP_EDGE_WPF_PROJECTS=true`，确认非 WPF 业务层是否先通过。

## 三、WPF 相关隔离构建开关

已添加项目级临时开关（默认关闭，不影响原生行为）：

- `SKIP_EDGE_WPF_PROJECTS=true`

触发后，以下 WPF 项目会被设置为 `ExcludeFromBuild=true`（跳过编译）：

- `IIoT.Edge.Shell`
- `IIoT.Edge.Presentation.Shell`
- `IIoT.Edge.Presentation.Navigation`
- `IIoT.Edge.Presentation.Panels`
- `IIoT.Edge.UI.Shared`
- `IIoT.Edge.TestSimulator`

调用示例：

- `dotnet build --no-restore IIoT.EdgeClient.slnx -p:SKIP_EDGE_WPF_PROJECTS=true`

说明：
- 该开关仅用于排障和临时隔离。
- 默认（不传该参数）行为不变，仍然保留 `net10.0-windows` 与 `UseWPF=true`。

## 四、环境层（仅在你同意后执行）

当前常见阻塞点：`MSB4276`、`GetTargetFrameworks / Workload*` 解析失败。

排查建议：

1. `dotnet --info`
2. `dotnet workload list`
3. `dotnet --version`
4. `dotnet list package`（必要时）

通常修复路径（最小影响）：

- 尝试 `dotnet workload update`
- 或 `dotnet workload repair`
- 同步后执行 `dotnet restore IIoT.EdgeClient.slnx` + 全量 `dotnet build IIoT.EdgeClient.slnx`

## 五、环境修复后的收口

1. `dotnet restore IIoT.EdgeClient.slnx`
2. `dotnet build IIoT.EdgeClient.slnx`
3. `dotnet run --project src/Edge/IIoT.Edge.Shell/IIoT.Edge.Shell.csproj`
4. `dotnet run --project src/Edge/IIoT.Edge.TestSimulator/IIoT.Edge.TestSimulator.csproj`

如果希望清理临时开关配置：

- 保留（推荐）：该开关仅在文档约定场景使用。
- 清理：直接移除 csproj 中 `SKIP_EDGE_WPF_PROJECTS` 条件块（默认构建不受影响）。
