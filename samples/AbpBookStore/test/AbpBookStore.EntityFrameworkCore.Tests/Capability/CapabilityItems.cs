namespace AbpBookStore.EntityFrameworkCore.Tests.Capability;

/// <summary>
/// One row of the capability matrix declared in
/// docs/EFCore10-KingbaseES-Compatibility-Report.md.
/// </summary>
public sealed record CapabilityItem(
    string Section,
    string Capability,
    string ReportStatus,
    string SampleStatus,
    string Evidence,
    string Note = "");

/// <summary>
/// The full compatibility matrix (report §3–§14) annotated with how each item
/// was re-verified in the AbpBookStore sample:
///   SampleStatus:
///     ✅ 本样例实库重验 — covered by a real-database test in this sample
///     🟡 Provider 套件已验证 — covered by the provider's own integration suite
///     ⛔/❌/🚫/🟠 — no server-side semantics / unsupported / N/A / partial (report conclusion)
/// The generator (CapabilityReportGeneratorTests) renders this into MD + HTML.
/// </summary>
public static class CapabilityItems
{
    private const string Check = "✅";
    private const string Provider = "🟡";
    private const string Partial = "🟠";
    private const string No = "❌";
    private const string NoServerSym = "⛔";
    private const string Na = "🚫";
    private const string Sample = "✅ 本样例实库重验";
    private const string ProviderCovered = "🟡 Provider 套件已验证";

    private static readonly List<CapabilityItem> Items = [];

    private static void Add(string section, string capability, string report, string sample, string evidence, string note = "")
        => Items.Add(new CapabilityItem(section, capability, report, sample, evidence, note));

    public static IReadOnlyList<CapabilityItem> All
    {
        get
        {
            if (Items.Count == 0)
            {
                Build();
            }
            return Items;
        }
    }

    private static void Build()
    {
        BuildSection3();
        BuildSection4();
        BuildSection5();
        BuildSection6();
        BuildSection7();
        BuildSection8();
        BuildSection9();
        BuildSection10();
        BuildSection11();
        BuildSection12();
        BuildSection13();
        BuildSection14();
    }

    // ---------------------------------------------------------------- §3
    private static void BuildSection3()
    {
        const string sec = "3. 已实库确认的能力";
        Add(sec, "UseKdbndp(string)", Check, Sample, "所有测试经 EF 模块 UseKdbndp(conn)");
        Add(sec, "UseKdbndp(DbConnection)", Check, Sample, "CapabilityConnectionTransactionsTests.Use_transaction_with_external_kdbndp_connection");
        Add(sec, "Oracle 兼容模式选项", Check, Sample, "AbpBookStoreEntityFrameworkCoreModule SetOracleCompatibilityMode()");
        Add(sec, "基础 Where/Select/OrderBy/Take", Check, Sample, "CapabilityLinqAndTranslationsTests.Comparison_logic_arithmetic_and_ternary");
        Add(sec, "LIMIT/OFFSET", Check, Sample, "CapabilityLinqAndTranslationsTests.Set_ordering_and_paging");
        Add(sec, "新增/查询/修改/删除", Check, Sample, "CapabilitySaveTrackingTests.Add/Update/Remove");
        Add(sec, "手工键值 Insert", Check, Sample, "ModelBehaviorTests.BookAuthor_composite_key（Guid 客户端赋值）");
        Add(sec, "identity 主键回读", Check, ProviderCovered, "KingbaseCrudIntegrationTests.Insert_reads_database_generated_identity");
        Add(sec, "乐观并发更新条件", Check, Sample, "ModelBehaviorTests.ConcurrencyStamp_stale_update_throws");
        Add(sec, "ExecuteDelete/ExecuteDeleteAsync", Check, Sample, "CapabilitySaveTrackingTests.ExecuteDelete_and_ExecuteUpdate");
        Add(sec, "Any/Single/ToList 异步查询", Check, Sample, "CapabilityLinqAndTranslationsTests.Aggregate/Element");
        Add(sec, "基础事务性 SaveChanges", Check, Sample, "CapabilitySaveTrackingTests.SaveChanges_sync_and_async");
        Add(sec, "EnsureCreated", Check, Sample, "ModuleAndRepositoryTests.Module_creates_schema;TestDatabase.ResetSchemaAsync");
        Add(sec, "EnsureDeleted", Check, ProviderCovered, "KingbaseCrudIntegrationTests.EnsureCreated_and_EnsureDeleted_manage_the_database");
        Add(sec, "数据库创建/删除", Check, Sample, "CapabilityMigrationsTests（管理库建/删临时库）");
        Add(sec, "数据库存在性/用户表检测", Check, ProviderCovered, "KingbaseCrudIntegrationTests.EnsureCreated_creates_tables_from_the_ef_model");
        Add(sec, "字符串专用翻译", Check, Sample, "CapabilityLinqAndTranslationsTests.String_translations");
        Add(sec, "日期部件翻译", Check, Sample, "CapabilityLinqAndTranslationsTests.Date_translations");
        Add(sec, "数学函数翻译", Check, Sample, "CapabilityLinqAndTranslationsTests.Math_translations");
        Add(sec, "查询 API 契约", Check, ProviderCovered, "QueryApiContractTests（反射清单）");
        Add(sec, "聚合与集合运算", Check, Sample, "CapabilityLinqAndTranslationsTests.Aggregate/Set_ordering");
        Add(sec, ".NET 10 左右连接", Check, ProviderCovered, "KingbaseQueryTranslationIntegrationTests.Grouping_and_join_operators");
        Add(sec, "Split Query", Check, Sample, "CapabilityLinqAndTranslationsTests.Query_extensions");
        Add(sec, "EF 查询扩展", Check, Sample, "CapabilityLinqAndTranslationsTests.Query_extensions");
        Add(sec, "关系型查询扩展", Check, Sample, "CapabilityLinqAndTranslationsTests.From_sql_and_create_db_command");
        Add(sec, "CLR 标量类型往返", Check, ProviderCovered, "KingbaseTypeAndSaveIntegrationTests.Scalar_clr_types_roundtrip");
        Add(sec, "保存与批处理", Check, Sample, "CapabilitySaveTrackingTests 全部");
        Add(sec, "设计时 Provider 发现", Check, ProviderCovered, "KingbaseDesignTimeServicesTests");
    }

