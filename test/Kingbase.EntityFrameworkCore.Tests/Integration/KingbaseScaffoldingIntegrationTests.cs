using Kdbndp;
using Kingbase.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Scaffolding;

namespace Kingbase.EntityFrameworkCore.Tests.Integration;

public sealed class KingbaseScaffoldingIntegrationTests
{
    [Fact]
    public async Task Database_model_factory_reads_tables_views_keys_indexes_foreign_keys_and_sequences()
    {
        var connectionString = Environment.GetEnvironmentVariable("KINGBASE_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        await using (var connection = new KdbndpConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                DROP VIEW IF EXISTS "efcore_scaffold_view";
                DROP TABLE IF EXISTS "efcore_scaffold_child";
                DROP TABLE IF EXISTS "efcore_scaffold_parent";
                DROP SEQUENCE IF EXISTS "efcore_scaffold_seq";
                CREATE SEQUENCE "efcore_scaffold_seq" START WITH 5 INCREMENT BY 3;
                CREATE TABLE "efcore_scaffold_parent" (
                    "Id" integer NOT NULL,
                    "Code" varchar(30) NOT NULL,
                    "Amount" numeric(12,2) NULL,
                    CONSTRAINT "PK_scaffold_parent" PRIMARY KEY ("Id"),
                    CONSTRAINT "AK_scaffold_parent_Code" UNIQUE ("Code")
                );
                CREATE TABLE "efcore_scaffold_child" (
                    "Id" integer NOT NULL,
                    "ParentId" integer NOT NULL,
                    CONSTRAINT "PK_scaffold_child" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_scaffold_child_parent" FOREIGN KEY ("ParentId") REFERENCES "efcore_scaffold_parent" ("Id") ON DELETE CASCADE
                );
                CREATE INDEX "IX_scaffold_child_ParentId" ON "efcore_scaffold_child" ("ParentId" DESC);
                CREATE VIEW "efcore_scaffold_view" AS SELECT "Id", "Code" FROM "efcore_scaffold_parent";
                """;
            await command.ExecuteNonQueryAsync();
        }

        var factory = new KingbaseDatabaseModelFactory();
        var model = factory.Create(connectionString, new DatabaseModelFactoryOptions(
            ["efcore_scaffold_parent", "efcore_scaffold_child", "efcore_scaffold_view"],
            ["public"]));

        Assert.Equal("efcore10_kingbase_dev", model.DatabaseName);
        Assert.Equal("public", model.DefaultSchema);
        var parent = Assert.Single(model.Tables, table => table.Name == "efcore_scaffold_parent");
        Assert.Equal("PK_scaffold_parent", parent.PrimaryKey?.Name);
        Assert.Contains(parent.UniqueConstraints, constraint => constraint.Name == "AK_scaffold_parent_Code");
        Assert.Equal("numeric(12,2)", parent.Columns.Single(column => column.Name == "Amount").StoreType);
        var child = Assert.Single(model.Tables, table => table.Name == "efcore_scaffold_child");
        var index = Assert.Single(child.Indexes, candidate => candidate.Name == "IX_scaffold_child_ParentId");
        Assert.True(index.IsDescending.Single());
        var foreignKey = Assert.Single(child.ForeignKeys);
        Assert.Equal("FK_scaffold_child_parent", foreignKey.Name);
        Assert.Equal("efcore_scaffold_parent", foreignKey.PrincipalTable.Name);
        Assert.Contains(model.Tables, table => table.Name == "efcore_scaffold_view" && table.GetType().Name == "DatabaseView");
        var sequence = Assert.Single(model.Sequences, candidate => candidate.Name == "efcore_scaffold_seq");
        Assert.Equal(5, sequence.StartValue);
        Assert.Equal(3, sequence.IncrementBy);
    }

    [Fact]
    public void Provider_code_generator_emits_UseKdbndp()
    {
        var generator = new KingbaseProviderCodeGenerator();
        var fragment = generator.GenerateUseProvider("Name=ConnectionStrings:Kingbase");
        Assert.Equal("UseKdbndp", fragment.Method);
        Assert.Equal("Name=ConnectionStrings:Kingbase", fragment.Arguments.Single());
    }
}

