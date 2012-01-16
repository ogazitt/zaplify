using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class ActionType
    {
        public int ActionTypeID { get; set; }
        public string ActionName { get; set; }
        public string DisplayName { get; set; }
        public string FieldName { get; set; }
        public int SortOrder { get; set; }
    }
}