    // ---------------------------------------------------------------- §4
    private static void BuildSection4()
    {
        const string sec = "4. EF Core 10 新增能力专项矩阵";
        Add(sec, "Optional complex types - table splitting", Check, ProviderCovered, "KingbaseRemainingCoverageIntegrationTests.Optional_struct_complex_equality_and_bulk_assignment_work");
        Add(sec, "Complex types 映射 JSON", No, No, "无 EF JSON SQL tree", "报告结论：不支持");
        Add(sec, "Complex type struct", Check, ProviderCovered, "KingbaseRemainingCoverageIntegrationTests.Optional_struct_complex_equality_and_bulk_assignment_work");
        Add(sec, "Complex type structural equality", Check, ProviderCovered, "同上");
        Add(sec, "Complex type bulk assignment", Check, ProviderCovered, "同上");
        Add(sec, "参数化集合默认多参数翻译", Check, ProviderCovered, "KingbaseQueryTranslationIntegrationTests.Microsecond_nanosecond_null_compensation_and_parameter_collections");
        Add(sec, ".NET 10 LeftJoin", Check, ProviderCovered, "KingbaseQueryTranslationIntegrationTests.Grouping_and_join_operators");
        Add(sec, ".NET 10 RightJoin", Check, ProviderCovered, "同上");
        Add(sec, "Split query 稳定排序改进", Check, ProviderCovered, "KingbaseQueryTranslationIntegrationTests.Ef_query_extensions");
        Add(sec, "DateOnly.ToDateTime 翻译", Check, ProviderCovered, "KingbaseQueryTranslationIntegrationTests.Date_time_methods_and_current_time");
        Add(sec, "DateOnly.DayNumber", Check, ProviderCovered, "KingbaseQueryTranslationIntegrationTests.Microsecond_nanosecond_null_compensation_and_parameter_collections");
        Add(sec, "微秒/纳秒 DatePart 翻译", Check, ProviderCovered, "同上");
        Add(sec, "string 方法接收 char", Check, ProviderCovered, "KingbaseQueryTranslationIntegrationTests.String_translations");
        Add(sec, "JSON 列 ExecuteUpdate", No, No, "无 JSON 更新 SQL", "报告结论：不支持");
        Add(sec, "Named query filters", Check, ProviderCovered, "KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance");
        Add(sec, "ExecuteUpdateAsync 普通 lambda", Check, Sample, "CapabilitySaveTrackingTests.ExecuteDelete_and_ExecuteUpdate");
        Add(sec, "参数名简化", Check, ProviderCovered, "Provider 自身测试（SQL 观察）");
        Add(sec, "原始 SQL 拼接分析器警告", Check, ProviderCovered, "编译期能力，与 Provider 无关");
        Add(sec, "内联常量日志脱敏", Check, ProviderCovered, "EF Core 日志能力");
    }

