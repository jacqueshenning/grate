using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using moo.Configuration;
using moo.Infrastructure;
using moo.Migration;
using NSubstitute;
using NUnit.Framework;

namespace moo.unittests.SqlServer
{
    [TestFixture]
    public class MigrationTable
    {
        private MooConfiguration? _config;
        private static string? AdminConnectionString() => $"Data Source=localhost,{MooTestContext.SqlServer.Port};Initial Catalog=master;User Id=sa;Password={MooTestContext.SqlServer.AdminPassword}";
        private static string? ConnectionString(string database) => $"Data Source=localhost,{MooTestContext.SqlServer.Port};Initial Catalog={database};User Id=sa;Password={MooTestContext.SqlServer.AdminPassword}";

        [TestCase("ScriptsRun")]
        [TestCase("ScriptsRunErrors")]
        [TestCase("Version")]
        public async Task Is_created_if_it_does_not_exist(string tableName)
        {
            var db = "MonoBonoJono";
            var fullTableName = "moo." + tableName;
            
            var knownFolders = KnownFolders.In(CreateRandomTempDirectory());

            await using (var migrator = GetMigrator(db, true, knownFolders))
            {
                await migrator.Migrate();
            }

            IEnumerable<string> scripts;
            string sql = $"SELECT modified_date FROM {fullTableName}";
            
            await using (var conn = new SqlConnection(ConnectionString(db)))
            {
                scripts = await conn.QueryAsync<string>(sql);
            }
            scripts.Should().NotBeNull();
        }
        
        
        [TestCase("ScriptsRun")]
        [TestCase("ScriptsRunErrors")]
        [TestCase("Version")]
        public async Task Is_created_even_if_scripts_fail(string tableName)
        {
            var db = "DatabaseWithFailingScripts";
            var fullTableName = "moo." + tableName;
            
            var knownFolders = KnownFolders.In(CreateRandomTempDirectory());
            CreateInvalidSql(knownFolders.Up);

            await using (var migrator = GetMigrator(db, true, knownFolders))
            {
                try
                {
                    await migrator.Migrate();
                }
                catch (SqlException)
                {
                }
            }

            IEnumerable<string> scripts;
            string sql = $"SELECT modified_date FROM {fullTableName}";
            
            await using (var conn = new SqlConnection(ConnectionString(db)))
            {
                scripts = await conn.QueryAsync<string>(sql);
            }
            scripts.Should().NotBeNull();
        }
        
        [TestCase("ScriptsRun")]
        [TestCase("ScriptsRunErrors")]
        [TestCase("Version")]
        public async Task Migration_does_not_fail_if_table_already_exists(string tableName)
        {
            var db = "MonoBonoJono";

            var knownFolders = KnownFolders.In(CreateRandomTempDirectory());
            
            await using (var migrator = GetMigrator(db, true, knownFolders))
            {
                await migrator.Migrate();
            }
            
            // Run migration again - make sure it does not throw an exception
            await using (var migrator = GetMigrator(db, true, knownFolders))
            {
                Assert.DoesNotThrowAsync(() => migrator.Migrate());
            }
        }
        
        [Test()]
        public async Task Inserts_version_in_version_table()
        {
            var db = "BooYaTribe";

            var knownFolders = KnownFolders.In(CreateRandomTempDirectory());
            
            await using (var migrator = GetMigrator(db, true, knownFolders))
            {
                await migrator.Migrate();
            }

            IEnumerable<string> entries;
            string sql = $"SELECT version FROM moo.Version";
            
            await using (var conn = new SqlConnection(ConnectionString(db)))
            {
                entries = await conn.QueryAsync<string>(sql);
            }

            var versions = entries.ToList();
            versions.Should().HaveCount(1);
            versions.FirstOrDefault().Should().Be("a.b.c.d");
        }

   private MooMigrator GetMigrator(string databaseName, bool createDatabase, KnownFolders knownFolders)
        {
            var connectionString = ConnectionString(databaseName);

            var dbLogger = new NullLogger<SqlServerDatabase>();
            var factory = Substitute.For<IFactory>();
            factory.GetService<DatabaseType, IDatabase>(DatabaseType.sqlserver)
                .Returns(new SqlServerDatabase(dbLogger));

            var dbMigrator = new DbMigrator(factory, new NullLogger<DbMigrator>(), new HashGenerator());
            var migrator = new MooMigrator(new NullLogger<MooMigrator>(), dbMigrator);

            _config = new MooConfiguration()
            {
                CreateDatabase = createDatabase, 
                ConnectionString = connectionString,
                AdminConnectionString = AdminConnectionString(),
                Version = "a.b.c.d",
                KnownFolders = knownFolders,
                AlterDatabase = true
            };


            dbMigrator.ApplyConfig(_config);

            return migrator;
        }

        private static DirectoryInfo CreateRandomTempDirectory()
        {
            var dummyFile = Path.GetTempFileName();
            File.Delete(dummyFile);

            var scriptsDir = Directory.CreateDirectory(dummyFile);
            return scriptsDir;
        }

        private static void CreateDummySql(MigrationsFolder? folder)
        {
            var dummySql = "SELECT @@VERSION";
            var path = MakeSurePathExists(folder);
            WriteSql(path, "1_jalla.sql", dummySql);
        }
        
        private static void CreateInvalidSql(MigrationsFolder? folder)
        {
            var dummySql = "SELECT TOP";
            var path = MakeSurePathExists(folder);
            WriteSql(path, "2_failing.sql", dummySql);
        }

        private static void WriteSql(DirectoryInfo path, string filename, string? sql)
        {
            File.WriteAllText(Path.Combine(path.ToString(), filename), sql);
        }

        private static DirectoryInfo MakeSurePathExists(MigrationsFolder? folder)
        {
            var path = folder?.Path ?? throw new ArgumentException(nameof(folder.Path));

            if (!path.Exists)
            {
                path.Create();
            }

            return path;
        }
        
    }
}