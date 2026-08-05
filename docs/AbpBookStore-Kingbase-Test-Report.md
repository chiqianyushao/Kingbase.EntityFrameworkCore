# ABP BookStore × KingbaseES — EF Core 10 实战测试报告

- 报告日期:2026-08-05
- 样例:`samples/AbpBookStore/`(标准 ABP 分层,真实 ABP 10.6 框架)
- EF Core:10.0.9 ｜ Provider:Kingbase.EntityFrameworkCore 10.0.0-alpha.3 ｜ Kdbndp_V9 10.0.1.703
- 目标库:KingbaseES V009R001C002B0014,`database_mode=oracle`(测试库建议 `abp_bookstore_dev`)
- 说明:本报告「离线已验证」部分已由本机执行填状态;「真实库」部分为测试预留,连接后回填。

## 1. 目标

把 ABP 图书管理项目(Acme.BookStore)的标准分层样例,换成人大金仓 + 本仓库 Provider,实测 EF Core 10
在真实业务形态(审计/软删除/乐观并发/复合键多对多/JSON ExtraProperties/仓储查询管线)下是否可用、哪里有问题。

## 2. 环境

| 项 | 值 |
|---|---|
| .NET SDK | 10.0.301 |
| EF Core | 10.0.9 |
| ABP Framework | 10.6.0(依赖锁定 EF Core 10.0.9,与 Provider 基线一致) |
| Provider | Kingbase.EntityFrameworkCore 10.0.0-alpha.3(仓库内 ProjectReference) |
| Kdbndp | Kdbndp_V9 10.0.1.703 |
| KingbaseES | V009R001C002B0014,Oracle 兼容模式 |

## 3. 结构

12 个项目:`Domain.Shared / Domain / Application.Contracts / Application / EntityFrameworkCore /
HttpApi / HttpApi.Host / DbMigrator / Client` + `TestBase / Application.Tests / EntityFrameworkCore.Tests`。

## 4. 离线已验证(无需数据库,本机已执行 ✅)

| 项 | 状态 | 结果 |
|---|---|---|
| `dotnet build AbpBookStore.slnx -c Release` | ✅ | 12 项目全绿 |
| 离线 DDL 生成(`ModelAndDdlTests`,4 项) | ✅ | 全部通过 |
| `dotnet ef migrations add InitialCreate` | ✅ | 生成 migration/designer/snapshot 三文件 |
| 迁移 DDL 内容 | ✅ | 见 §5 |
| `HttpApi.Host` 启动 | ✅ | Swagger 200;无库时 API 返回 ABP 错误信封 |
| 测试项目离线运行 | ✅ | EF.Tests 14 通过(10 项真实库早退)、Application.Tests 1 通过 |

### 4.1 模型存储类型(由 `ModelAndDdlTests` 实际记录)

| Book 属性 | Store Type |
|---|---|
| Id | `uuid` |
| Name | `character varying(128)` |
| Type(enum) | `integer` |
| PublishDate | `timestamp without time zone` |
| Price(float) | `real` |
| ExtraProperties | **`text`**(ABP 10 值转换器输出 JSON 字符串,不是 jsonb) |
| ConcurrencyStamp | `character varying(40)` |
| IsDeleted | `boolean` |

### 4.2 InitialCreate 迁移要点

- `Authors/Books`:全部 ABP 约定列(审计、软删除、ConcurrencyStamp、ExtraProperties)已生成。
- `BookAuthors`:复合主键 `(BookId, AuthorId)`,两条 FK 均 `ON DELETE CASCADE`。
- 索引:`Books.Name`、`Authors.Name`、`BookAuthors.AuthorId`。
- Guid 主键**未生成 identity**(客户端赋值,规避了 Provider 未验证的 EF 生成 identity DDL 风险)。

## 5. 真实库测试(已执行 ✅)

在真实 KingbaseES(`abp_bookstore_dev`)上执行,连接方式:

```powershell
cd samples\AbpBookStore
$env:KINGBASE_TEST_CONNECTION='Server=...;Port=54333;UID=system;PWD=...;Database=abp_bookstore_dev;SSL Mode=Disable;Timeout=60'
# 可选自动建库:
$env:KINGBASE_ADMIN_CONNECTION='Server=...;Port=54333;UID=system;PWD=...;Database=template1;SSL Mode=Disable;Timeout=60'
dotnet test AbpBookStore.slnx -c Release
```

> `Timeout=60` 为远程实例的网络抖动兜底;多个测试程序集共用同一测试库时,`AbpAppFixture`
> 用跨进程命名互斥锁串行化(`dotnet test` 会并行执行各程序集)。

| 测试 | 验证点 | 状态 |
|---|---|---|
| `Module_creates_schema_with_three_tables` | ABP module 启动 + EnsureCreated + 三表存在 | ✅ |
| `Seeder_seeds_authors_and_books_with_m2m_links` | IDataSeeder 种子 2 作者 + 3 书 + 链接 | ✅ |
| `Book_creation_and_readback_with_authors` | IRepository 图新建 + Include 回读 | ✅ |
| `GetPagedList_filters_sorts_and_pages` | 过滤 Contains + 排序 + Skip/Take + 总计数 | ✅ |
| `Soft_delete_hides_books_and_ignore_filters_reveals` | ABP 软删除过滤器 + IgnoreQueryFilters | ✅ |
| `BookManager_rejects_duplicate_name` | 领域服务唯一性校验查询 | ✅ |
| `ExtraProperties_jsonb_roundtrip` | ExtraProperties(text 列)字典往返 | ✅ |
| `ConcurrencyStamp_stale_update_throws` | 字符串乐观并发陈旧写 → `AbpDbConcurrencyException` | ✅ |
| `BookAuthor_composite_key_include_and_cascade_delete` | 复合键 + Include + `HardDeleteAsync` 级联 | ✅ |
| `SaveChanges_interceptor_fires` | 经 ABP `PreConfigure` 注入的 ISaveChangesInterceptor | ✅ |
| `BookAppServiceTests.Create_get_list_and_delete_via_app_service` | 应用服务 + AutoMapper + 仓储闭环 | ✅ |

**结果:15/15 全部通过(EF.Tests 14 + Application.Tests 1),未发现 Provider 阻断性问题。**

## 6. 风险区实测结论(原为预判,现为真实库确认)

1. **ExtraProperties 列** — ✅ 确认 ABP 10.6 映射为 `text`(值转换器输出 JSON 字符串,非 jsonb);`note`/`rating` 字典往返正确。
2. **string ConcurrencyStamp** — ✅ 模型 `IsConcurrencyToken=True`、列 `varchar(40)`;陈旧写确实抛并发异常,但 **ABP 包装为 `Volo.Abp.Data.AbpDbConcurrencyException`**(非 EF 的 `DbUpdateConcurrencyException`)。
3. **显式连接实体 BookAuthor** — ✅ 复合主键 + 双 FK `ON DELETE CASCADE`(DDL 与真实库约束均确认);`Include` 回读正常。**注意:级联只在硬删时发生**(见 §7-4)。
4. **软删除过滤器(EF.Property 形式)** — ✅ 软删后默认查询隐藏、`IgnoreQueryFilters()` 可回看。
5. **SaveChanges 拦截器** — ✅ 经 ABP **`PreConfigure`** 注入生效;**`ConfigureOnConfiguring` 注入永不生效**(见 §7-2)。
6. **Guid 主键客户端赋值** — ✅ `uuid` 列插入/回读正常。

## 7. 已发现的问题与结论(真实库回填后)

**总体结论:EF Core 10.0.9 + Kingbase.EntityFrameworkCore 在真实 ABP 10.6 应用 + 真实 KingbaseES 上,
审计/软删除/乐观并发/复合键多对多/ExtraProperties/仓储管线/拦截器/应用服务全链路可用,未发现 Provider 阻断性问题。**
测试过程中发现并修复的 4 个失败,全部是**测试代码对 ABP 语义的误用**,反过来说明 ABP 与 Provider 的组合行为:
数据库实际生成的 `UPDATE` SQL 正确(WHERE 含 `Id AND ConcurrencyStamp`)、外键级联正确、拦截器管线正确。