    // ---------------------------------------------------------------- §5
    private static void BuildSection5()
    {
        const string sec = "5. Queryable 标准运算符兼容矩阵";
        void NoServer(string cap) => Add(sec, cap, NoServerSym, NoServerSym, "EF Core 10 无关系型翻译入口");
        void Covered(string cap, string? ev = null) => Add(sec, cap, Check, ProviderCovered, ev ?? "KingbaseQueryTranslationIntegrationTests.Aggregate_and_element_operators / Set_ordering_and_paging_operators");
        void Rechecked(string cap, string ev) => Add(sec, cap, Check, Sample, ev);

        NoServer("Aggregate"); NoServer("AggregateBy"); NoServer("Append"); NoServer("Chunk");
        Rechecked("All", "CapabilityLinqAndTranslationsTests.Aggregate_operators");
        Rechecked("Any", "CapabilityLinqAndTranslationsTests.Aggregate_operators");
        Add(sec, "AsQueryable", Check, ProviderCovered, "LINQ 管线操作");
        Rechecked("Average", "CapabilityLinqAndTranslationsTests.Aggregate_operators");
        Covered("Cast");
        NoServer("CountBy");
        Rechecked("Concat", "CapabilityLinqAndTranslationsTests.Set_ordering_and_paging");
        Rechecked("Contains", "CapabilityLinqAndTranslationsTests.Set_ordering_and_paging / String_translations");
        Rechecked("Count", "CapabilityLinqAndTranslationsTests.Aggregate_operators");
        NoServer("DistinctBy");
        Covered("DefaultIfEmpty");
        Rechecked("Distinct", "CapabilityLinqAndTranslationsTests.Set_ordering_and_paging");
        NoServer("ExceptBy");
        Rechecked("ElementAt", "CapabilityLinqAndTranslationsTests.Element_operators");
        Rechecked("ElementAtOrDefault", "CapabilityLinqAndTranslationsTests.Element_operators");
        Rechecked("Except", "CapabilityLinqAndTranslationsTests.Set_ordering_and_paging");
        Rechecked("First", "CapabilityLinqAndTranslationsTests.Element_operators");
        Rechecked("FirstOrDefault", "CapabilityLinqAndTranslationsTests.Element_operators");
        Rechecked("GroupBy", "CapabilityLinqAndTranslationsTests.Join_grouping_and_select_many");
        Covered("GroupJoin", "KingbaseQueryTranslationIntegrationTests.Remaining_relational_queryable_shapes");
        NoServer("Index");
        Rechecked("Intersect", "CapabilityLinqAndTranslationsTests.Set_ordering_and_paging");
        NoServer("IntersectBy");
        Rechecked("Join", "CapabilityLinqAndTranslationsTests.Join_grouping_and_select_many");
        Rechecked("Last", "CapabilityLinqAndTranslationsTests.Element_operators");
        Rechecked("LastOrDefault", "CapabilityLinqAndTranslationsTests.Element_operators");
        Covered("LeftJoin", "KingbaseQueryTranslationIntegrationTests.Grouping_and_join_operators");
        Rechecked("LongCount", "CapabilityLinqAndTranslationsTests.Aggregate_operators");
        Rechecked("Max", "CapabilityLinqAndTranslationsTests.Aggregate_operators");
        NoServer("MaxBy");
        Rechecked("Min", "CapabilityLinqAndTranslationsTests.Aggregate_operators");
        NoServer("MinBy");
        Covered("OfType");
        Covered("Order");
        Rechecked("OrderBy", "CapabilityLinqAndTranslationsTests.Set_ordering_and_paging");
        Rechecked("OrderByDescending", "CapabilityLinqAndTranslationsTests.Set_ordering_and_paging");
        Covered("OrderDescending");
        NoServer("Prepend");
        Rechecked("Reverse", "CapabilityLinqAndTranslationsTests.Set_ordering_and_paging");
        Covered("RightJoin", "KingbaseQueryTranslationIntegrationTests.Grouping_and_join_operators");
        Rechecked("Select", "CapabilityLinqAndTranslationsTests.Comparison_logic / String_translations");
        Rechecked("SelectMany", "CapabilityLinqAndTranslationsTests.Join_grouping_and_select_many");
        NoServer("SequenceEqual");
        NoServer("Shuffle");
        Rechecked("Single", "CapabilityLinqAndTranslationsTests.Element_operators");
        Rechecked("SingleOrDefault", "CapabilityLinqAndTranslationsTests.Element_operators");
        Rechecked("Skip", "CapabilityLinqAndTranslationsTests.Set_ordering_and_paging");
        NoServer("SkipLast"); NoServer("SkipWhile");
        Rechecked("Sum", "CapabilityLinqAndTranslationsTests.Aggregate_operators");
        Rechecked("Take", "CapabilityLinqAndTranslationsTests.Set_ordering_and_paging");
        NoServer("TakeLast"); NoServer("TakeWhile");
        Rechecked("ThenBy", "CapabilityLinqAndTranslationsTests.Set_ordering_and_paging");
        Rechecked("ThenByDescending", "CapabilityLinqAndTranslationsTests.Set_ordering_and_paging");
        Rechecked("Union", "CapabilityLinqAndTranslationsTests.Set_ordering_and_paging");
        NoServer("UnionBy");
        Rechecked("Where", "CapabilityLinqAndTranslationsTests.Comparison_logic / String_translations");
        NoServer("Zip");
    }

