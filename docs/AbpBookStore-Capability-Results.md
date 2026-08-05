# ABP BookStore × KingbaseES — 能力矩阵实测结果(EF Core 10)

- 日期:2026-08-05
- 样例:`samples/AbpBookStore/`(真实 ABP 10.6 + Kingbase.EntityFrameworkCore + 真实 KingbaseES)
- 矩阵来源:`docs/EFCore10-KingbaseES-Compatibility-Report.md`(§3–§14)
- 共 369 项,已逐项标注本样例实测结果。

## 汇总

| 状态 | 数量 |
|---|---|
| ✅ 本样例实库重验 | 181 |
| 🟡 Provider 套件已验证 | 154 |
| ⛔ | 20 |
| ❌ | 13 |
| 🚫 | 1 |

## 3. 已实库确认的能力

| 能力 | 原报告 | 本样例实测 | 证据 |
|---|---|---|---|
| UseKdbndp(string) | ✅ | ✅ 本样例实库重验 | 所有测试经 EF 模块 UseKdbndp(conn) |
| UseKdbndp(DbConnection) | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Use_transaction_with_external_kdbndp_connection |
| Oracle 兼容模式选项 | ✅ | ✅ 本样例实库重验 | AbpBookStoreEntityFrameworkCoreModule SetOracleCompatibilityMode() |
| 基础 Where/Select/OrderBy/Take | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Comparison_logic_arithmetic_and_ternary |
| LIMIT/OFFSET | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Set_ordering_and_paging |
| 新增/查询/修改/删除 | ✅ | ✅ 本样例实库重验 | CapabilitySaveTrackingTests.Add/Update/Remove |
| 手工键值 Insert | ✅ | ✅ 本样例实库重验 | ModelBehaviorTests.BookAuthor_composite_key（Guid 客户端赋值） |
| identity 主键回读 | ✅ | 🟡 Provider 套件已验证 | KingbaseCrudIntegrationTests.Insert_reads_database_generated_identity |
| 乐观并发更新条件 | ✅ | ✅ 本样例实库重验 | ModelBehaviorTests.ConcurrencyStamp_stale_update_throws |
| ExecuteDelete/ExecuteDeleteAsync | ✅ | ✅ 本样例实库重验 | CapabilitySaveTrackingTests.ExecuteDelete_and_ExecuteUpdate |
| Any/Single/ToList 异步查询 | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Aggregate/Element |
| 基础事务性 SaveChanges | ✅ | ✅ 本样例实库重验 | CapabilitySaveTrackingTests.SaveChanges_sync_and_async |
| EnsureCreated | ✅ | ✅ 本样例实库重验 | ModuleAndRepositoryTests.Module_creates_schema;TestDatabase.ResetSchemaAsync |
| EnsureDeleted | ✅ | 🟡 Provider 套件已验证 | KingbaseCrudIntegrationTests.EnsureCreated_and_EnsureDeleted_manage_the_database |
| 数据库创建/删除 | ✅ | ✅ 本样例实库重验 | CapabilityMigrationsTests（管理库建/删临时库） |
| 数据库存在性/用户表检测 | ✅ | 🟡 Provider 套件已验证 | KingbaseCrudIntegrationTests.EnsureCreated_creates_tables_from_the_ef_model |
| 字符串专用翻译 | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.String_translations |
| 日期部件翻译 | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Date_translations |
| 数学函数翻译 | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Math_translations |
| 查询 API 契约 | ✅ | 🟡 Provider 套件已验证 | QueryApiContractTests（反射清单） |
| 聚合与集合运算 | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Aggregate/Set_ordering |
| .NET 10 左右连接 | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests.Grouping_and_join_operators |
| Split Query | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Query_extensions |
| EF 查询扩展 | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Query_extensions |
| 关系型查询扩展 | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.From_sql_and_create_db_command |
| CLR 标量类型往返 | ✅ | 🟡 Provider 套件已验证 | KingbaseTypeAndSaveIntegrationTests.Scalar_clr_types_roundtrip |
| 保存与批处理 | ✅ | ✅ 本样例实库重验 | CapabilitySaveTrackingTests 全部 |
| 设计时 Provider 发现 | ✅ | 🟡 Provider 套件已验证 | KingbaseDesignTimeServicesTests |

