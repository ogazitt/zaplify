using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class Permission
    {
        // permission types
        public static int View = 1;
        public static int Modify = 2;
        public static int Full = 3;

        public int PermissionID { get; set; }
        public string Name { get; set; }
    }
}