    // ---------------------------------------------------------------- §6
    private static void BuildSection6()
    {
        const string sec6_1 = "6.1 EntityFrameworkQueryableExtensions";
        void Rechecked(string cap, string ev) => Add(sec6_1, cap, Check, Sample, ev);
        void Covered(string cap) => Add(sec6_1, cap, Check, ProviderCovered, "KingbaseQueryTranslationIntegrationTests.Ef_query_extensions_execute_on_kingbase");

        Rechecked("AllAsync", "CapabilityLinqAndTranslationsTests.Aggregate_operators");
        Rechecked("AnyAsync", "CapabilityLinqAndTranslationsTests.Aggregate_operators");
        Covered("AsAsyncEnumerable");
        Rechecked("AsNoTracking", "CapabilityLinqAndTranslationsTests.Query_extensions");
        Rechecked("AsNoTrackingWithIdentityResolution", "CapabilityLinqAndTranslationsTests.Query_extensions");
        Rechecked("AsTracking", "CapabilityLinqAndTranslationsTests.Query_extensions");
        Rechecked("AverageAsync", "CapabilityLinqAndTranslationsTests.Aggregate_operators");
        Rechecked("ContainsAsync", "CapabilityLinqAndTranslationsTests.Aggregate_operators");
        Rechecked("CountAsync", "CapabilityLinqAndTranslationsTests.Aggregate_operators");
        Rechecked("ElementAtAsync", "CapabilityLinqAndTranslationsTests.Element_operators");
        Rechecked("ElementAtOrDefaultAsync", "CapabilityLinqAndTranslationsTests.Element_operators");
        Rechecked("ExecuteDelete", "CapabilitySaveTrackingTests.ExecuteDelete_and_ExecuteUpdate");
        Rechecked("ExecuteDeleteAsync", "CapabilitySaveTrackingTests.ExecuteDelete_and_ExecuteUpdate");
        Rechecked("ExecuteUpdate", "CapabilitySaveTrackingTests.ExecuteDelete_and_ExecuteUpdate");
        Rechecked("ExecuteUpdateAsync", "CapabilitySaveTrackingTests.ExecuteDelete_and_ExecuteUpdate");
        Rechecked("FirstAsync", "CapabilityLinqAndTranslationsTests.Element_operators");
        Rechecked("FirstOrDefaultAsync", "CapabilityLinqAndTranslationsTests.Element_operators");
        Rechecked("ForEachAsync", "CapabilityLinqAndTranslationsTests.Query_extensions");
        Covered("IgnoreAutoIncludes");
        Rechecked("IgnoreQueryFilters", "CapabilityLinqAndTranslationsTests.Ignore_query_filters_reveals_soft_deleted");
        Rechecked("Include", "CapabilityLinqAndTranslationsTests.Query_extensions");
        Rechecked("LastAsync", "CapabilityLinqAndTranslationsTests.Element_operators");
        Rechecked("LastOrDefaultAsync", "CapabilityLinqAndTranslationsTests.Element_operators");
        Rechecked("Load", "CapabilityLinqAndTranslationsTests.Query_extensions");
        Rechecked("LoadAsync", "CapabilityLinqAndTranslationsTests.Query_extensions");
        Rechecked("LongCountAsync", "CapabilityLinqAndTranslationsTests.Aggregate_operators");
        Rechecked("MaxAsync", "CapabilityLinqAndTranslationsTests.Aggregate_operators");
        Rechecked("MinAsync", "CapabilityLinqAndTranslationsTests.Aggregate_operators");
        Rechecked("SingleAsync", "CapabilityLinqAndTranslationsTests.Element_operators");
        Rechecked("SingleOrDefaultAsync", "CapabilityLinqAndTranslationsTests.Element_operators");
        Rechecked("SumAsync", "CapabilityLinqAndTranslationsTests.Aggregate_operators");
        Rechecked("TagWith", "CapabilityLinqAndTranslationsTests.Query_extensions");
        Rechecked("TagWithCallSite", "CapabilityLinqAndTranslationsTests.Query_extensions");
        Rechecked("ThenInclude", "CapabilityLinqAndTranslationsTests.Query_extensions");
        Rechecked("ToArrayAsync", "CapabilityLinqAndTranslationsTests.Query_extensions");
        Rechecked("ToDictionaryAsync", "CapabilityLinqAndTranslationsTests.Query_extensions");
        Rechecked("ToHashSetAsync", "CapabilityLinqAndTranslationsTests.Query_extensions");
        Rechecked("ToListAsync", "全测试通用");
        Rechecked("ToQueryString", "CapabilityLinqAndTranslationsTests.Query_extensions");

        const string sec6_2 = "6.2 RelationalQueryableExtensions";
        void Rechecked2(string cap, string ev) => Add(sec6_2, cap, Check, Sample, ev);
        Rechecked2("AsSingleQuery", "CapabilityLinqAndTranslationsTests.Query_extensions");
        Rechecked2("AsSplitQuery", "CapabilityLinqAndTranslationsTests.Query_extensions");
        Rechecked2("CreateDbCommand", "CapabilityLinqAndTranslationsTests.From_sql_and_create_db_command");
        Rechecked2("FromSql", "CapabilityLinqAndTranslationsTests.From_sql_and_create_db_command");
        Rechecked2("FromSqlInterpolated", "CapabilityLinqAndTranslationsTests.From_sql_and_create_db_command");
        Rechecked2("FromSqlRaw", "CapabilityLinqAndTranslationsTests.From_sql_and_create_db_command");
    }

