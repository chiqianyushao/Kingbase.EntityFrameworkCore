# AbpBookStore — ABP BookStore 实战样例(EF Core 10 + Kingbase.EntityFrameworkCore)

参考 ABP 图书管理项目(Acme.BookStore)的标准分层,使用真实 ABP 10.6 框架,数据层换成本仓库的
`Kingbase.EntityFrameworkCore`(人大金仓 / KingbaseES)Provider,实测 EF Core 10。

## 分层结构(标准 ABP)

```
src/
  AbpBookStore.Domain.Shared/        BookType 枚举、BookConsts/AuthorConsts
  AbpBookStore.Domain/               Book/Author/BookAuthor 实体、BookManager、仓储接口
  AbpBookStore.Application.Contracts/ DTO + IBookAppService/IAuthorAppService
  AbpBookStore.Application/          BookAppService/AuthorAppService + AutoMapper
  AbpBookStore.EntityFrameworkCore/  BookStoreDbContext(AbpDbContext)、EF 仓储、数据种子、
                                     Migrations(dotnet ef 已生成 InitialCreate)
  AbpBookStore.HttpApi/              BookController/AuthorController
  AbpBookStore.HttpApi.Host/         ASP.NET Core Web 主机(Swagger)
  AbpBookStore.DbMigrator/           迁移 + 种子控制台
  AbpBookStore.Client/               调用 Web API 的控制台客户端
test/
  AbpBookStore.TestBase/             共享测试基座(fixture、建库/清库、UOW 助手)
  AbpBookStore.Application.Tests/    应用服务测试
  AbpBookStore.EntityFrameworkCore.Tests/ EF 集成测试 + 离线 DDL 测试
```

## 技术栈

| 组件 | 版本 |
|---|---|
| .NET | 10.0.301 |
| EF Core | 10.0.9(与 Provider 基线一致) |
| ABP Framework | 10.6.0(其 net10.0 组恰好锁定 EF Core 10.0.9) |
| Provider | Kingbase.EntityFrameworkCore 10.0.0-alpha.3(本仓库 `src/`) |
| ADO.NET | Kdbndp_V9 10.0.1.703 |
| KingbaseES | V009R001C002B0014,`database_mode=oracle` |

## 运行真实库集成测试

测试连接环境变量未设置时,**数据库相关测试自动跳过**(与 Provider 自身测试一致),`dotnet test` 离线也全绿。

```powershell
cd samples\AbpBookStore

# 方式 A:已有专用库,直接指向(强烈建议用专用测试库,如 abp_bookstore_dev)
$env:KINGBASE_TEST_CONNECTION='Server=127.0.0.1;Port=54321;UID=system;PWD=<你的密码>;Database=abp_bookstore_dev;SSL Mode=Disable'

# 方式 B(可选):自动建库 abp_bookstore_dev
$env:KINGBASE_ADMIN_CONNECTION='Server=127.0.0.1;Port=54321;UID=system;PWD=<你的密码>;Database=template1;SSL Mode=Disable'

dotnet test AbpBookStore.slnx -c Release
```

集成测试覆盖:ABP module 启动 + EnsureCreated、Seeder、IRepository CRUD、GetPagedList
(过滤/排序/分页/总计数)、软删除 + IgnoreQueryFilters、ConcurrencyStamp 乐观并发、
ExtraProperties 往返、BookAuthor 复合键 + 级联删除、SaveChanges 拦截器、应用服务闭环。
> 注意:测试会 `DROP` 目标库中的 `Books / Authors / BookAuthors` 三张表后重建,务必使用专用测试库。
> `dotnet test AbpBookStore.slnx` 会并行执行多个测试程序集;`AbpAppFixture` 已用跨进程互斥锁
> (`AbpBookStore.Kingbase.Database`)串行化共用同一测试库的 DB 测试,避免互相 DROP 表。

## 迁移与种子

```powershell
# 已内置 InitialCreate 迁移(由 dotnet ef 10.0.9 生成)。如需重建:
dotnet ef migrations add InitialCreate `
  --project src\AbpBookStore.EntityFrameworkCore `
  --startup-project src\AbpBookStore.DbMigrator

# 应用迁移 + 种子(Orwell/Adams 两位作者 + 1984 等三本书)
$env:KINGBASE_TEST_CONNECTION='...'
dotnet run --project src\AbpBookStore.DbMigrator -c Release
```

## 启动 Web 主机与客户端

