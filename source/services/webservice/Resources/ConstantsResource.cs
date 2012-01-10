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
using TaskStoreWeb.Helpers;
using TaskStoreWeb.Models;
using System.Data.Entity;
using TaskStoreServerEntities;

namespace TaskStoreWeb.Resources
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

        private TaskStore TaskStore
        {
            get
            {
                // if in a debug build, always go to the database
                if (isDebugEnabled)
                    return new TaskStore();
                else // retail build
                {
                    // use a cached context (to promote serving values out of EF cache) 
                    return TaskStore.Current;
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
            //HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            //if (code != HttpStatusCode.OK)
            //    return new HttpResponseMessageWrapper<Constants>(req, code);  // user not authenticated

            TaskStore taskstore = TaskStore;

            try
            {
                var actions = taskstore.Actions.OrderBy(a => a.SortOrder).ToList<TaskStoreServerEntities.Action>();
                var colors = taskstore.Colors.OrderBy(c => c.ColorID).ToList<Color>();
                var fieldTypes = taskstore.FieldTypes.OrderBy(ft => ft.FieldTypeID).ToList<FieldType>();
                var listTypes = taskstore.ListTypes.Where(l => l.UserID == null).Include("Fields").ToList<ListType>();  // get the built-in listtypes
                var priorities = taskstore.Priorities.OrderBy(p => p.PriorityID).ToList<Priority>();
                var constants = new Constants() { Actions = actions, Colors = colors, FieldTypes = fieldTypes, ListTypes = listTypes, Priorities = priorities };
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