    // ---------------------------------------------------------------- §7
    private static void BuildSection7()
    {
        const string sec = "7. 表达式、运算符和函数翻译";
        void Rechecked(string cap, string ev, string note = "") => Add(sec, cap, Check, Sample, ev, note);
        void Covered(string cap, string note = "") => Add(sec, cap, Check, ProviderCovered, "KingbaseQueryTranslationIntegrationTests（对应专项）", note);

        Rechecked("== != > >= < <=", "CapabilityLinqAndTranslationsTests.Comparison_logic");
        Rechecked("&& || !", "CapabilityLinqAndTranslationsTests.Comparison_logic");
        Rechecked("+ - * / %", "CapabilityLinqAndTranslationsTests.Comparison_logic", "real % integer 无操作符(42883),取模用 (int) 转型");
        Rechecked("三元条件 ?:", "CapabilityLinqAndTranslationsTests.Comparison_logic");
        Rechecked("null 合并 ??", "CapabilityLinqAndTranslationsTests.Null_coalesce");
        Rechecked("null 比较与补偿", "CapabilityLinqAndTranslationsTests.Element_operators");
        Rechecked("字符串拼接", "CapabilityLinqAndTranslationsTests.Comparison_logic");
        Rechecked("string.Equals", "CapabilityLinqAndTranslationsTests.Comparison_logic");
        Rechecked("Contains/StartsWith/EndsWith", "CapabilityLinqAndTranslationsTests.String_translations");
        Rechecked("ToLower/ToUpper", "CapabilityLinqAndTranslationsTests.String_translations");
        Rechecked("Substring/Replace/Trim/IndexOf", "CapabilityLinqAndTranslationsTests.String_translations");
        Rechecked("string.Length", "CapabilityLinqAndTranslationsTests.String_translations");
        Rechecked("EF.Functions.Like", "CapabilityLinqAndTranslationsTests.Ef_functions_guid_enum");
        Rechecked("EF.Functions.Collate", "CapabilityLinqAndTranslationsTests.Ef_functions_guid_enum");
        Rechecked("EF.Functions.Random", "CapabilityLinqAndTranslationsTests.Ef_functions_guid_enum");
        Add(sec, "Math.*", Partial, ProviderCovered, "KingbaseQueryTranslationIntegrationTests.Math_translations", "已实现并验证 abs/ceil/floor/round/sqrt/cbrt/log/log2/max/min/clamp/三角");
        Rechecked("DateTime.* 成员/方法", "CapabilityLinqAndTranslationsTests.Date_translations");
        Covered("DateOnly/TimeOnly 方法");
        Rechecked("Guid.NewGuid()", "CapabilityLinqAndTranslationsTests.Ef_functions_guid_enum");
        Rechecked("Enum HasFlag/ToString", "CapabilityLinqAndTranslationsTests.Ef_functions_guid_enum");
        Covered("Regex");
        Covered("byte array SequenceEqual");
        Covered("全文检索");
        Covered("数组运算");
        Covered("range 运算");
        Add(sec, "JSON path/query", Partial, ProviderCovered, "KingbaseRemainingCoverageIntegrationTests.Json_array_list_range_and_fulltext_types_roundtrip", "标量 JSON 单层提取已验证");
        Add(sec, "窗口函数", No, No, "无 EF 通用公开窗口表达式入口", "报告结论：不支持");
        Covered("LATERAL/APPLY");
    }

    // ---------------------------------------------------------------- §8
    private static void BuildSection8()
    {
        const string sec = "8. 模型构建语法矩阵";
        void Rechecked(string cap, string ev) => Add(sec, cap, Check, Sample, ev);
        void Covered(string cap) => Add(sec, cap, Check, ProviderCovered, "KingbaseModelIntegrationTests.Model_facets_constraints_relationships_filters_views_and_inheritance / Tpt_tpc_entity_splitting_and_owned_table");

        Rechecked("DbSet<TEntity> / Entity<TEntity>", "BookStoreDbContext");
        Rechecked("表名、schema、列名", "BookStoreDbContext ToTable/列映射;ModelAndDdlTests");
        Rechecked("主键、复合主键", "Book/BookAuthor;ModelBehaviorTests.BookAuthor_composite_key");
        Covered("Alternate Key / Unique Constraint");
        Covered("Keyless Entity");
        Rechecked("Required/Optional 属性", "Book.Name IsRequired;Author.ShortBio 可空");
        Covered("Shadow Property");
        Covered("Indexer Property / Property Bag");
        Covered("Backing Field / Field-only");
        Rechecked("Column Type/Name/Order", "ModelAndDdlTests.Relational_store_types");
        Rechecked("MaxLength / Unicode", "BookConsts.MaxNameLength=128 -> varchar(128)");
        Covered("Precision/Scale");
        Covered("Default Value / Default SQL");
        Covered("Computed Column");
        Rechecked("ValueGeneratedNever", "Guid 主键客户端赋值");
        Covered("ValueGeneratedOnAdd identity");
        Covered("Sequence");
        Add(sec, "HiLo", No, No, "无 Kingbase HiLo/value generator", "报告结论：不支持");
        Covered("GUID 数据库生成");
        Rechecked("Concurrency Token", "ModelBehaviorTests.ConcurrencyStamp_stale_update_throws");
        Add(sec, "RowVersion/Timestamp", No, No, "无统一 rowversion 实现", "报告结论：不支持");
        Covered("Check Constraint");
        Rechecked("Index：普通/唯一/复合", "BookStoreDbContext HasIndex(Name)");
        Covered("Index：descending/filter/include");
        Covered("一对一");
        Rechecked("一对多", "Book-BookAuthor;Query_extensions Include");
        Covered("多对多/Skip Navigation");
        Covered("自引用关系");
        Rechecked("Cascade/Restrict/SetNull", "BookAuthor OnDelete(Cascade);ModelBehaviorTests");
        Covered("Owned Entity");
        Covered("Complex Type table splitting");
        Add(sec, "Complex Type JSON", No, No, "不支持", "报告结论：不支持");
        Covered("Entity/Table Splitting");
        Covered("TPH inheritance");
        Covered("TPT inheritance");
        Covered("TPC inheritance");
        Covered("Discriminator");
        Rechecked("Global Query Filter", "CapabilityLinqAndTranslationsTests.Ignore_query_filters_reveals_soft_deleted;Soft_delete_hides_books");
        Covered("Named Query Filters (EF10)");
        Covered("Value Converter");
        Covered("Value Comparer");
        Covered("Data Seeding HasData");
        Covered("UseSeeding/UseAsyncSeeding");
        Covered("Database Function mapping");
        Covered("View mapping");
        Add(sec, "Stored procedure CUD mapping", No, No, "无存储过程更新 SQL", "报告结论：不支持");
        Add(sec, "Temporal Table", Na, Na, "SQL Server 专有", "报告结论：不适用");
        Add(sec, "Spatial/NetTopologySuite", No, No, "无空间扩展包和类型映射", "报告结论：不支持");
        Add(sec, "JSON document model", Partial, ProviderCovered, "KingbaseRemainingCoverageIntegrationTests.Json_array_list_range_and_fulltext_types_roundtrip", "标量 JsonDocument 已验证");
        Add(sec, "Primitive Collections", Partial, ProviderCovered, "同上", "int[]/List<int> 已验证");
    }

