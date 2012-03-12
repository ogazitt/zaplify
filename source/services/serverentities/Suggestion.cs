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
        public Guid ItemID { get; set; }
        public string WorkflowName { get; set; }
        public Guid WorkflowInstanceID { get; set; }
        public string State { get; set; }
        public string FieldName { get; set; }
        public string DisplayName { get; set; }
        public string Value { get; set; }
        public DateTime? TimeChosen { get; set; }
    }
}