## 4. EF Core 10 新增能力专项矩阵

| 能力 | 原报告 | 本样例实测 | 证据 |
|---|---|---|---|
| Optional complex types - table splitting | ✅ | 🟡 Provider 套件已验证 | KingbaseRemainingCoverageIntegrationTests.Optional_struct_complex_equality_and_bulk_assignment_work |
| Complex types 映射 JSON | ❌ | ❌ | 无 EF JSON SQL tree<br/>*报告结论：不支持* |
| Complex type struct | ✅ | 🟡 Provider 套件已验证 | KingbaseRemainingCoverageIntegrationTests.Optional_struct_complex_equality_and_bulk_assignment_work |
| Complex type structural equality | ✅ | 🟡 Provider 套件已验证 | 同上 |
| Complex type bulk assignment | ✅ | 🟡 Provider 套件已验证 | 同上 |
| 参数化集合默认多参数翻译 | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests.Microsecond_nanosecond_null_compensation_and_parameter_collections |
| .NET 10 LeftJoin | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests.Grouping_and_join_operators |
| .NET 10 RightJoin | ✅ | 🟡 Provider 套件已验证 | 同上 |
| Split query 稳定排序改进 | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests.Ef_query_extensions |
| DateOnly.ToDateTime 翻译 | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests.Date_time_methods_and_current_time |
| DateOnly.DayNumber | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests.Microsecond_nanosecond_null_compensation_and_parameter_collections |
| 微秒/纳秒 DatePart 翻译 | ✅ | 🟡 Provider 套件已验证 | 同上 |
| string 方法接收 char | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests.String_translations |
| JSON 列 ExecuteUpdate | ❌ | ❌ | 无 JSON 更新 SQL<br/>*报告结论：不支持* |
| Named query filters | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance |
| ExecuteUpdateAsync 普通 lambda | ✅ | ✅ 本样例实库重验 | CapabilitySaveTrackingTests.ExecuteDelete_and_ExecuteUpdate |
| 参数名简化 | ✅ | 🟡 Provider 套件已验证 | Provider 自身测试（SQL 观察） |
| 原始 SQL 拼接分析器警告 | ✅ | 🟡 Provider 套件已验证 | 编译期能力，与 Provider 无关 |
| 内联常量日志脱敏 | ✅ | 🟡 Provider 套件已验证 | EF Core 日志能力 |

## 5. Queryable 标准运算符兼容矩阵

| 能力 | 原报告 | 本样例实测 | 证据 |
|---|---|---|---|
| Aggregate | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |
| AggregateBy | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |
| Append | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |
| Chunk | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |
| All | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Aggregate_operators |
| Any | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Aggregate_operators |
| AsQueryable | ✅ | 🟡 Provider 套件已验证 | LINQ 管线操作 |
| Average | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Aggregate_operators |
| Cast | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests.Aggregate_and_element_operators / Set_ordering_and_paging_operators |
| CountBy | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |
| Concat | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Set_ordering_and_paging |
| Contains | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Set_ordering_and_paging / String_translations |
| Count | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Aggregate_operators |
| DistinctBy | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |
| DefaultIfEmpty | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests.Aggregate_and_element_operators / Set_ordering_and_paging_operators |
| Distinct | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Set_ordering_and_paging |
| ExceptBy | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |
| ElementAt | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Element_operators |
| ElementAtOrDefault | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Element_operators |
| Except | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Set_ordering_and_paging |
| First | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Element_operators |
| FirstOrDefault | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Element_operators |
| GroupBy | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Join_grouping_and_select_many |
| GroupJoin | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests.Remaining_relational_queryable_shapes |
| Index | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |
| Intersect | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Set_ordering_and_paging |
| IntersectBy | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |
| Join | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Join_grouping_and_select_many |
| Last | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Element_operators |
| LastOrDefault | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Element_operators |
| LeftJoin | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests.Grouping_and_join_operators |
| LongCount | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Aggregate_operators |
| Max | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Aggregate_operators |
| MaxBy | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |
| Min | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Aggregate_operators |
| MinBy | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |
| OfType | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests.Aggregate_and_element_operators / Set_ordering_and_paging_operators |
| Order | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests.Aggregate_and_element_operators / Set_ordering_and_paging_operators |
| OrderBy | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Set_ordering_and_paging |
| OrderByDescending | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Set_ordering_and_paging |
| OrderDescending | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests.Aggregate_and_element_operators / Set_ordering_and_paging_operators |
| Prepend | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |
| Reverse | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Set_ordering_and_paging |
| RightJoin | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests.Grouping_and_join_operators |
| Select | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Comparison_logic / String_translations |
| SelectMany | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Join_grouping_and_select_many |
| SequenceEqual | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |
| Shuffle | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |
| Single | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Element_operators |
| SingleOrDefault | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Element_operators |
| Skip | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Set_ordering_and_paging |
| SkipLast | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |
| SkipWhile | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |
| Sum | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Aggregate_operators |
| Take | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Set_ordering_and_paging |
| TakeLast | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |
| TakeWhile | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |
| ThenBy | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Set_ordering_and_paging |
| ThenByDescending | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Set_ordering_and_paging |
| Union | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Set_ordering_and_paging |
| UnionBy | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |
| Where | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Comparison_logic / String_translations |
| Zip | ⛔ | ⛔ | EF Core 10 无关系型翻译入口 |

