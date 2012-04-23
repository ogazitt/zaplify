using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class DatabaseVersion
    {
        [Key]
        public string VersionString { get; set; }
        public string Updating { get; set; }

        public const string Corrupted = "Corrupted";
        public const string OK = "OK";
    }
}