具体发现(均与 Provider 无关,但踩坑记录):

1. **ABP 10 `Configure<BookStoreDbContext>` 每个 DbContext 类型只有一条 action**
   `AbpDbContextOptions.ConfigureActions` 是 `Dictionary<Type, object>`(单值)。EF 模块拥有 `UseKdbndp` 这条
   Configure;其他模块再调 `Configure<T>` 会**覆盖** provider 配置 → `No database provider has been configured`。
   追加逻辑必须走 additive 的 `PreConfigure<T>`(在 `DbContextOptionsFactory.Create` 执行)。

2. **`ConfigureOnConfiguring` 里加拦截器永不生效**
   `AbpDbContext.OnConfiguring` 在构造函数内执行,此时 `LazyServiceProvider` 仍为 null(Autofac 属性注入在
   构造**之后**),该 hook 直接 return。正确做法:
   `options.PreConfigure<BookStoreDbContext>(ctx => ctx.DbContextOptions.AddInterceptors(interceptor))`。

3. **EF Core 10 `SaveChangesInterceptor` 的 async 方法不转发到 sync 方法**
   `SavingChangesAsync` 默认实现直接返回 result,不会调用 `SavingChanges`。异步保存路径只走 async 重载,
   业务/测试拦截器必须重写 `SavingChangesAsync`(或两个都重写)。

4. **对软删聚合根 `Remove`/`DeleteAsync` 是软删除,不触发 DB 级联**
   `context.Remove(book)` 经 ABP 软删转换后是 `UPDATE ... IsDeleted=true`,m2m 连接行(BookAuthor)保留。
   `Books.CountAsync()` 为 0 只是因为软删过滤器。强制物理删除用 `IRepository.HardDeleteAsync`,DB 的
   `ON DELETE CASCADE` 才真正执行,连接行随之删除。

5. **ABP 把 EF 并发异常包装成 `AbpDbConcurrencyException`**
   陈旧 `ConcurrencyStamp` 写入(affected 0 rows)时,`AbpDbContext.SaveChangesAsync` 抛
   `Volo.Abp.Data.AbpDbConcurrencyException`。应用层应捕获该类型。

6. **ABP 嵌套 UoW 共享同一 DbContext**
   另一个 UoW 处于激活状态时 `Begin()` 会创建子 UoW,共享父 UoW 的 DbContext(进而共享同一实体实例)。
   因此"两个嵌套 scope 各自加载同一行再先后保存"无法制造真正的乐观并发冲突——两个实体是同一个实例。
   测试改为用裸 SQL 模拟第二个写者(直接改库里的 ConcurrencyStamp)才验证到真实冲突。

7. **同一 UoW 内先 Insert 再 GetAsync 会 EntityNotFoundException**
   未 `CompleteAsync()` 前行尚未入库,`GetAsync` 查询不到。插入与读取应分属两个 UoW(或先 commit)。

8. **`dotnet test AbpBookStore.slnx` 并行执行各测试程序集**
   多个程序集共用同一测试库会互相 DROP 表(实测:`关系 "Books" 不存在`、`23505 系统目录重复键`、
   `42P07 关系已存在`)。`AbpAppFixture` 用命名互斥锁 `AbpBookStore.Kingbase.Database` 串行化整个
   夹具生命周期解决。

9. **ExtraProperties 映射为 `text` 而非 jsonb**(ABP 10.6 行为)——意味着 Provider 的 `JsonElement→jsonb`
   映射在本场景未被触及,是 ABP 层把 JSON 转成了字符串。

## 8. 回填记录

- 2026-08-05:全部 15 项真实库测试通过并回填;§6 更新为实测结论;§7 追加真实库发现。
- 跑测命令:`KINGBASE_TEST_CONNECTION` + `KINGBASE_ADMIN_CONNECTION` 均设置,`dotnet test AbpBookStore.slnx -c Release`。
