using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;
using Kdbndp;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace Kingbase.EntityFrameworkCore.Design.Internal;

public sealed class KingbaseDatabaseModelFactory : IDatabaseModelFactory
{
    public DatabaseModel Create(string connectionString, DatabaseModelFactoryOptions options)
    {
        using var connection = new KdbndpConnection(connectionString);
        return Create(connection, options);
    }

    public DatabaseModel Create(DbConnection connection, DatabaseModelFactoryOptions options)
    {
        var close = connection.State == ConnectionState.Closed;
        if (close) connection.Open();
        try
        {
            var model = new DatabaseModel
            {
                DatabaseName = Scalar<string>(connection, "SELECT current_database()"),
                DefaultSchema = Scalar<string>(connection, "SELECT current_schema()")
            };
            LoadTables(connection, model, options);
            LoadColumns(connection, model);
            LoadConstraints(connection, model);
            LoadIndexes(connection, model);
            LoadForeignKeys(connection, model);
            LoadSequences(connection, model, options);
            return model;
        }
        finally
        {
            if (close) connection.Close();
        }
    }

    private static void LoadTables(DbConnection connection, DatabaseModel model, DatabaseModelFactoryOptions options)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT table_schema, table_name, table_type FROM information_schema.tables WHERE table_schema NOT IN ('sys_catalog','pg_catalog','information_schema') AND table_schema NOT LIKE 'sys%' ORDER BY table_schema, table_name";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var schema = reader.GetString(0);
            var name = reader.GetString(1);
            if (!Selected(schema, name, options)) continue;
            DatabaseTable table = reader.GetString(2).Equals("VIEW", StringComparison.OrdinalIgnoreCase) ? new DatabaseView() : new DatabaseTable();
            table.Database = model;
            table.Schema = schema;
            table.Name = name;
            model.Tables.Add(table);
        }
    }

    private static void LoadColumns(DbConnection connection, DatabaseModel model)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT table_schema, table_name, column_name, data_type, udt_name, is_nullable, column_default, is_identity, is_generated, generation_expression, character_maximum_length, numeric_precision, numeric_scale FROM information_schema.columns ORDER BY table_schema, table_name, ordinal_position";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var table = FindTable(model, reader.GetString(0), reader.GetString(1));
            if (table is null) continue;
            var storeType = BuildStoreType(reader);
            var generated = !reader.IsDBNull(8) && !reader.GetString(8).Equals("NEVER", StringComparison.OrdinalIgnoreCase);
            table.Columns.Add(new DatabaseColumn
            {
                Table = table,
                Name = reader.GetString(2),
                StoreType = storeType,
                IsNullable = reader.GetString(5).Equals("YES", StringComparison.OrdinalIgnoreCase),
                DefaultValueSql = reader.IsDBNull(6) ? null : reader.GetString(6),
                ValueGenerated = reader.GetString(7).Equals("YES", StringComparison.OrdinalIgnoreCase) ? Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd : null,
                ComputedColumnSql = generated && !reader.IsDBNull(9) ? reader.GetString(9) : null,
                IsStored = generated ? true : null
            });
        }
    }

    private static void LoadConstraints(DbConnection connection, DatabaseModel model)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT tc.table_schema, tc.table_name, tc.constraint_name, tc.constraint_type, kcu.column_name FROM information_schema.table_constraints tc JOIN information_schema.key_column_usage kcu ON kcu.constraint_schema=tc.constraint_schema AND kcu.constraint_name=tc.constraint_name AND kcu.table_schema=tc.table_schema AND kcu.table_name=tc.table_name WHERE tc.constraint_type IN ('PRIMARY KEY','UNIQUE') ORDER BY tc.table_schema,tc.table_name,tc.constraint_name,kcu.ordinal_position";
        using var reader = command.ExecuteReader();
        foreach (var group in ReadGroups(reader, 0, 1, 2, 3, 4))
        {
            var table = FindTable(model, group.Schema, group.Table);
            if (table is null) continue;
            var columns = group.Columns.Select(name => table.Columns.Single(column => column.Name == name)).ToArray();
            if (group.Kind == "PRIMARY KEY")
            {
                table.PrimaryKey = new DatabasePrimaryKey { Table = table, Name = group.Name };
                foreach (var column in columns) table.PrimaryKey.Columns.Add(column);
            }
            else
            {
                var unique = new DatabaseUniqueConstraint { Table = table, Name = group.Name };
                foreach (var column in columns) unique.Columns.Add(column);
                table.UniqueConstraints.Add(unique);
            }
        }
    }

    private static void LoadIndexes(DbConnection connection, DatabaseModel model)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT schemaname, tablename, indexname, indexdef FROM sys_indexes ORDER BY schemaname, tablename, indexname";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var table = FindTable(model, reader.GetString(0), reader.GetString(1));
            if (table is null) continue;
            var name = reader.GetString(2);
            if (table.PrimaryKey?.Name == name || table.UniqueConstraints.Any(constraint => constraint.Name == name)) continue;
            var definition = reader.GetString(3);
            var index = new DatabaseIndex { Table = table, Name = name, IsUnique = definition.StartsWith("CREATE UNIQUE INDEX", StringComparison.OrdinalIgnoreCase) };
            var columnList = Regex.Match(definition, @"\((?<columns>.*)\)(?:\s+WHERE|$)").Groups["columns"].Value;
            foreach (Match match in Regex.Matches(columnList, "\"(?<name>[^\"]+)\"(?<desc>\\s+DESC)?"))
            {
                var column = table.Columns.FirstOrDefault(candidate => candidate.Name == match.Groups["name"].Value);
                if (column is null) continue;
                index.Columns.Add(column);
                index.IsDescending.Add(match.Groups["desc"].Success);
            }
            var where = Regex.Match(definition, @"\sWHERE\s(?<filter>.+)$", RegexOptions.IgnoreCase);
            index.Filter = where.Success ? where.Groups["filter"].Value : null;
            table.Indexes.Add(index);
        }
    }

    private static void LoadForeignKeys(DbConnection connection, DatabaseModel model)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT tc.table_schema,tc.table_name,tc.constraint_name,kcu.column_name,ccu.table_schema,ccu.table_name,ccu.column_name,rc.delete_rule,kcu.ordinal_position FROM information_schema.table_constraints tc JOIN information_schema.key_column_usage kcu ON kcu.constraint_schema=tc.constraint_schema AND kcu.constraint_name=tc.constraint_name JOIN information_schema.referential_constraints rc ON rc.constraint_schema=tc.constraint_schema AND rc.constraint_name=tc.constraint_name JOIN information_schema.constraint_column_usage ccu ON ccu.constraint_schema=rc.unique_constraint_schema AND ccu.constraint_name=rc.unique_constraint_name WHERE tc.constraint_type='FOREIGN KEY' ORDER BY tc.table_schema,tc.table_name,tc.constraint_name,kcu.ordinal_position";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var table = FindTable(model, reader.GetString(0), reader.GetString(1));
            var principal = FindTable(model, reader.GetString(4), reader.GetString(5));
            if (table is null || principal is null) continue;
            var name = reader.GetString(2);
            var foreignKey = table.ForeignKeys.FirstOrDefault(candidate => candidate.Name == name);
            if (foreignKey is null)
            {
                foreignKey = new DatabaseForeignKey { Table = table, PrincipalTable = principal, Name = name, OnDelete = ParseAction(reader.GetString(7)) };
                table.ForeignKeys.Add(foreignKey);
            }
            foreignKey.Columns.Add(table.Columns.Single(column => column.Name == reader.GetString(3)));
            foreignKey.PrincipalColumns.Add(principal.Columns.Single(column => column.Name == reader.GetString(6)));
        }
    }

    private static void LoadSequences(DbConnection connection, DatabaseModel model, DatabaseModelFactoryOptions options)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT sequence_schema,sequence_name,data_type,start_value,minimum_value,maximum_value,increment,cycle_option FROM information_schema.sequences ORDER BY sequence_schema,sequence_name";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var schema = reader.GetString(0); var name = reader.GetString(1);
            if (options.Schemas.Any() && !options.Schemas.Contains(schema, StringComparer.OrdinalIgnoreCase)) continue;
            model.Sequences.Add(new DatabaseSequence { Database = model, Schema = schema, Name = name, StoreType = reader.GetString(2), StartValue = Convert.ToInt64(reader.GetValue(3)), MinValue = Convert.ToInt64(reader.GetValue(4)), MaxValue = Convert.ToInt64(reader.GetValue(5)), IncrementBy = Convert.ToInt32(reader.GetValue(6)), IsCyclic = reader.GetString(7).Equals("YES", StringComparison.OrdinalIgnoreCase) });
        }
    }

    private static string BuildStoreType(DbDataReader reader)
    {
        var type = reader.GetString(3);
        if (!reader.IsDBNull(10) && Convert.ToInt64(reader.GetValue(10)) > 0) return $"{type}({reader.GetValue(10)})";
        if (!reader.IsDBNull(11) && !reader.IsDBNull(12) && type is "numeric" or "decimal") return $"{type}({reader.GetValue(11)},{reader.GetValue(12)})";
        return type == "USER-DEFINED" ? reader.GetString(4) : type;
    }

    private static bool Selected(string schema, string table, DatabaseModelFactoryOptions options)
        => (!options.Schemas.Any() || options.Schemas.Contains(schema, StringComparer.OrdinalIgnoreCase))
            && (!options.Tables.Any() || options.Tables.Any(value => value.Equals(table, StringComparison.OrdinalIgnoreCase) || value.Equals($"{schema}.{table}", StringComparison.OrdinalIgnoreCase)));

    private static DatabaseTable? FindTable(DatabaseModel model, string schema, string table)
        => model.Tables.FirstOrDefault(candidate => candidate.Schema == schema && candidate.Name == table);

    private static T Scalar<T>(DbConnection connection, string sql)
    {
        using var command = connection.CreateCommand(); command.CommandText = sql; return (T)Convert.ChangeType(command.ExecuteScalar()!, typeof(T));
    }

    private static IEnumerable<(string Schema, string Table, string Name, string Kind, List<string> Columns)> ReadGroups(DbDataReader reader, int schemaIndex, int tableIndex, int nameIndex, int kindIndex, int columnIndex)
    {
        var groups = new Dictionary<string, (string, string, string, string, List<string>)>(StringComparer.Ordinal);
        while (reader.Read())
        {
            var key = $"{reader.GetString(schemaIndex)}\0{reader.GetString(tableIndex)}\0{reader.GetString(nameIndex)}";
            if (!groups.TryGetValue(key, out var group)) group = (reader.GetString(schemaIndex), reader.GetString(tableIndex), reader.GetString(nameIndex), reader.GetString(kindIndex), []);
            group.Item5.Add(reader.GetString(columnIndex)); groups[key] = group;
        }
        return groups.Values.Select(group => (group.Item1, group.Item2, group.Item3, group.Item4, group.Item5));
    }

    private static ReferentialAction ParseAction(string action) => action switch { "CASCADE" => ReferentialAction.Cascade, "SET NULL" => ReferentialAction.SetNull, "SET DEFAULT" => ReferentialAction.SetDefault, "RESTRICT" => ReferentialAction.Restrict, _ => ReferentialAction.NoAction };
}