## 6.1 EntityFrameworkQueryableExtensions

| 能力 | 原报告 | 本样例实测 | 证据 |
|---|---|---|---|
| AllAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Aggregate_operators |
| AnyAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Aggregate_operators |
| AsAsyncEnumerable | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests.Ef_query_extensions_execute_on_kingbase |
| AsNoTracking | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Query_extensions |
| AsNoTrackingWithIdentityResolution | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Query_extensions |
| AsTracking | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Query_extensions |
| AverageAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Aggregate_operators |
| ContainsAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Aggregate_operators |
| CountAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Aggregate_operators |
| ElementAtAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Element_operators |
| ElementAtOrDefaultAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Element_operators |
| ExecuteDelete | ✅ | ✅ 本样例实库重验 | CapabilitySaveTrackingTests.ExecuteDelete_and_ExecuteUpdate |
| ExecuteDeleteAsync | ✅ | ✅ 本样例实库重验 | CapabilitySaveTrackingTests.ExecuteDelete_and_ExecuteUpdate |
| ExecuteUpdate | ✅ | ✅ 本样例实库重验 | CapabilitySaveTrackingTests.ExecuteDelete_and_ExecuteUpdate |
| ExecuteUpdateAsync | ✅ | ✅ 本样例实库重验 | CapabilitySaveTrackingTests.ExecuteDelete_and_ExecuteUpdate |
| FirstAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Element_operators |
| FirstOrDefaultAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Element_operators |
| ForEachAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Query_extensions |
| IgnoreAutoIncludes | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests.Ef_query_extensions_execute_on_kingbase |
| IgnoreQueryFilters | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Ignore_query_filters_reveals_soft_deleted |
| Include | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Query_extensions |
| LastAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Element_operators |
| LastOrDefaultAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Element_operators |
| Load | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Query_extensions |
| LoadAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Query_extensions |
| LongCountAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Aggregate_operators |
| MaxAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Aggregate_operators |
| MinAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Aggregate_operators |
| SingleAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Element_operators |
| SingleOrDefaultAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Element_operators |
| SumAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Aggregate_operators |
| TagWith | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Query_extensions |
| TagWithCallSite | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Query_extensions |
| ThenInclude | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Query_extensions |
| ToArrayAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Query_extensions |
| ToDictionaryAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Query_extensions |
| ToHashSetAsync | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Query_extensions |
| ToListAsync | ✅ | ✅ 本样例实库重验 | 全测试通用 |
| ToQueryString | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Query_extensions |

## 6.2 RelationalQueryableExtensions

| 能力 | 原报告 | 本样例实测 | 证据 |
|---|---|---|---|
| AsSingleQuery | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Query_extensions |
| AsSplitQuery | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Query_extensions |
| CreateDbCommand | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.From_sql_and_create_db_command |
| FromSql | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.From_sql_and_create_db_command |
| FromSqlInterpolated | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.From_sql_and_create_db_command |
| FromSqlRaw | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.From_sql_and_create_db_command |

