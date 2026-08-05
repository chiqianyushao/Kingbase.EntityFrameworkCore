# Kingbase.EntityFrameworkCore

面向 `.NET 10 / EF Core 10` 的人大金仓 **KingbaseES** 数据库 Provider,附真实 **ABP 10.6 BookStore 实战样例**。

在 KingbaseES `V009R001C002B0014`(Oracle 兼容模式)+ `Kdbndp_V9 10.0.1.703` 上通过 **58 项 Release 回归(34 项真实库集成测试)**;ABP 样例另通过 **43 项真实库测试**,并按 `docs/EFCore10-KingbaseES-Compatibility-Report.md` 完成了 **369 项能力矩阵实测**。

> 状态:核心 CRUD / 查询 / 迁移已可试生产;EF Core 10 **全语法**支持尚未完成,见[当前限制](#当前限制)。

## 特性亮点

- **EF Core 10 / .NET 10 原生** Provider,基于原生 `Kdbndp_V9` ADO.NET 驱动。
- `UseKdbndp(...)` 即插即用,支持 `Auto / Postgres / Oracle` 三种兼容模式选项。
- 基础 CRUD、identity 回读、并发条件、批处理、`ExecuteDelete / ExecuteUpdate` 翻译。
- 常用 LINQ 翻译:字符串 / 日期部件 / 数学函数、聚合、集合运算、排序分页、Join / GroupBy / SelectMany、EF.Functions(Like / Random / Collate)。
- 连接 / 事务全链路:BeginTransaction、Savepoint、UseTransaction、执行策略(RetryOnFailure)、连接生命周期 API。
- Migration 全链路:历史表、幂等脚本、升级 / 回滚(含 `Migration.InitialDatabase`)。
- 真实 **ABP 10.6 + 样例** 组合验证:审计 / 软删除 / 乐观并发 / 复合键多对多 / ExtraProperties JSON / 仓储查询管线 / Web 主机 / Swagger。

## 仓库结构

```
src/Kingbase.EntityFrameworkCore/       Provider 实现(PackageId: Kingbase.EntityFrameworkCore 10.0.0-alpha.3)
test/Kingbase.EntityFrameworkCore.Tests/ Provider 回归测试(含 34 项真实库集成测试)
samples/AbpBookStore/                  ABP 10.6 BookStore 实战样例(标准 ABP 分层,独立 slnx)
docs/                                  EF Core 10 兼容性报告 + 能力矩阵实测报告(MD / HTML)
tools/  V9R1C10/  tmp/                 本地探测工程 / 金仓官方文档 / 探针脚本 —— 不纳入版本库
```

## 已验证环境

| 组件 | 版本 |
|---|---|
| .NET | 10.0.301 |
| EF Core | 10.0.9 |
| ABP Framework | 10.6.0 |
| Provider | Kingbase.EntityFrameworkCore 10.0.0-alpha.3 |
| ADO.NET | Kdbndp_V9 10.0.1.703 |
| KingbaseES | V009R001C002B0014,`database_mode=oracle` |

## 快速开始

```powershell
dotnet build Kingbase.EntityFrameworkCore.slnx -c Release
dotnet test  Kingbase.EntityFrameworkCore.slnx -c Release   # 离线也全绿(真实库测试自动跳过)
```

## 真实数据库集成测试

真实库测试通过环境变量控制,**未设置时自动跳过**(离线全绿),密码不写入源码:

```powershell
$env:KINGBASE_TEST_CONNECTION='Server=127.0.0.1;Port=54321;UID=system;PWD=<你的密码>;Database=efcore10_kingbase_dev;SSL Mode=Disable'
$env:KINGBASE_ADMIN_CONNECTION='Server=127.0.0.1;Port=54321;UID=system;PWD=<你的密码>;Database=template1;SSL Mode=Disable'
dotnet test Kingbase.EntityFrameworkCore.slnx -c Release
```

> 建议使用专用测试库:测试会 DROP 相关表后重建。

## ABP BookStore 实战样例

`samples/AbpBookStore/` 参考 ABP 官方 Acme.BookStore 的标准分层,数据层换成该 Provider,在真实 KingbaseES 上验证 ABP × EF Core 10 × KingbaseES 的组合场景。包含 42 项 EF 集成测试 + 1 项应用服务测试、`dotnet ef` 迁移与种子、可启动的 Web 主机与客户端。

```powershell
cd samples\AbpBookStore
$env:KINGBASE_TEST_CONNECTION='Server=...;Port=54321;UID=system;PWD=...;Database=abp_bookstore_dev;SSL Mode=Disable'
dotnet test AbpBookStore.slnx -c Release
```

详见 `samples/AbpBookStore/README.md`(含 ABP 10.6 的 API 差异与踩坑清单)。

## 兼容性实测报告

| 文档 | 内容 |
|---|---|
| `docs/EFCore10-KingbaseES-Compatibility-Report.md` | 约 400 项能力分析(§3–§20):实库确认 / EF10 新能力 / 查询翻译 / 模型映射 / 迁移 / 设计时 / 不支持清单 / 结论 |
| `docs/AbpBookStore-Capability-Results.md` · `.html` | 369 项能力矩阵在 ABP 样例中的逐项实测结果(181 项本样例实库重验 / 154 项 Provider 套件验证 / 其余沿用原报告) |
| `docs/AbpBookStore-Kingbase-Test-Report.md` | ABP 样例测试结果与风险区 |

## 当前限制

不能宣称"EF Core 10 所有语法均支持"。尚未实现 / 受数据库限制的项:

- EF JSON 聚合模型(complex / owned JSON、JSON `ExecuteUpdate`)未实现。
- Stored procedure CUD / SaveChanges 未实现。
- HiLo、统一 rowversion、Spatial / NetTopologySuite、NodaTime 未实现。
- 窗口函数 API,以及 `Aggregate / AggregateBy / Append / Chunk / CountBy / DistinctBy / MaxBy / MinBy / ...` 等 EF Core 10 无关系型翻译入口的项(不提供隐式客户端回退)。
- `AlterDatabaseOperation` 在线修改 collation 受数据库限制。
- 尚未验证真实主备故障转移、网络分区、大规模压力;Oracle / pg 双模式 CI 未建立。

## 相关文档

- `EFCORE10_KINGBASE_SOLUTION.md` — 适配方案与技术选型
- `docs/` — 兼容性报告、能力矩阵实测报告、样例测试报告
