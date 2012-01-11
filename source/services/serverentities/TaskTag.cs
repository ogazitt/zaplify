using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class TaskTag
    {
        public Guid ID { get; set; }
        public Guid TaskID { get; set; }
        public Guid TagID { get; set; }
    }
}