using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class ServerEntity
    {
        public virtual Guid ID { get; set; }
        public virtual string Name { get; set; }
    }
}