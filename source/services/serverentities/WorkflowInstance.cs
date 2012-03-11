using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class WorkflowInstance
    {
        public Guid ID { get; set; }
        public string WorkflowType { get; set; }
        public string State { get; set; }
        public Guid ItemID { get; set; }
        public string Name { get; set; }
        public string Body { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
    }
}