using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class WorkflowType
    {
        [Key]
        public string Type { get; set; }
        public string Definition { get; set; }
    }
}