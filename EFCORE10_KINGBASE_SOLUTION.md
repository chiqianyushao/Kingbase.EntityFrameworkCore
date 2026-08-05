# KingbaseES V9R1C10 的 EF Core 10 适配方案

## 核心结论

现有 `Kdbndp.EntityFrameworkCore.KingbaseES` 不能通过替换 EF Core DLL 的方式升级到 EF Core 10。V9R1C10 官方文档只声明了 EF Core 2.0、3.0、5.0、6.0、7.0 方言包，而 EF Core Provider 与 EF Core 主版本强耦合。

应建设 `.NET 10 + EF Core 10` 原生关系型 Provider：

- 首版只支持 KingbaseES `pg` 兼容模式。
- ADO.NET 继续使用 `Kdbndp`，但先完成 .NET 10 兼容认证和修复。
- Provider 参考 Npgsql EF Core 10 的架构，不直接依赖其内部实现。
- 对外保留 `UseKdbndp(...)`、`DbContext`、LINQ、Migration 和 Database First 用法。
- 用 EF Core 官方关系型 specification tests 和真实 KingbaseES 集成测试证明兼容。

## 文档调研结果

- `V9R1C10/03_应用开发/KingbaseES客户端编程接口指南.pdf`
  - PDF 第 1035 页附近说明 ADO.NET 驱动为 `Kdbndp.dll`。
  - PDF 第 1088 页附近说明 EF Core Provider 为 `Kdbndp.EntityFrameworkCore.KingbaseES`。
  - 文档公开的最高方言包为 `.NET 7 / EF Core 7`。
  - 示例 API 为 `optionsBuilder.UseKdbndp(connectionString)`。
- `V9R1C10/03_应用开发/KingbaseES应用开发指南.pdf`
  - PDF 第 3785 页附近说明默认兼容模式是 Oracle，`--dbmode=pg` 才是 PostgreSQL 兼容模式。
  - 文档包含 JSON/JSONB、数组、序列、identity、`RETURNING`、`ON CONFLICT`、范围类型和事务能力。
- `V9R1C10/11_版本说明/金仓数据库管理系统KingbaseES_V9版本说明书.pdf`
  - V009R001C010 增加了多种兼容语法，但数据库兼容语法不能代替 EF Core 查询翻译、更新管线、Migration 和设计时服务。

## 为什么必须固定 pg 模式

- 四种模式在标识符、空字符串、布尔值、分页、序列、日期运算和 DDL 上语义不同。
- 同一 Provider 动态生成四套方言，会破坏模型快照、迁移脚本和查询缓存的一致性。
- 建立连接后应检测模式；非 `pg` 模式默认快速失败。
- 其他模式如需支持，应分别开发独立 Provider 或方言包。

## “支持所有 EF Core 10 语法”的定义

必须覆盖：

- 公共关系型模型 API：实体、复杂类型、拥有类型、继承、影子属性、值转换、并发标记、默认值、计算列、索引、约束和序列。
- 标准 LINQ：筛选、投影、排序、分页、连接、分组、聚合、集合导航、相关子查询、集合运算、`Any/All/Contains`、`First/Single`、`Distinct` 和 `SelectMany`。
- 跟踪、无跟踪、身份解析以及 Include 对应的查询行为。
- `SaveChanges` 的增删改、级联、批处理、数据库生成值、乐观并发和事务。
- `ExecuteUpdate`、`ExecuteDelete`、原始 SQL，以及数据库可等价表达的存储过程映射。
- Migration：建库、schema、表、列、键、索引、序列、identity、默认值、计算列、重命名、注释和幂等脚本。
- Database First：表、视图、列、键、索引、序列、类型、默认值和注释。
- 同步/异步、取消令牌、保存点、连接池、重试和故障转移。
- EF Core 10 的 JSON complex types、named query filters、新 LINQ 翻译、批量更新表达式和 primitive collections。

数据库无法等价表达时：

- 能等价转换的必须服务端翻译。
- 只允许 EF Core 规则允许的最终投影客户端计算。
- 无法保持语义时在查询编译期抛出清晰异常，禁止静默改变结果。
- KingbaseES 专有能力通过 `EF.Functions` 或 Provider 扩展 API 提供。

