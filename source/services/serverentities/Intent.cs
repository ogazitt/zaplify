using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class Intent
    {
        public long IntentID { get; set; }
        public string Name { get; set; }
        public string Verb { get; set; }
        public string Noun { get; set; }
        public string SearchFormatString { get; set; }
    }
}