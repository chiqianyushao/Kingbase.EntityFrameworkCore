using System.Text;

namespace AbpBookStore.EntityFrameworkCore.Tests.Capability;

/// <summary>
/// Offline report generator: reads the capability matrix declared in
/// CapabilityItems and renders two files into the repo's docs/ folder:
///   docs/AbpBookStore-Capability-Results.md
///   docs/AbpBookStore-Capability-Results.html  (standalone, inline CSS)
///
/// The test runs without a database (the matrix is static data + test-evidence
/// names), so `dotnet test` stays green offline. The output directory can be
/// overridden with the ABP_CAPABILITY_REPORT_OUTPUT environment variable.
/// </summary>
public sealed class CapabilityReportGeneratorTests(ITestOutputHelper output)
{
    private const string OutputDirEnvVar = "ABP_CAPABILITY_REPORT_OUTPUT";
    private const string MdFileName = "AbpBookStore-Capability-Results.md";
    private const string HtmlFileName = "AbpBookStore-Capability-Results.html";

    [Fact]
    public async Task Renders_capability_matrix_to_markdown_and_html()
    {
        var outputDir = ResolveOutputDirectory();
        var items = CapabilityItems.All;

        var md = RenderMarkdown(items);
        var html = RenderHtml(items, md);

        var mdPath = Path.Combine(outputDir, MdFileName);
        var htmlPath = Path.Combine(outputDir, HtmlFileName);
        await File.WriteAllTextAsync(mdPath, md);
        await File.WriteAllTextAsync(htmlPath, html);

        Assert.True(File.Exists(mdPath), $"Expected {mdPath}");
        Assert.True(File.Exists(htmlPath), $"Expected {htmlPath}");
        Assert.NotEmpty(await File.ReadAllTextAsync(mdPath));
        Assert.NotEmpty(await File.ReadAllTextAsync(htmlPath));

        output.WriteLine($"MD   -> {mdPath}");
        output.WriteLine($"HTML -> {htmlPath}");
    }

    private static string ResolveOutputDirectory()
    {
        var overrideDir = Environment.GetEnvironmentVariable(OutputDirEnvVar);
        if (!string.IsNullOrWhiteSpace(overrideDir))
        {
            return overrideDir;
        }

        // Walk up from the test bin dir to the repo root (where docs/ lives).
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var docs = Path.Combine(directory.FullName, "docs");
            if (Directory.Exists(docs))
            {
                return docs;
            }
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the repo docs/ directory. Set ABP_CAPABILITY_REPORT_OUTPUT to an explicit path.");
    }

