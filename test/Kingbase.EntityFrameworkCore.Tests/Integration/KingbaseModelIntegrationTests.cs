using System.Data.Common;
using System.Reflection;
using Kdbndp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kingbase.EntityFrameworkCore.Tests.Integration;

[Collection("Kingbase integration")]
public sealed class KingbaseModelIntegrationTests
{
    private const string ConnectionVariable = "KINGBASE_TEST_CONNECTION";
    private const string Schema = "efcore_model_probe";
    private const string AdvancedSchema = "efcore_model_advanced_probe";

    [Fact]
    public async Task Model_facets_constraints_relationships_filters_views_and_inheritance_execute_on_kingbase()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using var connection = new KdbndpConnection(connectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, $"DROP SCHEMA IF EXISTS {Quote(Schema)} CASCADE;");

        var options = new DbContextOptionsBuilder<ModelDbContext>()
            .UseKdbndp(connection, kingbaseOptionsAction: builder => builder.SetOracleCompatibilityMode())
            .Options;

        try
        {
            await using (var context = new ModelDbContext(options))
            {
                await context.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
                await ExecuteAsync(connection,
                    $"CREATE VIEW {Quote(Schema)}.{Quote("PrincipalView")} AS SELECT {Quote("Id")}, {Quote("TenantId")}, {Quote("Name")} FROM {Quote(Schema)}.{Quote("Principals")};");

                var principal = new ModelPrincipal
                {
                    Id = 1,
                    TenantId = 1,
                    Code = "P-001",
                    Name = "Alpha",
                    Amount = 12.3456m,
                    Status = ModelStatus.Active,
                    Contact = new ContactInfo { Email = "alpha@example.com", Phone = "10086" },
                    Address = new Address { City = "Beijing", Street = "Road 1" },
                    Children = [new ModelChild { Id = 10, TenantId = 1, Name = "Child" }],
                    Profile = new ModelProfile { Id = 20, TenantId = 1, Bio = "Profile" },
                    Tags = [new ModelTag { Id = 1, Name = "Runtime" }]
                };
                principal.SetNotes("backing-field");

                context.Add(principal);
                context.Entry(principal).Property("ShadowCode").CurrentValue = "shadow";
                context.Add(new ModelNode { Id = 1, Name = "Root", Children = [new ModelNode { Id = 2, Name = "Leaf" }] });
                context.AddRange(
                    new ModelCat { Id = 1, Name = "Cat", Lives = 9 },
                    new ModelDog { Id = 2, Name = "Dog", GoodBoy = true });
                context.Set<Dictionary<string, object>>("ModelSettings").Add(new Dictionary<string, object>
                {
                    ["Id"] = 1,
                    ["Name"] = "Theme",
                    ["Value"] = "Dark"
                });

                await context.SaveChangesAsync();

                Assert.Equal(7, principal.DefaultNumber);
                Assert.Equal(42, principal.DefaultSqlNumber);
                Assert.Equal(24.6912m, principal.ComputedAmount);
                Assert.NotEqual(Guid.Empty, principal.DatabaseGuid);
                Assert.True(principal.SequenceValue >= 100);
            }

            await using (var context = new ModelDbContext(options))
            {
                var principal = await context.Principals
                    .Include(entity => entity.Children)
                    .Include(entity => entity.Profile)
                    .Include(entity => entity.Tags)
                    .SingleAsync();

                Assert.Equal("Alpha", principal.Name);
                Assert.Equal("ALPHA", await context.Principals.Select(ModelDbContext.NormalizeExpression).SingleAsync());
                Assert.Equal("shadow", context.Entry(principal).Property<string>("ShadowCode").CurrentValue);
                Assert.Equal("backing-field", principal.Notes);
                Assert.Equal("alpha@example.com", principal.Contact.Email);
                Assert.Equal("Beijing", principal.Address.City);
                Assert.Single(principal.Children);
                Assert.NotNull(principal.Profile);
                Assert.Single(principal.Tags);
                Assert.Equal(1, await context.PrincipalViews.CountAsync());
                Assert.Equal(1, await context.PrincipalSqlViews.CountAsync());
                Assert.Equal(2, await context.Animals.CountAsync());
                Assert.Single(await context.Cats.ToListAsync());
                Assert.Single(await context.Dogs.ToListAsync());
                Assert.Equal("Dark", await context.Set<Dictionary<string, object>>("ModelSettings")
                    .Select(setting => EF.Property<string>(setting, "Value"))
                    .SingleAsync());
                Assert.Equal("Seeded", (await context.Tags.SingleAsync(tag => tag.Id == 99)).Name);

                principal.IsDeleted = true;
                await context.SaveChangesAsync();
                Assert.Empty(await context.Principals.ToListAsync());
                Assert.Single(await context.Principals.IgnoreQueryFilters().ToListAsync());

                var root = await context.Nodes.SingleAsync(node => node.Id == 1);
                context.Remove(root);
                await context.SaveChangesAsync();
                Assert.Null((await context.Nodes.SingleAsync(node => node.Id == 2)).ParentId);

                context.Remove(principal.Profile!);
                await context.SaveChangesAsync();
                context.Remove(principal);
                await context.SaveChangesAsync();
                Assert.Empty(await context.Children.ToListAsync());
            }

            await AssertDatabaseMetadataAsync(connection);
        }
        finally
        {
            await ExecuteAsync(connection, $"DROP SCHEMA IF EXISTS {Quote(Schema)} CASCADE;");
        }
    }

    [Fact]
    public async Task Tpt_tpc_entity_splitting_and_owned_table_execute_on_kingbase()
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await using var connection = new KdbndpConnection(connectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, $"DROP SCHEMA IF EXISTS {Quote(AdvancedSchema)} CASCADE;");
        var options = new DbContextOptionsBuilder<AdvancedModelDbContext>()
            .UseKdbndp(connection, kingbaseOptionsAction: builder => builder.SetOracleCompatibilityMode())
            .Options;

        try
        {
            await using (var context = new AdvancedModelDbContext(options))
            {
                await context.GetService<IRelationalDatabaseCreator>().CreateTablesAsync();
                context.AddRange(
                    new SplitEntity { Id = 1, Name = "Split", Details = "Second table" },
                    new TptEmployee { Id = 1, Name = "Employee", Department = "R&D" },
                    new TpcCardPayment { Id = 1, Amount = 12.50m, LastFour = "1234" },
                    new TpcCashPayment { Id = 2, Amount = 20m, ReceivedBy = "Cashier" },
                    new ModelOrder
                    {
                        Id = 1,
                        Number = "ORDER-1",
                        ShippingAddress = new ShippingAddress { City = "Shanghai", Street = "Road 2" }
                    });
                await context.SaveChangesAsync();
            }

            await using (var context = new AdvancedModelDbContext(options))
            {
                Assert.Equal("Second table", (await context.SplitEntities.SingleAsync()).Details);
                Assert.Equal("R&D", (await context.TptPeople.OfType<TptEmployee>().SingleAsync()).Department);
                Assert.Equal(2, await context.TpcPayments.CountAsync());
                Assert.Single(await context.TpcPayments.OfType<TpcCardPayment>().ToListAsync());
                Assert.Equal("Shanghai", (await context.Orders.Include(order => order.ShippingAddress).SingleAsync()).ShippingAddress.City);
            }
        }
        finally
        {
            await ExecuteAsync(connection, $"DROP SCHEMA IF EXISTS {Quote(AdvancedSchema)} CASCADE;");
        }
    }

    private static async Task AssertDatabaseMetadataAsync(DbConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                (SELECT COUNT(*) FROM information_schema.table_constraints WHERE constraint_schema = 'efcore_model_probe' AND constraint_type = 'CHECK'),
                (SELECT COUNT(*) FROM pg_indexes WHERE schemaname = 'efcore_model_probe'),
                (SELECT COUNT(*) FROM information_schema.sequences WHERE sequence_schema = 'efcore_model_probe'),
                (SELECT COUNT(*) FROM information_schema.views WHERE table_schema = 'efcore_model_probe')
            """;
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        Assert.True(Convert.ToInt32(reader.GetValue(0)) >= 1);
        Assert.True(Convert.ToInt32(reader.GetValue(1)) >= 3);
        Assert.True(Convert.ToInt32(reader.GetValue(2)) >= 1);
        Assert.Equal(1, Convert.ToInt32(reader.GetValue(3)));
    }

    private static async Task ExecuteAsync(DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static string Quote(string identifier)
        => $"\"{identifier.Replace("\"", "\"\"")}\"";

    private sealed class ModelDbContext(DbContextOptions<ModelDbContext> options) : DbContext(options)
    {
        private static readonly MethodInfo NormalizeMethod = typeof(ModelDbContext).GetMethod(nameof(Normalize))!;

        public static readonly System.Linq.Expressions.Expression<Func<ModelPrincipal, string>> NormalizeExpression
            = principal => Normalize(principal.Name);

        public DbSet<ModelPrincipal> Principals => Set<ModelPrincipal>();
        public DbSet<ModelChild> Children => Set<ModelChild>();
        public DbSet<ModelTag> Tags => Set<ModelTag>();
        public DbSet<ModelNode> Nodes => Set<ModelNode>();
        public DbSet<ModelAnimal> Animals => Set<ModelAnimal>();
        public DbSet<ModelCat> Cats => Set<ModelCat>();
        public DbSet<ModelDog> Dogs => Set<ModelDog>();
        public DbSet<PrincipalView> PrincipalViews => Set<PrincipalView>();
        public DbSet<PrincipalSqlView> PrincipalSqlViews => Set<PrincipalSqlView>();

        public static string Normalize(string value)
            => throw new InvalidOperationException();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(Schema);
            modelBuilder.HasSequence<long>("ModelSequence").StartsAt(100).IncrementsBy(5);
            modelBuilder.HasDbFunction(NormalizeMethod).HasName("upper").HasSchema("pg_catalog");

            modelBuilder.Entity<ModelPrincipal>(entity =>
            {
                entity.ToTable("Principals", table => table.HasCheckConstraint("CK_Principals_Amount", "\"Amount\" >= 0"));
                entity.HasKey(value => new { value.Id, value.TenantId });
                entity.HasAlternateKey(value => value.Code);
                entity.Property(value => value.Id).ValueGeneratedNever().HasColumnOrder(0);
                entity.Property(value => value.TenantId).ValueGeneratedNever().HasColumnOrder(1);
                entity.Property(value => value.Code).HasMaxLength(20).IsUnicode().IsRequired();
                entity.Property(value => value.Name).HasColumnName("Name").HasMaxLength(32).IsUnicode().IsRequired();
                entity.Property(value => value.Amount).HasPrecision(18, 4).HasColumnType("numeric(18,4)");
                entity.Property(value => value.OptionalText).IsRequired(false);
                entity.Property(value => value.DefaultNumber).HasDefaultValue(7);
                entity.Property(value => value.DefaultSqlNumber).HasDefaultValueSql("42");
                entity.Property(value => value.ComputedAmount)
                    .HasComputedColumnSql("\"Amount\" * 2", stored: true);
                entity.Property(value => value.DatabaseGuid)
                    .HasDefaultValueSql("CAST(sys_guid() AS uuid)");
                entity.Property(value => value.SequenceValue)
                    .HasDefaultValueSql("nextval('efcore_model_probe.\"ModelSequence\"')");
                entity.Property(value => value.Status).HasConversion<string>().HasMaxLength(16);
                entity.Property(value => value.Version).IsConcurrencyToken();
                entity.Property<string>("ShadowCode").HasMaxLength(16);
                entity.Property(value => value.Notes).HasField("_notes").UsePropertyAccessMode(PropertyAccessMode.Field);
                entity.Property(value => value.Name).Metadata.SetValueComparer(
                    new ValueComparer<string>(
                        (left, right) => string.Equals(left, right, StringComparison.Ordinal),
                        value => value.GetHashCode(),
                        value => value));
                entity.ComplexProperty(value => value.Contact, complex =>
                {
                    complex.Property(value => value.Email).HasColumnName("ContactEmail").HasMaxLength(100);
                    complex.Property(value => value.Phone).HasColumnName("ContactPhone").HasMaxLength(32);
                });
                entity.OwnsOne(value => value.Address, owned =>
                {
                    owned.Property(value => value.City).HasColumnName("AddressCity").HasMaxLength(32);
                    owned.Property(value => value.Street).HasColumnName("AddressStreet").HasMaxLength(100);
                });
                entity.HasIndex(value => value.Name).IsUnique();
                entity.HasIndex(value => new { value.OptionalText, value.Amount })
                    .IsDescending(true, false)
                    .HasFilter("\"OptionalText\" IS NOT NULL");
                entity.HasMany(value => value.Children)
                    .WithOne(value => value.Principal)
                    .HasForeignKey(value => new { value.PrincipalId, value.TenantId })
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(value => value.Profile)
                    .WithOne(value => value.Principal)
                    .HasForeignKey<ModelProfile>(value => new { value.PrincipalId, value.TenantId })
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(value => value.Tags)
                    .WithMany(value => value.Principals)
                    .UsingEntity("PrincipalTags");
                entity.HasQueryFilter("TenantFilter", value => value.TenantId == 1);
                entity.HasQueryFilter("SoftDeleteFilter", value => !value.IsDeleted);
            });

            modelBuilder.Entity<ModelChild>(entity =>
            {
                entity.ToTable("Children");
                entity.HasKey(value => new { value.Id, value.TenantId });
                entity.Property(value => value.Id).ValueGeneratedNever();
            });
            modelBuilder.Entity<ModelProfile>(entity =>
            {
                entity.ToTable("Profiles");
                entity.HasKey(value => new { value.Id, value.TenantId });
                entity.Property(value => value.Id).ValueGeneratedNever();
            });
            modelBuilder.Entity<ModelTag>(entity =>
            {
                entity.ToTable("Tags");
                entity.Property(value => value.Id).ValueGeneratedNever();
                entity.HasData(new ModelTag { Id = 99, Name = "Seeded" });
            });
            modelBuilder.Entity<ModelNode>(entity =>
            {
                entity.ToTable("Nodes");
                entity.Property(value => value.Id).ValueGeneratedNever();
                entity.HasOne(value => value.Parent)
                    .WithMany(value => value.Children)
                    .HasForeignKey(value => value.ParentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
            modelBuilder.Entity<ModelAnimal>(entity =>
            {
                entity.ToTable("Animals");
                entity.Property(value => value.Id).ValueGeneratedNever();
                entity.HasDiscriminator<string>("AnimalType")
                    .HasValue<ModelCat>("cat")
                    .HasValue<ModelDog>("dog");
            });
            modelBuilder.Entity<PrincipalView>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("PrincipalView");
            });
            modelBuilder.Entity<PrincipalSqlView>(entity =>
            {
                entity.HasNoKey();
                entity.ToSqlQuery($"SELECT \"Id\", \"TenantId\", \"Name\" FROM \"{Schema}\".\"Principals\"");
            });
            modelBuilder.SharedTypeEntity<Dictionary<string, object>>("ModelSettings", entity =>
            {
                entity.ToTable("Settings");
                entity.IndexerProperty<int>("Id").ValueGeneratedNever();
                entity.IndexerProperty<string>("Name").HasMaxLength(32);
                entity.IndexerProperty<string>("Value").HasMaxLength(100);
                entity.HasKey("Id");
            });
        }
    }

    private sealed class AdvancedModelDbContext(DbContextOptions<AdvancedModelDbContext> options) : DbContext(options)
    {
        public DbSet<SplitEntity> SplitEntities => Set<SplitEntity>();
        public DbSet<TptPerson> TptPeople => Set<TptPerson>();
        public DbSet<TpcPayment> TpcPayments => Set<TpcPayment>();
        public DbSet<ModelOrder> Orders => Set<ModelOrder>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema(AdvancedSchema);
            modelBuilder.Entity<SplitEntity>(entity =>
            {
                entity.ToTable("SplitMain");
                entity.Property(value => value.Id).ValueGeneratedNever();
                entity.SplitToTable("SplitDetails", table => table.Property(value => value.Details));
            });
            modelBuilder.Entity<TptPerson>(entity =>
            {
                entity.UseTptMappingStrategy();
                entity.ToTable("TptPeople");
                entity.Property(value => value.Id).ValueGeneratedNever();
            });
            modelBuilder.Entity<TptEmployee>().ToTable("TptEmployees");
            modelBuilder.Entity<TpcPayment>(entity =>
            {
                entity.UseTpcMappingStrategy();
                entity.Property(value => value.Id).ValueGeneratedNever();
                entity.Property(value => value.Amount).HasPrecision(18, 2);
            });
            modelBuilder.Entity<TpcCardPayment>().ToTable("TpcCardPayments");
            modelBuilder.Entity<TpcCashPayment>().ToTable("TpcCashPayments");
            modelBuilder.Entity<ModelOrder>(entity =>
            {
                entity.ToTable("Orders");
                entity.Property(value => value.Id).ValueGeneratedNever();
                entity.OwnsOne(value => value.ShippingAddress, owned =>
                {
                    owned.ToTable("ShippingAddresses");
                    owned.Property(value => value.City).HasMaxLength(32);
                    owned.Property(value => value.Street).HasMaxLength(100);
                });
            });
        }
    }

    private sealed class ModelPrincipal
    {
        private string _notes = string.Empty;
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? OptionalText { get; set; }
        public decimal Amount { get; set; }
        public int DefaultNumber { get; set; }
        public int DefaultSqlNumber { get; set; }
        public decimal ComputedAmount { get; set; }
        public Guid DatabaseGuid { get; set; }
        public long SequenceValue { get; set; }
        public ModelStatus Status { get; set; }
        public int Version { get; set; }
        public bool IsDeleted { get; set; }
        public string Notes => _notes;
        public ContactInfo Contact { get; set; } = new();
        public Address Address { get; set; } = new();
        public List<ModelChild> Children { get; set; } = [];
        public ModelProfile? Profile { get; set; }
        public List<ModelTag> Tags { get; set; } = [];
        public void SetNotes(string value) => _notes = value;
    }

    private sealed class ContactInfo
    {
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    private sealed class Address
    {
        public string City { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
    }

    private sealed class ModelChild
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int PrincipalId { get; set; }
        public string Name { get; set; } = string.Empty;
        public ModelPrincipal Principal { get; set; } = null!;
    }

    private sealed class ModelProfile
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int PrincipalId { get; set; }
        public string Bio { get; set; } = string.Empty;
        public ModelPrincipal Principal { get; set; } = null!;
    }

    private sealed class ModelTag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<ModelPrincipal> Principals { get; set; } = [];
    }

    private sealed class ModelNode
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public ModelNode? Parent { get; set; }
        public List<ModelNode> Children { get; set; } = [];
    }

    private abstract class ModelAnimal
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class ModelCat : ModelAnimal
    {
        public int Lives { get; set; }
    }

    private sealed class ModelDog : ModelAnimal
    {
        public bool GoodBoy { get; set; }
    }

    private sealed class PrincipalView
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class PrincipalSqlView
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private enum ModelStatus
    {
        Inactive,
        Active
    }

    private sealed class SplitEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
    }

    private abstract class TptPerson
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TptEmployee : TptPerson
    {
        public string Department { get; set; } = string.Empty;
    }

    private abstract class TpcPayment
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
    }

    private sealed class TpcCardPayment : TpcPayment
    {
        public string LastFour { get; set; } = string.Empty;
    }

    private sealed class TpcCashPayment : TpcPayment
    {
        public string ReceivedBy { get; set; } = string.Empty;
    }

    private sealed class ModelOrder
    {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;
        public ShippingAddress ShippingAddress { get; set; } = new();
    }

    private sealed class ShippingAddress
    {
        public string City { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
    }
}