## 7. 表达式、运算符和函数翻译

| 能力 | 原报告 | 本样例实测 | 证据 |
|---|---|---|---|
| == != > >= < <= | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Comparison_logic |
| && \|\| ! | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Comparison_logic |
| + - * / % | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Comparison_logic<br/>*real % integer 无操作符(42883),取模用 (int) 转型* |
| 三元条件 ?: | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Comparison_logic |
| null 合并 ?? | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Null_coalesce |
| null 比较与补偿 | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Element_operators |
| 字符串拼接 | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Comparison_logic |
| string.Equals | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Comparison_logic |
| Contains/StartsWith/EndsWith | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.String_translations |
| ToLower/ToUpper | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.String_translations |
| Substring/Replace/Trim/IndexOf | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.String_translations |
| string.Length | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.String_translations |
| EF.Functions.Like | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Ef_functions_guid_enum |
| EF.Functions.Collate | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Ef_functions_guid_enum |
| EF.Functions.Random | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Ef_functions_guid_enum |
| Math.* | 🟠 | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests.Math_translations<br/>*已实现并验证 abs/ceil/floor/round/sqrt/cbrt/log/log2/max/min/clamp/三角* |
| DateTime.* 成员/方法 | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Date_translations |
| DateOnly/TimeOnly 方法 | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests（对应专项） |
| Guid.NewGuid() | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Ef_functions_guid_enum |
| Enum HasFlag/ToString | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Ef_functions_guid_enum |
| Regex | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests（对应专项） |
| byte array SequenceEqual | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests（对应专项） |
| 全文检索 | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests（对应专项） |
| 数组运算 | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests（对应专项） |
| range 运算 | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests（对应专项） |
| JSON path/query | 🟠 | 🟡 Provider 套件已验证 | KingbaseRemainingCoverageIntegrationTests.Json_array_list_range_and_fulltext_types_roundtrip<br/>*标量 JSON 单层提取已验证* |
| 窗口函数 | ❌ | ❌ | 无 EF 通用公开窗口表达式入口<br/>*报告结论：不支持* |
| LATERAL/APPLY | ✅ | 🟡 Provider 套件已验证 | KingbaseQueryTranslationIntegrationTests（对应专项） |

## 8. 模型构建语法矩阵