“全部支持”不包括 SQL Server、SQLite、Cosmos 或 Npgsql 的专有 API。

## 推荐项目结构

```text
src/
  Kingbase.EntityFrameworkCore/
  Kingbase.EntityFrameworkCore.Design/
  Kingbase.EntityFrameworkCore.NetTopologySuite/
  Kingbase.EntityFrameworkCore.NodaTime/
test/
  Kingbase.EntityFrameworkCore.UnitTests/
  Kingbase.EntityFrameworkCore.FunctionalTests/
  Kingbase.EntityFrameworkCore.Design.Tests/
  Kingbase.EntityFrameworkCore.Migrations.Tests/
  Kingbase.EntityFrameworkCore.Specification.Tests/
  Kingbase.EntityFrameworkCore.CompatibilityTests/
samples/
  Kingbase.EFCore10.Sample/
eng/
  docker/
  scripts/
```

Provider `10.0.x` 只依赖同一补丁线的 `Microsoft.EntityFrameworkCore.Relational 10.0.x`。

## API 兼容目标

旧代码保持不变：

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseKdbndp(connectionString));
```

增加可选配置：

```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseKdbndp(connectionString, kingbase =>
    {
        kingbase.SetPostgresCompatibilityMode();
        kingbase.MigrationsHistoryTable("__EFMigrationsHistory");
        kingbase.EnableRetryOnFailure();
    }));
