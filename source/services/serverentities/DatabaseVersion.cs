using System.ComponentModel.DataAnnotations;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class DatabaseVersion
    {
        [Key]
        public string VersionString { get; set; }
        public string Status { get; set; }

        public const string Corrupted = "Corrupted";
        public const string OK = "OK";
    }
}