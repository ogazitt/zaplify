namespace BuiltSteady.Zaplify.Website.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Mvc;

    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.Shared.Entities;
    using System.Net;

    public class UserInfoController : BaseController
    {
        class JsContactsResult
        {
            public HttpStatusCode StatusCode = HttpStatusCode.OK;
            public int Count = -1;
            public Dictionary<string, string> Contacts = null;
        }

        public ActionResult PossibleContacts(string startsWith = null, string contains = null, int maxCount = 10)
        {
            JsContactsResult contactsResults = new JsContactsResult();
            Folder userFolder = StorageContext.GetOrCreateUserFolder(CurrentUser);
            Item possibleContactsList = StorageContext.GetOrCreateUserItemTypeList(CurrentUser, SystemItemTypes.Contact);
            if (possibleContactsList != null)
            {
                Dictionary<string, string> contacts = new Dictionary<string, string>();
                List<Item> possibleContacts = StorageContext.Items.
                    Include("FieldValues").
                    Where(item => item.UserID == CurrentUser.ID
                        && item.FolderID == userFolder.ID
                        && item.ParentID == possibleContactsList.ID
                        && item.Name.StartsWith(startsWith)       
                        // && System.Data.Objects.SqlClient.SqlFunctions.PatIndex("%" + contains + "%", item.Name) == 1     // TODO: test if this works
                    ).ToList<Item>();

                foreach (var item in possibleContacts)
                {
                    if (contains == null || item.Name.Contains(contains))
                    {
                        FieldValue fv = item.GetFieldValue(FieldNames.Value);
                        if (fv != null)
                        {
                            if (contacts.ContainsKey(item.Name))
                            {   // disambiguate duplicate names
                                item.Name = string.Format("{0} ({1})", item.Name, contacts.Count.ToString());
                            }
                            contacts.Add(item.Name, fv.Value);
                        }
                        if (contacts.Count == maxCount) { break; }
                    }
                }
                contactsResults.Count = contacts.Count;
                contactsResults.Contacts = contacts;
            }

            JsonResult result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            result.Data = contactsResults;
            return result;
        }

    }
}