```

现有项目迁移目标是只替换 NuGet 包，实体、`DbContext`、LINQ 和迁移命令不变。

## 核心实现模块

### 1. Kdbndp 驱动层

必须验证：

- `.NET 10` 加载、TLS、连接池、异步 I/O 和取消。
- `DbDataSource`、`DbBatch`、参数、预处理语句和多结果集。
- 类型 OID、数组、JSONB、UUID、日期时间、区间、范围、枚举、二进制和大对象。
- `RETURNING`、影响行数、SQLSTATE 和服务端异常字段。
- 隔离级别、保存点、环境事务和故障转移。

驱动行为错误时 EF Provider 无法在上层补救，因此需要独立 ADO.NET 测试套件。

### 2. Provider 基础服务

- 选项扩展、运行时服务和设计时服务。
- 关系型连接、数据库创建器、执行策略和瞬时错误检测。
- SQL 生成、标识符引用、参数占位符和脚本生成。
- Provider 选项哈希和查询缓存键。

### 3. 类型映射

| CLR 类型 | KingbaseES pg 类型 |
|---|---|
| `bool` | `boolean` |
| `byte/short/int/long` | `smallint/integer/bigint` |
| `decimal` | `numeric(p,s)` |
| `float/double` | `real/double precision` |
| `string/char` | `text/varchar/char` |
| `Guid` | `uuid` |
| `byte[]` | `bytea` |
| `DateOnly` | `date` |
| `TimeOnly/TimeSpan` | `time/interval`，按语义区分 |
| `DateTime` | `timestamp without time zone`，策略固定 |
| `DateTimeOffset` | `timestamp with time zone` |
| `JsonDocument/JsonElement` | `jsonb` |
| primitive collections | 数组或 JSONB，按配置选择 |
| enum/range | 数据库类型或值转换 |
| 空间类型 | 独立 NetTopologySuite 插件 |

精度、舍入、时区、夏令时、Unicode、空字符、最大长度和 null 语义都必须测试。

### 4. 查询翻译

- SQL 表达式工厂和查询 SQL 生成器。
- CLR 成员、方法、字符串、数学和日期时间翻译器。
- 数组、集合、JSON、正则、全文检索和范围运算。
- 分页、锁、窗口函数、LATERAL、集合运算和相关子查询。
- null 语义、布尔表达式、类型转换和参数化集合。
- `ExecuteUpdate/Delete` 的表定位、别名和 `RETURNING`。

每项同时验证结果和生成 SQL，防止“能执行但语义不同”。

### 5. 更新管线

- Insert/Update/Delete 和批处理 SQL。
- identity、sequence、HiLo、GUID、默认值和计算列回读。
- `RETURNING` 回读数据库生成值。
- 并发令牌、零影响行数、级联删除和临时键。
- 参数数量、批大小和语句长度限制。

`rowversion` 在 pg 模式下不是通用系统列语义，应使用显式版本列加触发器，或经过实测的系统版本字段。

### 6. Migration

- schema、表、列、约束、索引、序列、注释和排序规则。
- identity、默认值、计算列、rename、alter type、alter nullability 和数据回填。
- enum、range、extension 的创建和删除。
- 幂等脚本、迁移历史表和事务抑制。
- 不支持直接修改的 DDL 使用“新列/新表 + 数据搬迁 + 重建约束”。

### 7. Database First

- 从 KingbaseES 系统视图读取元数据，不能照搬 PostgreSQL 系统目录假设。
- 识别 identity、默认表达式、计算列、数组、JSONB、enum、range 和空间类型。
- 生成 `UseKdbndp`，不生成 `UseNpgsql`。
- 支持多 schema、大小写对象名、保留字和表过滤。

## 测试和发布门槛

测试分层：

1. `Kdbndp` ADO.NET 测试。
2. 类型映射、SQL、翻译器和选项单元测试。
3. EF Core 10 官方关系型 specification tests。
4. JSON、数组、范围、全文、空间、Migration 和 scaffolding 测试。
5. 同一 LINQ 在 SQL Server、PostgreSQL/Npgsql、KingbaseES 的结果差异测试。
6. KingbaseES V9R1C10 单机、主备、TLS、连接池和高并发测试。

发布门槛：

- 官方规范测试适用项 100% 通过；跳过项必须记录数据库原因。
- CRUD、查询、Migration、Database First 无 P0/P1 缺陷。
- 不允许静默客户端求值或静默改变语义。
- 现有示例只更换 Provider 包即可运行。
- 中断、回滚、并发冲突和取消无连接泄漏。
- EF Core 每个 `10.0.x` 补丁都做完整回归，再发布对应 Provider 补丁。

核心测试必须连接真实 KingbaseES，不能用 PostgreSQL 代替。

## 实施计划

### 阶段 0：基线，1-2 周

- 获取 `Kdbndp`、旧 Provider 源码、许可证、SDK 和类型说明。
- 固定 KingbaseES V9R1C10 补丁号、`pg` 模式和测试镜像。
- 收集现有业务 LINQ、迁移、原始 SQL、类型和扩展 API。

### 阶段 1：最小 Provider，3-5 周

- 打通 `UseKdbndp -> EnsureCreated -> CRUD -> Migration`。
- 支持基础类型、常用 LINQ、事务、主外键、identity/sequence 和 `RETURNING`。

### 阶段 2：关系型完整能力，5-8 周

- 复杂查询、继承、复杂类型、批量更新删除、并发和完整 Migration。
- 接入 specification tests，完成 Database First。

### 阶段 3：EF Core 10 和高级类型，4-6 周

- JSONB、数组、primitive collections、enum、range 和全文检索。
- JSON complex types、named filters 和新 LINQ 翻译。
- 空间和 NodaTime 独立扩展包。

### 阶段 4：生产强化，3-5 周

- 性能、批处理、连接池、故障转移、TLS、高并发和稳定性。
- 兼容矩阵、迁移指南、限制清单和 NuGet 发布流程。

粗略投入为 4-6 名熟悉 EF Core Provider、ADO.NET 和 PostgreSQL 方言的工程师，约 4-6 个月。拿不到 `Kdbndp` 源码时，周期和兼容度都会明显恶化。

## 立即执行

1. 向电科金仓获取 `Kdbndp`、旧 EF Core Provider 源码和 .NET 10 支持计划。
2. 准备 `pg` 模式 KingbaseES V9R1C10 测试实例。
3. 建立现有项目兼容样本库。
4. 创建 Provider 骨架并完成最小闭环。
5. 导入 EF Core 10 官方 specification tests，按失败项驱动实现。

只有测试矩阵通过后，才能声明“与原有 EF Core 用法一致、效果一致”。