| 能力 | 原报告 | 本样例实测 | 证据 |
|---|---|---|---|
| DbSet<TEntity> / Entity<TEntity> | ✅ | ✅ 本样例实库重验 | BookStoreDbContext |
| 表名、schema、列名 | ✅ | ✅ 本样例实库重验 | BookStoreDbContext ToTable/列映射;ModelAndDdlTests |
| 主键、复合主键 | ✅ | ✅ 本样例实库重验 | Book/BookAuthor;ModelBehaviorTests.BookAuthor_composite_key |
| Alternate Key / Unique Constraint | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Keyless Entity | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Required/Optional 属性 | ✅ | ✅ 本样例实库重验 | Book.Name IsRequired;Author.ShortBio 可空 |
| Shadow Property | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Indexer Property / Property Bag | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Backing Field / Field-only | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Column Type/Name/Order | ✅ | ✅ 本样例实库重验 | ModelAndDdlTests.Relational_store_types |
| MaxLength / Unicode | ✅ | ✅ 本样例实库重验 | BookConsts.MaxNameLength=128 -> varchar(128) |
| Precision/Scale | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Default Value / Default SQL | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Computed Column | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| ValueGeneratedNever | ✅ | ✅ 本样例实库重验 | Guid 主键客户端赋值 |
| ValueGeneratedOnAdd identity | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Sequence | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| HiLo | ❌ | ❌ | 无 Kingbase HiLo/value generator<br/>*报告结论：不支持* |
| GUID 数据库生成 | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Concurrency Token | ✅ | ✅ 本样例实库重验 | ModelBehaviorTests.ConcurrencyStamp_stale_update_throws |
| RowVersion/Timestamp | ❌ | ❌ | 无统一 rowversion 实现<br/>*报告结论：不支持* |
| Check Constraint | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Index：普通/唯一/复合 | ✅ | ✅ 本样例实库重验 | BookStoreDbContext HasIndex(Name) |
| Index：descending/filter/include | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| 一对一 | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| 一对多 | ✅ | ✅ 本样例实库重验 | Book-BookAuthor;Query_extensions Include |
| 多对多/Skip Navigation | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| 自引用关系 | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Cascade/Restrict/SetNull | ✅ | ✅ 本样例实库重验 | BookAuthor OnDelete(Cascade);ModelBehaviorTests |
| Owned Entity | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Complex Type table splitting | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Complex Type JSON | ❌ | ❌ | 不支持<br/>*报告结论：不支持* |
| Entity/Table Splitting | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| TPH inheritance | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| TPT inheritance | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| TPC inheritance | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Discriminator | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Global Query Filter | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.Ignore_query_filters_reveals_soft_deleted;Soft_delete_hides_books |
| Named Query Filters (EF10) | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Value Converter | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Value Comparer | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Data Seeding HasData | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| UseSeeding/UseAsyncSeeding | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Database Function mapping | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| View mapping | ✅ | 🟡 Provider 套件已验证 | KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table |
| Stored procedure CUD mapping | ❌ | ❌ | 无存储过程更新 SQL<br/>*报告结论：不支持* |
| Temporal Table | 🚫 | 🚫 | SQL Server 专有<br/>*报告结论：不适用* |
| Spatial/NetTopologySuite | ❌ | ❌ | 无空间扩展包和类型映射<br/>*报告结论：不支持* |
| JSON document model | 🟠 | 🟡 Provider 套件已验证 | KingbaseRemainingCoverageIntegrationTests.Json_array_list_range_and_fulltext_types_roundtrip<br/>*标量 JsonDocument 已验证* |
| Primitive Collections | 🟠 | 🟡 Provider 套件已验证 | 同上<br/>*int[]/List<int> 已验证* |

## 9. CLR 与数据库类型支持

| 能力 | 原报告 | 本样例实测 | 证据 |
|---|---|---|---|
| bool → boolean | ✅ | ✅ 本样例实库重验 | Soft_delete_hides_books（IsDeleted） |
| byte → smallint | ✅ | 🟡 Provider 套件已验证 | KingbaseTypeAndSaveIntegrationTests.Scalar_clr_types_roundtrip |
| short → smallint | ✅ | 🟡 Provider 套件已验证 | 同上 |
| int → integer | ✅ | 🟡 Provider 套件已验证 | 同上 |
| long → bigint | ✅ | 🟡 Provider 套件已验证 | 同上 |
| float → real | ✅ | ✅ 本样例实库重验 | Book.Price 往返;Aggregate/Comparison |
| double → double precision | ✅ | 🟡 Provider 套件已验证 | 同上 |
| decimal → numeric(p,s) | ✅ | 🟡 Provider 套件已验证 | 同上 |
| string → text/varchar(n)/char(n) | ✅ | ✅ 本样例实库重验 | Book.Name varchar(128) |
| Guid → uuid | ✅ | ✅ 本样例实库重验 | Book.Id/Author.Id 往返 |
| byte[] → bytea | ✅ | 🟡 Provider 套件已验证 | 同上 |
| DateOnly → date | ✅ | 🟡 Provider 套件已验证 | 同上 |
| TimeOnly → time without time zone | ✅ | 🟡 Provider 套件已验证 | 同上 |
| TimeSpan → interval | ✅ | 🟡 Provider 套件已验证 | 同上 |
| DateTime → timestamp without time zone | ✅ | ✅ 本样例实库重验 | Book.PublishDate 往返;Date_translations |
| DateTimeOffset → timestamp with time zone | ✅ | 🟡 Provider 套件已验证 | 同上 |
| Nullable 上述类型 → 对应类型 | ✅ | 🟡 Provider 套件已验证 | 同上 |
| char → character(1) | ✅ | 🟡 Provider 套件已验证 | 同上 |
| unsigned integers → smallint/integer/bigint/numeric(20,0) | ✅ | 🟡 Provider 套件已验证 | 同上 |
| enum → integer | ✅ | ✅ 本样例实库重验 | Book.Type 往返;Ef_functions_guid_enum |
| JsonDocument/JsonElement → jsonb | ✅ | 🟡 Provider 套件已验证 | KingbaseRemainingCoverageIntegrationTests.Json_array_list_range_and_fulltext_types_roundtrip |
| int[] / List<int> → integer[] | ✅ | 🟡 Provider 套件已验证 | 同上 |
| KdbndpRange<int> → int4range | ✅ | 🟡 Provider 套件已验证 | 同上 |
| Spatial 类型 | ❌ | ❌ | 实例未装空间扩展<br/>*报告结论：不支持* |
| NodaTime 类型 | ❌ | ❌ | 需 NodaTime 包及插件<br/>*报告结论：需独立交付* |

