﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using grate.Infrastructure;

namespace grate.Configuration
{
    public class GrateConfiguration
    {
        private readonly string? _adminConnectionString = null;

        //public KnownFolders KnownFolders { get; set; } = InCurrentDirectory();
        public KnownFolders? KnownFolders { get; set; }
        
        public DatabaseType DatabaseType { get; init; } = DatabaseType.sqlserver;
        //public DatabaseType DatabaseType { get; init; }
        
        public DirectoryInfo SqlFilesDirectory { get; init; } = CurrentDirectory;
        //public DirectoryInfo? SqlFilesDirectory { get; init; }
        
        public DirectoryInfo OutputPath { get; init; } = new(Path.Combine(CurrentDirectory.FullName, "output"));
        //public DirectoryInfo? OutputPath { get; set; }
        
        public string? ConnectionString { get; init; } = null;

        public string SchemaName { get; init; } = "grate";

        public string? AdminConnectionString
        {
            get => _adminConnectionString ?? WithAdminDb(ConnectionString);
            init => _adminConnectionString = value;
        }

        private static string? WithAdminDb(string? connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return connectionString;
            }
            var pattern = new Regex("(.*;\\s*(?:Initial Catalog|Database)=)([^;]*)(.*)");
            var replaced = pattern.Replace(connectionString, "$1master$3");
            return replaced;
        }

        public static GrateConfiguration Default => new();
        public bool CreateDatabase { get; init; } = true;
        public bool AlterDatabase { get; init; } = false;
        public bool Transaction { get; init; } = false;
        public IEnumerable<GrateEnvironment> Environments { get; init; } = Enumerable.Empty<GrateEnvironment>();
        public string Version { get; init; } = "0.0.0.1";
        
        public int CommandTimeout { get; set; }
        public int AdminCommandTimeout { get; set; }
        
        
        //private static KnownFolders InCurrentDirectory() => KnownFolders.In(CurrentDirectory);
        private static DirectoryInfo CurrentDirectory => new(Directory.GetCurrentDirectory());
    }
}