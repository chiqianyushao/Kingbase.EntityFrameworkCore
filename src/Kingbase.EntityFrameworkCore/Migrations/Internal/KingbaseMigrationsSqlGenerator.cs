using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Metadata;
using Kingbase.EntityFrameworkCore.Metadata.Internal;

namespace Kingbase.EntityFrameworkCore.Migrations.Internal;

public sealed class KingbaseMigrationsSqlGenerator(MigrationsSqlGeneratorDependencies dependencies)
    : MigrationsSqlGenerator(dependencies)
{
    protected override void Generate(CreateIndexOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        builder.Append("CREATE ");
        if (operation.IsUnique) builder.Append("UNIQUE ");
        builder.Append("INDEX ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name))
            .Append(" ON ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Table, operation.Schema))
            .Append(" (");
        for (var index = 0; index < operation.Columns.Length; index++)
        {
            if (index > 0) builder.Append(", ");
            builder.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Columns[index]));
            if (operation.IsDescending is { Length: > 0 } && operation.IsDescending[index]) builder.Append(" DESC");
        }
        builder.Append(")");
        if (operation[KingbaseAnnotationNames.IndexInclude] is string[] includeProperties && includeProperties.Length > 0)
        {
            builder.Append(" INCLUDE (");
            for (var index = 0; index < includeProperties.Length; index++)
            {
                if (index > 0) builder.Append(", ");
                builder.Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(includeProperties[index]));
            }
            builder.Append(")");
        }
        if (!string.IsNullOrWhiteSpace(operation.Filter)) builder.Append(" WHERE ").Append(operation.Filter);
        EndKingbaseStatement(builder, terminate);
    }
    protected override void Generate(DropIndexOperation operation, IModel? model, MigrationCommandListBuilder builder, bool terminate = true)
    {
        builder.Append("DROP INDEX ").Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema));
        EndKingbaseStatement(builder, terminate);
    }

    protected override void Generate(RenameIndexOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        builder.Append("ALTER INDEX ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema))
            .Append(" RENAME TO ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.NewName ?? throw new InvalidOperationException("A new index name is required.")));
        EndKingbaseStatement(builder, true);
    }

    protected override void Generate(RenameColumnOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        builder.Append("ALTER TABLE ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Table, operation.Schema))
            .Append(" RENAME COLUMN ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name))
            .Append(" TO ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.NewName ?? throw new InvalidOperationException("A new column name is required.")));
        EndKingbaseStatement(builder, true);
    }

    protected override void Generate(RenameSequenceOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        builder.Append("ALTER SEQUENCE ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema))
            .Append(" RENAME TO ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.NewName ?? throw new InvalidOperationException("A new sequence name is required.")));
        EndKingbaseStatement(builder, true);
    }

    protected override void Generate(RenameTableOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        var currentName = Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema);
        if (!string.IsNullOrWhiteSpace(operation.NewName) && operation.NewName != operation.Name)
        {
            builder.Append("ALTER TABLE ").Append(currentName).Append(" RENAME TO ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.NewName));
            EndKingbaseStatement(builder, true);
            currentName = Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.NewName, operation.Schema);
        }

        if (operation.NewSchema != operation.Schema && operation.NewSchema is not null)
        {
            builder.Append("ALTER TABLE ").Append(currentName).Append(" SET SCHEMA ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.NewSchema));
            EndKingbaseStatement(builder, true);
        }
    }

    protected override void Generate(RestartSequenceOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        builder.Append("ALTER SEQUENCE ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema))
            .Append(" RESTART WITH ")
            .Append(Convert.ToString(operation.StartValue, System.Globalization.CultureInfo.InvariantCulture)!);
        EndKingbaseStatement(builder, true);
    }

    protected override void Generate(DropSchemaOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        builder.Append("DROP SCHEMA ").Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name));
        EndKingbaseStatement(builder, true);
    }

    protected override void Generate(AlterColumnOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        var table = Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Table, operation.Schema);
        var column = Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name);
        if (!operation.IsNullable && operation.OldColumn.IsNullable && (operation.DefaultValue is not null || operation.DefaultValueSql is not null))
        {
            builder.Append("UPDATE ").Append(table).Append(" SET ").Append(column).Append(" = ")
                .Append(GetDefaultValueSql(operation)).Append(" WHERE ").Append(column).Append(" IS NULL");
            EndKingbaseStatement(builder, true);
        }
        if (operation.ColumnType != operation.OldColumn.ColumnType && operation.ColumnType is not null)
        {
            builder.Append("ALTER TABLE ").Append(table).Append(" ALTER COLUMN ").Append(column)
                .Append(" TYPE ").Append(operation.ColumnType);
            EndKingbaseStatement(builder, true);
        }

        if (operation.IsNullable != operation.OldColumn.IsNullable)
        {
            builder.Append("ALTER TABLE ").Append(table).Append(" ALTER COLUMN ").Append(column)
                .Append(operation.IsNullable ? " DROP NOT NULL" : " SET NOT NULL");
            EndKingbaseStatement(builder, true);
        }

        if (operation.DefaultValueSql != operation.OldColumn.DefaultValueSql || !Equals(operation.DefaultValue, operation.OldColumn.DefaultValue))
        {
            builder.Append("ALTER TABLE ").Append(table).Append(" ALTER COLUMN ").Append(column);
            if (operation.DefaultValueSql is not null)
            {
                builder.Append(" SET DEFAULT ").Append(operation.DefaultValueSql);
            }
            else if (operation.DefaultValue is not null)
            {
                builder.Append(" SET DEFAULT ").Append(GetDefaultValueSql(operation));
            }
            else
            {
                builder.Append(" DROP DEFAULT");
            }
            EndKingbaseStatement(builder, true);
        }
    }

    private string GetDefaultValueSql(ColumnOperation operation)
    {
        if (operation.DefaultValueSql is not null)
        {
            return operation.DefaultValueSql;
        }

        var mapping = Dependencies.TypeMappingSource.FindMapping(operation.ClrType)
            ?? throw new InvalidOperationException($"No type mapping exists for {operation.ClrType}.");
        return mapping.GenerateSqlLiteral(operation.DefaultValue!);
    }

    protected override void Generate(AlterTableOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        if (operation.Comment == operation.OldTable.Comment)
        {
            return;
        }

        builder.Append("COMMENT ON TABLE ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema))
            .Append(" IS ")
            .Append(operation.Comment is null ? "NULL" : $"'{operation.Comment.Replace("'", "''", StringComparison.Ordinal)}'");
        EndKingbaseStatement(builder, true);
    }

    protected override void Generate(SqlOperation operation, IModel? model, MigrationCommandListBuilder builder)
    {
        builder.Append(operation.Sql.TrimEnd());
        if (!operation.Sql.TrimEnd().EndsWith(Dependencies.SqlGenerationHelper.StatementTerminator, StringComparison.Ordinal))
        {
            builder.Append(Dependencies.SqlGenerationHelper.StatementTerminator);
        }
        builder.AppendLine().EndCommand(operation.SuppressTransaction);
    }
    protected override void Generate(
        EnsureSchemaOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        builder
            .Append("CREATE SCHEMA IF NOT EXISTS ")
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name))
            .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
            .EndCommand();
    }

    protected override void ComputedColumnDefinition(
        string? schema,
        string table,
        string name,
        ColumnOperation operation,
        IModel? model,
        MigrationCommandListBuilder builder)
    {
        builder
            .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(name))
            .Append(" ")
            .Append(operation.ColumnType ?? throw new InvalidOperationException("Computed columns require a store type."))
            .Append(" GENERATED ALWAYS AS (")
            .Append(operation.ComputedColumnSql ?? throw new InvalidOperationException("Computed columns require SQL."))
            .Append(") STORED");
    }

    private void EndKingbaseStatement(MigrationCommandListBuilder builder, bool terminate)
    {
        if (terminate)
        {
            builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator).EndCommand();
        }
    }

}