    private static string RenderMarkdown(IReadOnlyList<CapabilityItem> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# ABP BookStore × KingbaseES — 能力矩阵实测结果(EF Core 10)");
        sb.AppendLine();
        sb.AppendLine("- 日期:2026-08-05");
        sb.AppendLine("- 样例:`samples/AbpBookStore/`(真实 ABP 10.6 + Kingbase.EntityFrameworkCore + 真实 KingbaseES)");
        sb.AppendLine("- 矩阵来源:`docs/EFCore10-KingbaseES-Compatibility-Report.md`(§3–§14)");
        sb.Append("- 共 ").Append(items.Count).AppendLine(" 项,已逐项标注本样例实测结果。");
        sb.AppendLine();

        var summary = items
            .GroupBy(i => i.SampleStatus)
            .OrderByDescending(g => g.Count())
            .Select(g => $"- **{g.Key}**:{g.Count()} 项");

        sb.AppendLine("## 汇总");
        sb.AppendLine();
        sb.AppendLine("| 状态 | 数量 |");
        sb.AppendLine("|---|---|");
        foreach (var g in items.GroupBy(i => i.SampleStatus).OrderByDescending(g => g.Count()))
        {
            sb.AppendLine($"| {g.Key} | {g.Count()} |");
        }
        sb.AppendLine();

        foreach (var section in items.GroupBy(i => i.Section))
        {
            sb.AppendLine($"## {section.Key}");
            sb.AppendLine();
            sb.AppendLine("| 能力 | 原报告 | 本样例实测 | 证据 |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var item in section)
            {
                sb.Append("| ").Append(item.Capability.Replace("|", "\\|"))
                  .Append(" | ").Append(item.ReportStatus)
                  .Append(" | ").Append(item.SampleStatus)
                  .Append(" | ").Append(item.Evidence.Replace("|", "\\|"));
                if (!string.IsNullOrEmpty(item.Note))
                {
                    sb.Append("<br/>*").Append(item.Note.Replace("|", "\\|")).Append("*");
                }
                sb.AppendLine(" |");
            }
            sb.AppendLine();
        }

        sb.AppendLine("## 状态图例");
        sb.AppendLine();
        sb.AppendLine("- ✅ 本样例实库重验:在真实 KingbaseES 上经 ABP 栈(仓储/UoW/DbContext)执行并通过。");
        sb.AppendLine("- 🟡 Provider 套件已验证:由 `test/Kingbase.EntityFrameworkCore.Tests` 自带集成测试覆盖,本样例未重复。");
        sb.AppendLine("- ⛔ EF 无服务端语义 / ❌ 不支持 / 🚫 不适用 / 🟠 部分支持:沿用原报告结论。");
        return sb.ToString();
    }

    private static string RenderHtml(IReadOnlyList<CapabilityItem> items, string markdown)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\"><head><meta charset=\"utf-8\">");
        sb.AppendLine("<title>ABP BookStore × KingbaseES — 能力矩阵实测结果</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(
            """
            body{font-family:-apple-system,'Segoe UI',Roboto,'Microsoft YaHei',sans-serif;margin:2rem auto;max-width:1100px;color:#1f2328;line-height:1.55;padding:0 1rem}
            h1{font-size:1.6rem;border-bottom:2px solid #d0d7de;padding-bottom:.4rem}
            h2{font-size:1.2rem;margin-top:2rem;border-bottom:1px solid #d0d7de;padding-bottom:.3rem}
            table{border-collapse:collapse;width:100%;margin:.6rem 0 1.4rem;font-size:.9rem}
            th,td{border:1px solid #d0d7de;padding:.35rem .6rem;text-align:left;vertical-align:top}
            th{background:#f6f8fa;font-weight:600}
            tr:nth-child(even) td{background:#fafbfc}
            code{background:#f0f2f4;border-radius:3px;padding:0 .25em;font-size:.85em}
            .note{color:#57606a;font-style:italic;font-size:.85em}
            .summary td:first-child{font-weight:600}
            """);
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<h1>ABP BookStore × KingbaseES — 能力矩阵实测结果(EF Core 10)</h1>");
        sb.AppendLine("<p>日期:2026-08-05 · 样例:<code>samples/AbpBookStore/</code>(真实 ABP 10.6 + Kingbase.EntityFrameworkCore + 真实 KingbaseES) · 矩阵来源:<code>docs/EFCore10-KingbaseES-Compatibility-Report.md</code>(§3–§14)</p>");

        sb.AppendLine("<h2>汇总</h2><table class=\"summary\"><tr><th>状态</th><th>数量</th></tr>");
        foreach (var g in items.GroupBy(i => i.SampleStatus).OrderByDescending(g => g.Count()))
        {
            sb.AppendLine($"<tr><td>{g.Key}</td><td>{g.Count()}</td></tr>");
        }
        sb.AppendLine("</table>");

        foreach (var section in items.GroupBy(i => i.Section))
        {
            sb.AppendLine($"<h2>{section.Key}</h2>");
            sb.AppendLine("<table><tr><th>能力</th><th>原报告</th><th>本样例实测</th><th>证据</th></tr>");
            foreach (var item in section)
            {
                sb.Append("<tr><td><code>")
                  .Append(System.Net.WebUtility.HtmlEncode(item.Capability))
                  .Append("</code></td><td>").Append(item.ReportStatus)
                  .Append("</td><td>").Append(item.SampleStatus)
                  .Append("</td><td>").Append(System.Net.WebUtility.HtmlEncode(item.Evidence));
                if (!string.IsNullOrEmpty(item.Note))
                {
                    sb.Append("<br/><span class=\"note\">").Append(System.Net.WebUtility.HtmlEncode(item.Note)).Append("</span>");
                }
                sb.AppendLine("</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        sb.AppendLine("<h2>状态图例</h2>");
        sb.AppendLine("<ul>");
        sb.AppendLine("<li><b>✅ 本样例实库重验</b> — 在真实 KingbaseES 上经 ABP 栈(仓储/UoW/DbContext)执行并通过。</li>");
        sb.AppendLine("<li><b>🟡 Provider 套件已验证</b> — 由 <code>test/Kingbase.EntityFrameworkCore.Tests</code> 自带集成测试覆盖,本样例未重复。</li>");
        sb.AppendLine("<li><b>⛔/❌/🚫/🟠</b> — 沿用原报告结论(EF 无服务端语义 / 不支持 / 不适用 / 部分支持)。</li>");
        sb.AppendLine("</ul>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}