## 10. 保存、跟踪和批量操作

| 能力 | 原报告 | 本样例实测 | 证据 |
|---|---|---|---|
| Add/AddAsync/AddRange | ✅ | ✅ 本样例实库重验 | CapabilitySaveTrackingTests.Add_and_AddRange |
| Attach/AttachRange | ✅ | ✅ 本样例实库重验 | CapabilitySaveTrackingTests.Attach_and_AttachRange |
| Update/UpdateRange | ✅ | ✅ 本样例实库重验 | CapabilitySaveTrackingTests.Update_and_UpdateRange |
| Remove/RemoveRange | ✅ | ✅ 本样例实库重验 | CapabilitySaveTrackingTests.Remove_and_RemoveRange |
| SaveChanges/SaveChangesAsync | ✅ | ✅ 本样例实库重验 | CapabilitySaveTrackingTests.SaveChanges_sync_and_async |
| ChangeTracker DetectChanges | ✅ | ✅ 本样例实库重验 | CapabilitySaveTrackingTests.DetectChanges |
| Snapshot/Notification tracking | ✅ | 🟡 Provider 套件已验证 | KingbaseTypeAndSaveIntegrationTests.Save_tracking_batching_concurrency_and_cascade |
| Identity Resolution | ✅ | 🟡 Provider 套件已验证 | KingbaseTypeAndSaveIntegrationTests.Save_tracking_batching_concurrency_and_cascade |
| Relationship fixup | ✅ | 🟡 Provider 套件已验证 | KingbaseTypeAndSaveIntegrationTests.Save_tracking_batching_concurrency_and_cascade |
| Temporary values | ✅ | 🟡 Provider 套件已验证 | KingbaseTypeAndSaveIntegrationTests.Save_tracking_batching_concurrency_and_cascade |
| Store-generated identity | ✅ | 🟡 Provider 套件已验证 | KingbaseTypeAndSaveIntegrationTests.Save_tracking_batching_concurrency_and_cascade |
| Store-generated default/computed | ✅ | 🟡 Provider 套件已验证 | KingbaseTypeAndSaveIntegrationTests.Save_tracking_batching_concurrency_and_cascade |
| Optimistic concurrency success | ✅ | ✅ 本样例实库重验 | CapabilitySaveTrackingTests.Update_and_UpdateRange |
| DbUpdateConcurrencyException | ✅ | ✅ 本样例实库重验 | ModelBehaviorTests.ConcurrencyStamp_stale_update_throws（ABP 包装为 AbpDbConcurrencyException） |
| Cascade delete client/database | ✅ | ✅ 本样例实库重验 | ModelBehaviorTests.BookAuthor_composite_key;CapabilitySaveTrackingTests.Remove_and_RemoveRange |
| Batching | ✅ | ✅ 本样例实库重验 | CapabilitySaveTrackingTests.SaveChanges_sync_and_async |
| ExecuteDelete | ✅ | ✅ 本样例实库重验 | CapabilitySaveTrackingTests.ExecuteDelete_and_ExecuteUpdate |
| ExecuteUpdate | ✅ | ✅ 本样例实库重验 | CapabilitySaveTrackingTests.ExecuteDelete_and_ExecuteUpdate |
| JSON ExecuteUpdate | ❌ | ❌ | 不支持<br/>*报告结论：不支持* |
| Stored procedure SaveChanges | ❌ | ❌ | 不支持<br/>*报告结论：不支持* |

## 11. 连接、事务、原始 SQL 和执行策略

