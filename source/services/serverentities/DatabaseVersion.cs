using System.ComponentModel.DataAnnotations;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class DatabaseVersion
    {
        [Key]
        public string VersionType { get; set; }
        public string VersionString { get; set; }
        public string Status { get; set; }

        // VersionType values
        public const string Schema = "Schema";
        public const string Constants = "Constants";

        // Status values
        public const string Corrupted = "Corrupted";
        public const string OK = "OK";
    }
}