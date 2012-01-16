using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class FolderUser
    {
        public Guid ID { get; set; }
        public Guid FolderID { get; set; }
        public Guid UserID { get; set; }
        public int PermissionID { get; set; }
    }
}