```powershell
# 1) 改 appsettings.json 的 ConnectionStrings:Default,然后:
dotnet run --project src\AbpBookStore.HttpApi.Host -c Release
#    打开 http://localhost:5000/swagger 查看 API

# 2) 另开终端,运行客户端(默认 http://localhost:5000):
dotnet run --project src\AbpBookStore.Client -c Release
```

## 离线已验证(无需数据库)

- `dotnet build AbpBookStore.slnx -c Release` 全绿(12 个项目)。
- `ModelAndDdlTests`(4 项):`GenerateCreateScript()` 断言 ABP 10 约定列、`uuid` 主键、
  复合主键、Cascade FK、索引全部生成;并记录各列存储类型。
- `dotnet ef migrations add InitialCreate`:ABP + Provider 设计时链路可用,迁移 DDL 正确。
- `HttpApi.Host` 可启动,Swagger 返回 200;API 在无数据库时返回 ABP 错误信封(链路完整)。

## 已知 ABP 10.6 API 差异 / 踩坑(与本 Provider 无关,但写 ABP 应用时必踩)

1. **必须用 Autofac**。ABP 的 EF 层靠属性注入 `LazyServiceProvider` 给 DbContext;内置 MS DI 不做属性注入,
   DbContext 在审计/工作单元处会 NRE。主机 `builder.Host.UseAutofac()`,测试 `AbpApplicationFactory.CreateAsync<T>(o => o.UseAutofac())`,
   模块 `[DependsOn(typeof(AbpAutofacModule))]`。

2. **`Configure<BookStoreDbContext>` 每个 DbContext 类型只有一条 action**。`AbpDbContextOptions.ConfigureActions`
   是 `Dictionary<Type, object>`(单值);EF 模块拥有 `UseKdbndp` 这条,别的模块再调 `Configure<T>` 会覆盖它
   → `No database provider has been configured`。追加逻辑(如拦截器)必须用 additive 的 `PreConfigure<T>`:
   ```csharp
   options.PreConfigure<BookStoreDbContext>(ctx =>
       ctx.DbContextOptions.AddInterceptors(saveChangesInterceptor));
   ```

3. **`ConfigureOnConfiguring<T>` 里加拦截器永不生效**。`AbpDbContext.OnConfiguring` 在构造函数内执行,
   此时 `LazyServiceProvider` 仍为 null(Autofac 属性注入在构造**之后**),hook 直接 return。用 `PreConfigure` 代替。

4. **EF Core 10 的 `SaveChangesInterceptor.SavingChangesAsync` 不转发到 `SavingChanges`**。异步保存只调 async
   重载;拦截器必须重写 `SavingChangesAsync`(或两个都重写)。

5. **对软删聚合根 `Remove`/`DeleteAsync` 是软删除**,m2m 连接行不会级联删除。强制物理删除用
   `IRepository.HardDeleteAsync`,DB 的 `ON DELETE CASCADE` 才执行。

6. **并发异常被 ABP 包装成 `Volo.Abp.Data.AbpDbConcurrencyException`**(不是 EF 的 `DbUpdateConcurrencyException`)。

7. **嵌套 UoW 共享同一个 DbContext**:另一个 UoW 激活时 `Begin()` 创建子 UoW,共享父 DbContext 与实体实例。
   制造真正的乐观并发冲突需让两个写入方彼此独立(测试里用裸 SQL 模拟第二个写者)。

8. 其他 API 变化:`AbpDddDomainModule`/细粒度模块类已移除;`ConfigureByConvention()` 移到
   `Volo.Abp.EntityFrameworkCore.Modeling`;`Entity<TKey>.Id` setter 是 `protected`(实体需 `(Guid id): base(id)` 构造);
   `IUnitOfWorkManager.Begin()` 需要显式 `AbpUnitOfWorkOptions`。

详见 `docs/AbpBookStore-Kingbase-Test-Report.md`。

## 能力矩阵实测报告

按 `docs/EFCore10-KingbaseES-Compatibility-Report.md` 的能力清单逐项测试,结果见:

- **Markdown**:`docs/AbpBookStore-Capability-Results.md`
- **HTML**:`docs/AbpBookStore-Capability-Results.html`

矩阵覆盖报告 §3–§14 全部约 370 项:能落到本样例领域的能力全部新增了真实库测试(统计为
`✅ 本样例实库重验`),其余标注 `🟡 Provider 套件已验证` 或沿用原报告结论。
新增测试位于 `test/AbpBookStore.EntityFrameworkCore.Tests/Capability/`,报告由
`CapabilityReportGeneratorTests`(离线)生成,`dotnet test` 即可刷新。
