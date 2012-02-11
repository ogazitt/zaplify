namespace BuiltSteady.Zaplify.Website.Resources
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using System.Net.Http;
    using System.Net;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    using BuiltSteady.Zaplify.Website.Helpers;
    using BuiltSteady.Zaplify.Website.Models;
    using BuiltSteady.Zaplify.ServerEntities;

    [ServiceContract]
    [LogMessages]
    public class ConstantsResource : BaseResource
    {
        public ConstantsResource()
        { }

        override protected StorageContext StorageContext
        {   // use static storage context, as constants do not change
            get
            {
                if (storageContext == null)
                {
                    storageContext = Storage.StaticContext;
                }
                return storageContext;
            }
        }

        [WebGet(UriTemplate="")]
        [LogMessages]
        public HttpResponseMessageWrapper<Constants> Get(HttpRequestMessage req)
        {
            // constant values are not protected, no authentication required

            try
            {
                var actionTypes = this.StorageContext.ActionTypes.OrderBy(a => a.SortOrder).ToList<ActionType>();
                var colors = this.StorageContext.Colors.OrderBy(c => c.ColorID).ToList<Color>();
                var fieldTypes = this.StorageContext.FieldTypes.OrderBy(ft => ft.FieldTypeID).ToList<FieldType>();
                var itemTypes = this.StorageContext.ItemTypes.Where(l => l.UserID == null).Include("Fields").ToList<ItemType>();  // get the built-in itemtypes
                var permissions = this.StorageContext.Permissions.OrderBy(p => p.PermissionID).ToList<Permission>();
                var priorities = this.StorageContext.Priorities.OrderBy(p => p.PriorityID).ToList<Priority>();
                var constants = new Constants() 
                { 
                    ActionTypes = actionTypes, 
                    Colors = colors, 
                    FieldTypes = fieldTypes, 
                    ItemTypes = itemTypes, 
                    Permissions = permissions, 
                    Priorities = priorities 
                };
                return new HttpResponseMessageWrapper<Constants>(req, constants, HttpStatusCode.OK);
            }
            catch (Exception)
            {
                // constants not found - return 404 Not Found
                return new HttpResponseMessageWrapper<Constants>(req, HttpStatusCode.NotFound);
            }
        }
    }
}