    // ---------------------------------------------------------------- §9
    private static void BuildSection9()
    {
        const string sec = "9. CLR 与数据库类型支持";
        void Scalar(string cap, string store, string sample, string ev)
            => Add(sec, cap + " → " + store, Check, sample, ev);
        Scalar("bool", "boolean", Sample, "Soft_delete_hides_books（IsDeleted）");
        Scalar("byte", "smallint", ProviderCovered, "KingbaseTypeAndSaveIntegrationTests.Scalar_clr_types_roundtrip");
        Scalar("short", "smallint", ProviderCovered, "同上");
        Scalar("int", "integer", ProviderCovered, "同上");
        Scalar("long", "bigint", ProviderCovered, "同上");
        Scalar("float", "real", Sample, "Book.Price 往返;Aggregate/Comparison");
        Scalar("double", "double precision", ProviderCovered, "同上");
        Scalar("decimal", "numeric(p,s)", ProviderCovered, "同上");
        Scalar("string", "text/varchar(n)/char(n)", Sample, "Book.Name varchar(128)");
        Scalar("Guid", "uuid", Sample, "Book.Id/Author.Id 往返");
        Scalar("byte[]", "bytea", ProviderCovered, "同上");
        Scalar("DateOnly", "date", ProviderCovered, "同上");
        Scalar("TimeOnly", "time without time zone", ProviderCovered, "同上");
        Scalar("TimeSpan", "interval", ProviderCovered, "同上");
        Scalar("DateTime", "timestamp without time zone", Sample, "Book.PublishDate 往返;Date_translations");
        Scalar("DateTimeOffset", "timestamp with time zone", ProviderCovered, "同上");
        Scalar("Nullable 上述类型", "对应类型", ProviderCovered, "同上");
        Scalar("char", "character(1)", ProviderCovered, "同上");
        Scalar("unsigned integers", "smallint/integer/bigint/numeric(20,0)", ProviderCovered, "同上");
        Scalar("enum", "integer", Sample, "Book.Type 往返;Ef_functions_guid_enum");
        Scalar("JsonDocument/JsonElement", "jsonb", ProviderCovered, "KingbaseRemainingCoverageIntegrationTests.Json_array_list_range_and_fulltext_types_roundtrip");
        Scalar("int[] / List<int>", "integer[]", ProviderCovered, "同上");
        Scalar("KdbndpRange<int>", "int4range", ProviderCovered, "同上");
        Add(sec, "Spatial 类型", No, No, "实例未装空间扩展", "报告结论：不支持");
        Add(sec, "NodaTime 类型", No, No, "需 NodaTime 包及插件", "报告结论：需独立交付");
    }

