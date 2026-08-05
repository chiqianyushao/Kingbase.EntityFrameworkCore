using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using Kingbase.EntityFrameworkCore.Infrastructure.Internal;
using Kingbase.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kingbase.EntityFrameworkCore.Tests.Infrastructure;

public sealed class KingbaseOptionsTests
{
    [Fact]
    public void UseKdbndp_adds_provider_extension_with_connection_string()
    {
        var builder = new DbContextOptionsBuilder().UseKdbndp("Server=localhost;Database=TEST");

        var extension = builder.Options.FindExtension<KingbaseOptionsExtension>();

        Assert.NotNull(extension);
        Assert.Equal("Server=localhost;Database=TEST", extension.ConnectionString);
        Assert.True(extension.Info.IsDatabaseProvider);
    }

    [Fact]
    public void UseKdbndp_accepts_external_connection()
    {
        using var connection = new TestDbConnection();

        var builder = new DbContextOptionsBuilder().UseKdbndp(connection);
        var extension = builder.Options.FindExtension<KingbaseOptionsExtension>();

        Assert.Same(connection, extension?.Connection);
        Assert.False(extension?.IsConnectionOwned);
    }

    [Fact]
    public void Compatibility_mode_is_configured_by_the_provider_builder()
    {
        var builder = new DbContextOptionsBuilder()
            .UseKdbndp("Server=localhost", options => options.SetOracleCompatibilityMode());

        var extension = builder.Options.FindExtension<KingbaseOptionsExtension>();

        Assert.Equal(KingbaseCompatibilityMode.Oracle, extension?.CompatibilityMode);
    }

    [Fact]
    public void DbContext_resolves_the_external_connection()
    {
        using var connection = new TestDbConnection();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseKdbndp(connection)
            .Options;

        using var context = new TestDbContext(options);

        Assert.Same(connection, context.Database.GetDbConnection());
        Assert.Equal("Kingbase.EntityFrameworkCore", context.Database.ProviderName);
    }

    [Fact]
    public void DbContext_compiles_a_basic_linq_query()
    {
        using var connection = new TestDbConnection();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseKdbndp(connection)
            .Options;

        using var context = new TestDbContext(options);
        var sql = context.Entities
            .Where(entity => entity.Id > 10)
            .OrderBy(entity => entity.Name)
            .Take(5)
            .ToQueryString();

        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LIMIT @", sql, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestEntity> Entities => Set<TestEntity>();
    }

    private sealed class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestDbConnection : DbConnection
    {
        [AllowNull]
        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "TEST";
        public override string DataSource => "Test";
        public override string ServerVersion => "V9R1C10";
        public override ConnectionState State => ConnectionState.Closed;
        public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
        protected override DbCommand CreateDbCommand() => new TestDbCommand(this);
    }

    private sealed class TestDbCommand(DbConnection connection) : DbCommand
    {
        private readonly TestDbParameterCollection _parameters = new();

        [AllowNull]
        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        [AllowNull]
        protected override DbConnection DbConnection { get; set; } = connection;
        protected override DbParameterCollection DbParameterCollection => _parameters;
        protected override DbTransaction? DbTransaction { get; set; }
        public override void Cancel() { }
        public override int ExecuteNonQuery() => throw new NotSupportedException();
        public override object? ExecuteScalar() => throw new NotSupportedException();
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => new TestDbParameter();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
    }

    private sealed class TestDbParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        [AllowNull]
        public override string ParameterName { get; set; } = string.Empty;
        [AllowNull]
        public override string SourceColumn { get; set; } = string.Empty;
        public override object? Value { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override int Size { get; set; }
        public override void ResetDbType() { }
    }

    private sealed class TestDbParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _items = [];

        public override int Count => _items.Count;
        public override object SyncRoot => ((ICollection)_items).SyncRoot;
        public override int Add(object value)
        {
            _items.Add((DbParameter)value);
            return _items.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (var value in values)
            {
                Add(value!);
            }
        }

        public override void Clear() => _items.Clear();
        public override bool Contains(object value) => _items.Contains((DbParameter)value);
        public override bool Contains(string value) => IndexOf(value) >= 0;
        public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
        public override IEnumerator GetEnumerator() => _items.GetEnumerator();
        public override int IndexOf(object value) => _items.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName)
            => _items.FindIndex(parameter => parameter.ParameterName == parameterName);
        public override void Insert(int index, object value) => _items.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _items.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _items.RemoveAt(index);
        public override void RemoveAt(string parameterName)
        {
            var index = IndexOf(parameterName);
            if (index >= 0)
            {
                RemoveAt(index);
            }
        }

        protected override DbParameter GetParameter(int index) => _items[index];
        protected override DbParameter GetParameter(string parameterName) => _items[IndexOf(parameterName)];
        protected override void SetParameter(int index, DbParameter value) => _items[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var index = IndexOf(parameterName);
            if (index < 0)
            {
                _items.Add(value);
            }
            else
            {
                _items[index] = value;
            }
        }
    }
}