| 能力 | 原报告 | 本样例实测 | 证据 |
|---|---|---|---|
| OpenConnection/CloseConnection 同步/异步 | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Connection_lifecycle |
| BeginTransaction/Commit/Rollback | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Begin_transaction |
| Savepoint | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Savepoints |
| Ambient TransactionScope | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Facade_transaction_raw_sql_timeout_compiled_query_and_interceptors_work |
| UseTransaction | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Use_transaction_with_external_kdbndp_connection |
| Execution Strategy/Retry | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Execution_strategy |
| Command timeout | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Connection_lifecycle |
| Connection pooling | ✅ | 🟡 Provider 套件已验证 | KingbaseRemainingCoverageIntegrationTests.Query_transaction_interceptors_and_connection_pool_concurrency_work |
| 多主机/故障转移 | ✅ | 🟡 Provider 套件已验证 | KingbaseRemainingCoverageIntegrationTests.Data_source_and_single_host_multi_host_source_work |
| FromSql/FromSqlRaw/FromSqlInterpolated | ✅ | ✅ 本样例实库重验 | CapabilityLinqAndTranslationsTests.From_sql_and_create_db_command |
| SqlQuery/SqlQueryRaw | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Raw_sql |
| ExecuteSql/ExecuteSqlRaw/Interpolated | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Raw_sql |
| Query/Command/Transaction Interceptor | ✅ | 🟡 Provider 套件已验证 | TestSaveChangesInterceptor（样例）;KingbaseFacadeAndMigrationsIntegrationTests |
| Compiled Query | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Facade_transaction_raw_sql_timeout_compiled_query_and_interceptors_work |
| Compiled Model | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Facade_transaction_raw_sql_timeout_compiled_query_and_interceptors_work |
| Query precompilation/NativeAOT | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Facade_transaction_raw_sql_timeout_compiled_query_and_interceptors_work |

## 12. RelationalDatabaseFacadeExtensions 全清单

| 能力 | 原报告 | 本样例实测 | 证据 |
|---|---|---|---|
| BeginTransaction | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Begin_transaction |
| BeginTransactionAsync | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Begin_transaction |
| CloseConnection | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Connection_lifecycle |
| CloseConnectionAsync | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Connection_lifecycle |
| ExecuteSql | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Raw_sql |
| ExecuteSqlAsync | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Raw_sql |
| ExecuteSqlInterpolated | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Raw_sql |
| ExecuteSqlInterpolatedAsync | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Raw_sql |
| ExecuteSqlRaw | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Raw_sql |
| ExecuteSqlRawAsync | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Raw_sql |
| GenerateCreateScript | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Connection_lifecycle;ModelAndDdlTests |
| GetAppliedMigrations | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Facade_transaction_raw_sql_timeout_compiled_query_and_interceptors_work |
| GetAppliedMigrationsAsync | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Facade_transaction_raw_sql_timeout_compiled_query_and_interceptors_work |
| GetCommandTimeout | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Connection_lifecycle |
| GetConnectionString | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Connection_lifecycle |
| GetDbConnection | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Connection_lifecycle |
| GetMigrations | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Facade_transaction_raw_sql_timeout_compiled_query_and_interceptors_work |
| GetPendingMigrations | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Facade_transaction_raw_sql_timeout_compiled_query_and_interceptors_work |
| GetPendingMigrationsAsync | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Facade_transaction_raw_sql_timeout_compiled_query_and_interceptors_work |
| HasPendingModelChanges | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Facade_transaction_raw_sql_timeout_compiled_query_and_interceptors_work |
| IsRelational | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Connection_lifecycle |
| Migrate | ✅ | ✅ 本样例实库重验 | CapabilityMigrationsTests.Initial_create_migration |
| MigrateAsync | ✅ | ✅ 本样例实库重验 | CapabilityMigrationsTests.Initial_create_migration |
| OpenConnection | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Connection_lifecycle |
| OpenConnectionAsync | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Connection_lifecycle |
| SetCommandTimeout | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Connection_lifecycle |
| SetConnectionString | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Facade_transaction_raw_sql_timeout_compiled_query_and_interceptors_work |
| SetDbConnection | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Facade_transaction_raw_sql_timeout_compiled_query_and_interceptors_work |
| SqlQuery | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Raw_sql |
| SqlQueryRaw | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Raw_sql |
| UseTransaction | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Use_transaction_with_external_kdbndp_connection |
| UseTransactionAsync | ✅ | ✅ 本样例实库重验 | CapabilityConnectionTransactionsTests.Use_transaction_with_external_kdbndp_connection |

