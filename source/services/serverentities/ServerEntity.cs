using System;

namespace BuiltSteady.Zaplify.ServerEntities
{
    // base class for User, Folder, Item, etc
    public class ServerEntity
    {
        public virtual Guid ID { get; set; }
        public virtual string Name { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }

    // base class for all user-owned entities (Folder, Item, Tag, ItemType, etc)
    public class UserOwnedEntity : ServerEntity
    {
        public virtual Guid UserID { get; set; }
    }
}