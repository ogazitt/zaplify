using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TaskStoreServerEntities
{
    public class Constants
    {
        public List<Action> Actions { get; set; }
        public List<Color> Colors { get; set; }
        public List<FieldType> FieldTypes { get; set; }
        public List<ListType> ListTypes { get; set; }
        public List<Priority> Priorities { get; set; }
    }
}