    // ---------------------------------------------------------------- §10
    private static void BuildSection10()
    {
        const string sec = "10. 保存、跟踪和批量操作";
        void Rechecked(string cap, string ev) => Add(sec, cap, Check, Sample, ev);
        void Covered(string cap) => Add(sec, cap, Check, ProviderCovered, "KingbaseTypeAndSaveIntegrationTests.Save_tracking_batching_concurrency_and_cascade");

        Rechecked("Add/AddAsync/AddRange", "CapabilitySaveTrackingTests.Add_and_AddRange");
        Rechecked("Attach/AttachRange", "CapabilitySaveTrackingTests.Attach_and_AttachRange");
        Rechecked("Update/UpdateRange", "CapabilitySaveTrackingTests.Update_and_UpdateRange");
        Rechecked("Remove/RemoveRange", "CapabilitySaveTrackingTests.Remove_and_RemoveRange");
        Rechecked("SaveChanges/SaveChangesAsync", "CapabilitySaveTrackingTests.SaveChanges_sync_and_async");
        Rechecked("ChangeTracker DetectChanges", "CapabilitySaveTrackingTests.DetectChanges");
        Covered("Snapshot/Notification tracking");
        Covered("Identity Resolution");
        Covered("Relationship fixup");
        Covered("Temporary values");
        Covered("Store-generated identity");
        Covered("Store-generated default/computed");
        Rechecked("Optimistic concurrency success", "CapabilitySaveTrackingTests.Update_and_UpdateRange");
        Rechecked("DbUpdateConcurrencyException", "ModelBehaviorTests.ConcurrencyStamp_stale_update_throws（ABP 包装为 AbpDbConcurrencyException）");
        Rechecked("Cascade delete client/database", "ModelBehaviorTests.BookAuthor_composite_key;CapabilitySaveTrackingTests.Remove_and_RemoveRange");
        Rechecked("Batching", "CapabilitySaveTrackingTests.SaveChanges_sync_and_async");
        Rechecked("ExecuteDelete", "CapabilitySaveTrackingTests.ExecuteDelete_and_ExecuteUpdate");
        Rechecked("ExecuteUpdate", "CapabilitySaveTrackingTests.ExecuteDelete_and_ExecuteUpdate");
        Add(sec, "JSON ExecuteUpdate", No, No, "不支持", "报告结论：不支持");
        Add(sec, "Stored procedure SaveChanges", No, No, "不支持", "报告结论：不支持");
    }

    // ---------------------------------------------------------------- §11
    private static void BuildSection11()
    {
        const string sec = "11. 连接、事务、原始 SQL 和执行策略";
        void Rechecked(string cap, string ev) => Add(sec, cap, Check, Sample, ev);
        void Covered(string cap, string? ev = null) => Add(sec, cap, Check, ProviderCovered, ev ?? "KingbaseFacadeAndMigrationsIntegrationTests.Facade_transaction_raw_sql_timeout_compiled_query_and_interceptors_work");

        Rechecked("OpenConnection/CloseConnection 同步/异步", "CapabilityConnectionTransactionsTests.Connection_lifecycle");
        Rechecked("BeginTransaction/Commit/Rollback", "CapabilityConnectionTransactionsTests.Begin_transaction");
        Rechecked("Savepoint", "CapabilityConnectionTransactionsTests.Savepoints");
        Covered("Ambient TransactionScope");
        Rechecked("UseTransaction", "CapabilityConnectionTransactionsTests.Use_transaction_with_external_kdbndp_connection");
        Rechecked("Execution Strategy/Retry", "CapabilityConnectionTransactionsTests.Execution_strategy");
        Rechecked("Command timeout", "CapabilityConnectionTransactionsTests.Connection_lifecycle");
        Covered("Connection pooling", "KingbaseRemainingCoverageIntegrationTests.Query_transaction_interceptors_and_connection_pool_concurrency_work");
        Covered("多主机/故障转移", "KingbaseRemainingCoverageIntegrationTests.Data_source_and_single_host_multi_host_source_work");
        Rechecked("FromSql/FromSqlRaw/FromSqlInterpolated", "CapabilityLinqAndTranslationsTests.From_sql_and_create_db_command");
        Rechecked("SqlQuery/SqlQueryRaw", "CapabilityConnectionTransactionsTests.Raw_sql");
        Rechecked("ExecuteSql/ExecuteSqlRaw/Interpolated", "CapabilityConnectionTransactionsTests.Raw_sql");
        Covered("Query/Command/Transaction Interceptor", "TestSaveChangesInterceptor（样例）;KingbaseFacadeAndMigrationsIntegrationTests");
        Covered("Compiled Query");
        Covered("Compiled Model");
        Covered("Query precompilation/NativeAOT");
    }

