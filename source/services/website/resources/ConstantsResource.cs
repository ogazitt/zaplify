using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel;
using System.ServiceModel.Web;
using Microsoft.ApplicationServer.Http;
using System.Net.Http;
using System.Net;
using System.Reflection;
using BuiltSteady.Zaplify.Website.Helpers;
using BuiltSteady.Zaplify.Website.Models;
using System.Data.Entity;
using BuiltSteady.Zaplify.ServerEntities;

namespace BuiltSteady.Zaplify.Website.Resources
{
    [ServiceContract]
    [LogMessages]
    public class ConstantsResource
    {
        private bool isDebugEnabled = false;

        public ConstantsResource()
        {
            // enable debug flag if this is a debug build
#if DEBUG
            isDebugEnabled = true;
#endif
        }

        private ZaplifyStore ZaplifyStore
        {
            get
            {
                // if in a debug build, always go to the database
                if (isDebugEnabled)
                    return new ZaplifyStore();
                else // retail build
                {
                    // use a cached context (to promote serving values out of EF cache) 
                    return ZaplifyStore.Current;
                }
            }
        }

        /// <summary>
        /// Get all constants
        /// </summary>
        /// <returns>All Constant information (FieldTypes, Priorities, etc)</returns>
        [WebGet(UriTemplate="")]
        [LogMessages]
        public HttpResponseMessageWrapper<Constants> Get(HttpRequestMessage req)
        {
            // no need to authenticate
            //HttpStatusCode code = ResourceHelper.AuthenticateUser(req, ZaplifyStore);
            //if (code != HttpStatusCode.OK)
            //    return new HttpResponseMessageWrapper<Constants>(req, code);  // user not authenticated

            ZaplifyStore zaplifystore = ZaplifyStore;

            try
            {
                var actionTypes = zaplifystore.ActionTypes.OrderBy(a => a.SortOrder).ToList<ActionType>();
                var colors = zaplifystore.Colors.OrderBy(c => c.ColorID).ToList<Color>();
                var fieldTypes = zaplifystore.FieldTypes.OrderBy(ft => ft.FieldTypeID).ToList<FieldType>();
                var itemTypes = zaplifystore.ItemTypes.Where(l => l.UserID == null).Include("Fields").ToList<ItemType>();  // get the built-in itemtypes
                var permissions = zaplifystore.Permissions.OrderBy(p => p.PermissionID).ToList<Permission>();
                var priorities = zaplifystore.Priorities.OrderBy(p => p.PriorityID).ToList<Priority>();
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