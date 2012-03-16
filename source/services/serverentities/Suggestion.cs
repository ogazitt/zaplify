using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class Suggestion
    {
        public Guid ID { get; set; }
        public Guid EntityID { get; set; }
        public string EntityType { get; set; }
        public string WorkflowType { get; set; }
        public Guid WorkflowInstanceID { get; set; }
        public string State { get; set; }
        public string FieldName { get; set; }
        public string DisplayName { get; set; }
        public string Value { get; set; }
        public DateTime? TimeSelected { get; set; }
        public string ReasonSelected { get; set; }
    }
}