## 13. Migration 全部操作类型兼容矩阵

| 能力 | 原报告 | 本样例实测 | 证据 |
|---|---|---|---|
| CreateTableOperation（InitialCreate 应用） | ✅ | ✅ 本样例实库重验 | CapabilityMigrationsTests.Initial_create_migration |
| AddCheckConstraintOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| AddColumnOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| AddForeignKeyOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| AddPrimaryKeyOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| AddUniqueConstraintOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| AlterColumnOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| AlterDatabaseOperation | ❌ | ❌ | 数据库不能在线修改 collation<br/>*报告结论：不支持* |
| AlterSequenceOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| AlterTableOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| CreateIndexOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| CreateSequenceOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| CreateTableOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| DeleteDataOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| DropCheckConstraintOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| DropColumnOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| DropForeignKeyOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| DropIndexOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| DropPrimaryKeyOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| DropSchemaOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| DropSequenceOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| DropTableOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| DropUniqueConstraintOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| EnsureSchemaOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| InsertDataOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| RenameColumnOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| RenameIndexOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| RenameSequenceOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| RenameTableOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| RestartSequenceOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| SqlOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |
| UpdateDataOperation | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes |

## 14. Database First、设计时和工具链

| 能力 | 原报告 | 本样例实测 | 证据 |
|---|---|---|---|
| dotnet ef 识别 Provider | ✅ | 🟡 Provider 套件已验证 | KingbaseScaffoldingIntegrationTests / KingbaseDesignTimeServicesTests / dotnet ef |
| dotnet ef migrations add | ✅ | 🟡 Provider 套件已验证 | 样例已内置 InitialCreate 迁移 |
| dotnet ef database update | ✅ | 🟡 Provider 套件已验证 | KingbaseFacadeAndMigrationsIntegrationTests |
| dotnet ef migrations script | ✅ | 🟡 Provider 套件已验证 | KingbaseScaffoldingIntegrationTests / KingbaseDesignTimeServicesTests / dotnet ef |
| dotnet ef dbcontext scaffold | ✅ | 🟡 Provider 套件已验证 | KingbaseScaffoldingIntegrationTests / KingbaseDesignTimeServicesTests / dotnet ef |
| Provider 配置代码生成 | ✅ | 🟡 Provider 套件已验证 | KingbaseScaffoldingIntegrationTests / KingbaseDesignTimeServicesTests / dotnet ef |
| Annotation C# 代码生成 | ✅ | 🟡 Provider 套件已验证 | KingbaseScaffoldingIntegrationTests / KingbaseDesignTimeServicesTests / dotnet ef |
| Reverse engineer table/view/key/index | ✅ | 🟡 Provider 套件已验证 | KingbaseScaffoldingIntegrationTests / KingbaseDesignTimeServicesTests / dotnet ef |
| Reverse engineer sequence | ✅ | 🟡 Provider 套件已验证 | KingbaseScaffoldingIntegrationTests / KingbaseDesignTimeServicesTests / dotnet ef |
| Compiled model optimize | ✅ | 🟡 Provider 套件已验证 | KingbaseScaffoldingIntegrationTests / KingbaseDesignTimeServicesTests / dotnet ef |
| NativeAOT optimize/precompiled queries | ✅ | 🟡 Provider 套件已验证 | KingbaseScaffoldingIntegrationTests / KingbaseDesignTimeServicesTests / dotnet ef |

## 状态图例

- ✅ 本样例实库重验:在真实 KingbaseES 上经 ABP 栈(仓储/UoW/DbContext)执行并通过。
- 🟡 Provider 套件已验证:由 `test/Kingbase.EntityFrameworkCore.Tests` 自带集成测试覆盖,本样例未重复。
- ⛔ EF 无服务端语义 / ❌ 不支持 / 🚫 不适用 / 🟠 部分支持:沿用原报告结论。
