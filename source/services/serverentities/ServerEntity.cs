using System;

namespace BuiltSteady.Zaplify.ServerEntities
{
    public class ServerEntity
    {
        public virtual Guid ID { get; set; }
        public virtual string Name { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }
}