    // ---------------------------------------------------------------- §12
    private static void BuildSection12()
    {
        const string sec = "12. RelationalDatabaseFacadeExtensions 全清单";
        void Rechecked(string cap, string ev) => Add(sec, cap, Check, Sample, ev);
        void Covered(string cap) => Add(sec, cap, Check, ProviderCovered, "KingbaseFacadeAndMigrationsIntegrationTests.Facade_transaction_raw_sql_timeout_compiled_query_and_interceptors_work");

        Rechecked("BeginTransaction", "CapabilityConnectionTransactionsTests.Begin_transaction");
        Rechecked("BeginTransactionAsync", "CapabilityConnectionTransactionsTests.Begin_transaction");
        Rechecked("CloseConnection", "CapabilityConnectionTransactionsTests.Connection_lifecycle");
        Rechecked("CloseConnectionAsync", "CapabilityConnectionTransactionsTests.Connection_lifecycle");
        Rechecked("ExecuteSql", "CapabilityConnectionTransactionsTests.Raw_sql");
        Rechecked("ExecuteSqlAsync", "CapabilityConnectionTransactionsTests.Raw_sql");
        Rechecked("ExecuteSqlInterpolated", "CapabilityConnectionTransactionsTests.Raw_sql");
        Rechecked("ExecuteSqlInterpolatedAsync", "CapabilityConnectionTransactionsTests.Raw_sql");
        Rechecked("ExecuteSqlRaw", "CapabilityConnectionTransactionsTests.Raw_sql");
        Rechecked("ExecuteSqlRawAsync", "CapabilityConnectionTransactionsTests.Raw_sql");
        Rechecked("GenerateCreateScript", "CapabilityConnectionTransactionsTests.Connection_lifecycle;ModelAndDdlTests");
        Covered("GetAppliedMigrations");
        Covered("GetAppliedMigrationsAsync");
        Rechecked("GetCommandTimeout", "CapabilityConnectionTransactionsTests.Connection_lifecycle");
        Rechecked("GetConnectionString", "CapabilityConnectionTransactionsTests.Connection_lifecycle");
        Rechecked("GetDbConnection", "CapabilityConnectionTransactionsTests.Connection_lifecycle");
        Covered("GetMigrations");
        Covered("GetPendingMigrations");
        Covered("GetPendingMigrationsAsync");
        Covered("HasPendingModelChanges");
        Rechecked("IsRelational", "CapabilityConnectionTransactionsTests.Connection_lifecycle");
        Rechecked("Migrate", "CapabilityMigrationsTests.Initial_create_migration");
        Rechecked("MigrateAsync", "CapabilityMigrationsTests.Initial_create_migration");
        Rechecked("OpenConnection", "CapabilityConnectionTransactionsTests.Connection_lifecycle");
        Rechecked("OpenConnectionAsync", "CapabilityConnectionTransactionsTests.Connection_lifecycle");
        Rechecked("SetCommandTimeout", "CapabilityConnectionTransactionsTests.Connection_lifecycle");
        Covered("SetConnectionString");
        Covered("SetDbConnection");
        Rechecked("SqlQuery", "CapabilityConnectionTransactionsTests.Raw_sql");
        Rechecked("SqlQueryRaw", "CapabilityConnectionTransactionsTests.Raw_sql");
        Rechecked("UseTransaction", "CapabilityConnectionTransactionsTests.Use_transaction_with_external_kdbndp_connection");
        Rechecked("UseTransactionAsync", "CapabilityConnectionTransactionsTests.Use_transaction_with_external_kdbndp_connection");
    }

    // ---------------------------------------------------------------- §13
    private static void BuildSection13()
    {
        const string sec = "13. Migration 全部操作类型兼容矩阵";
        void Covered(string cap) => Add(sec, cap, Check, ProviderCovered, "KingbaseFacadeAndMigrationsIntegrationTests.Migration_history_upgrade_script_and_rollback_work / Migration_operation_sql_executes");
        void Unsupported(string cap) => Add(sec, cap, No, No, "数据库不能在线修改 collation", "报告结论：不支持");

        Add(sec, "CreateTableOperation（InitialCreate 应用）", Check, Sample, "CapabilityMigrationsTests.Initial_create_migration");
        Covered("AddCheckConstraintOperation");
        Covered("AddColumnOperation");
        Covered("AddForeignKeyOperation");
        Covered("AddPrimaryKeyOperation");
        Covered("AddUniqueConstraintOperation");
        Covered("AlterColumnOperation");
        Unsupported("AlterDatabaseOperation");
        Covered("AlterSequenceOperation");
        Covered("AlterTableOperation");
        Covered("CreateIndexOperation");
        Covered("CreateSequenceOperation");
        Covered("CreateTableOperation");
        Covered("DeleteDataOperation");
        Covered("DropCheckConstraintOperation");
        Covered("DropColumnOperation");
        Covered("DropForeignKeyOperation");
        Covered("DropIndexOperation");
        Covered("DropPrimaryKeyOperation");
        Covered("DropSchemaOperation");
        Covered("DropSequenceOperation");
        Covered("DropTableOperation");
        Covered("DropUniqueConstraintOperation");
        Covered("EnsureSchemaOperation");
        Covered("InsertDataOperation");
        Covered("RenameColumnOperation");
        Covered("RenameIndexOperation");
        Covered("RenameSequenceOperation");
        Covered("RenameTableOperation");
        Covered("RestartSequenceOperation");
        Covered("SqlOperation");
        Covered("UpdateDataOperation");
    }

    // ---------------------------------------------------------------- §14
    private static void BuildSection14()
    {
        const string sec = "14. Database First、设计时和工具链";
        void Covered(string cap, string ev = "KingbaseScaffoldingIntegrationTests / KingbaseDesignTimeServicesTests / dotnet ef") => Add(sec, cap, Check, ProviderCovered, ev);

        Covered("dotnet ef 识别 Provider");
        Covered("dotnet ef migrations add", "样例已内置 InitialCreate 迁移");
        Covered("dotnet ef database update", "KingbaseFacadeAndMigrationsIntegrationTests");
        Covered("dotnet ef migrations script");
        Covered("dotnet ef dbcontext scaffold");
        Covered("Provider 配置代码生成");
        Covered("Annotation C# 代码生成");
        Covered("Reverse engineer table/view/key/index");
        Covered("Reverse engineer sequence");
        Covered("Compiled model optimize");
        Covered("NativeAOT optimize/precompiled queries");
    }
}
