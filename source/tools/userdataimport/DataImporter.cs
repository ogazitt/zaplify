using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuiltSteady.Zaplify.ServiceHost;
using System.IO;
using BuiltSteady.Zaplify.ServerEntities;
using BuiltSteady.Zaplify.Shared.Entities;

namespace BuiltSteady.Zaplify.Tools.UserDataImport
{
    public class DataImporter
    {
        static Dictionary<Guid, Guid> NewID = new Dictionary<Guid, Guid>();

        public static bool Import(string connectionString, string filename, string username)
        {
            var context = new UserStorageContext(connectionString);
            var user = context.Users.
                Include("ItemTypes.Fields").
                Include("Tags").
                FirstOrDefault(u => u.Name == username);
            if (user == null)
            {
                Console.WriteLine(String.Format("Import: user {0} not found", username));
                return false;
            }

            // get the folders 
            List<Folder> folders = context.Folders.
                Include("FolderUsers").
                Include("Items.ItemTags").
                Include("Items.FieldValues").
                Where(f => f.UserID == user.ID && f.ItemTypeID != SystemItemTypes.System).
                ToList();

            var userDataModel = new UserDataModel(context, user);
            try
            {
                User jsonUser = null;
                filename = filename ?? @"userdata.json";

                // read the file and deserialize the data into a User class
                using (var stream = System.IO.File.Open(filename, FileMode.Open, FileAccess.Read))
                using (var reader = new StreamReader(stream))
                {
                    var json = reader.ReadToEnd();
                    jsonUser = JsonSerializer.Deserialize<User>(json);
                }

                // reorder the folders by item type ID - this is to get locations before contacts before tasks
                // this heuristic relies on that order so that a task's location and contact references will be live in the DB before the task is imported  
                // likewise, it needs the locations to be live in the DB before a contact (which may point to a location) is imported  
                var jsonFolders = new List<Folder>();
                foreach (var jsonFolder in jsonUser.Folders.Where(f => f.ItemTypeID == SystemItemTypes.Location))
                    jsonFolders.Add(jsonFolder);
                foreach (var jsonFolder in jsonUser.Folders.Where(f => f.ItemTypeID == SystemItemTypes.Contact))
                    jsonFolders.Add(jsonFolder);
                foreach (var jsonFolder in jsonUser.Folders.Where(f => f.ItemTypeID == SystemItemTypes.Grocery))
                    jsonFolders.Add(jsonFolder);
                foreach (var jsonFolder in jsonUser.Folders.Where(f => f.ItemTypeID == SystemItemTypes.ListItem))
                    jsonFolders.Add(jsonFolder);
                foreach (var jsonFolder in jsonUser.Folders.Where(f => f.ItemTypeID == SystemItemTypes.Task))
                    jsonFolders.Add(jsonFolder);
                foreach (var jsonFolder in jsonUser.Folders.Where(f => f.ItemTypeID == SystemItemTypes.Appointment))
                    jsonFolders.Add(jsonFolder);

                // process the serialized user folders
                foreach (var jsonFolder in jsonFolders)
                {
                    // skip the $PhoneClient folder (currently import does not support merging $PhoneClient)
                    // the difficulty is in merge semantics for individual items which have substructure (like PhoneSettings), 
                    // as well as ListMetadata for lists/folders that have the same names, but different ID's, from the lists in the DB
                    //   (e.g. Tasks, Groceries)
                    if (jsonFolder.Name == SystemEntities.PhoneClient ||
                        jsonFolder.Name == SystemEntities.Client || 
                        jsonFolder.Name == SystemEntities.WebClient)
                        continue;

                    // reset some of the fields in the serialized structure to the database values
                    jsonFolder.UserID = user.ID;

                    // find the folder by name
                    var folder = folders.FirstOrDefault(f => f.Name == jsonFolder.Name);
                    if (folder == null)
                    {
                        // folder not found - add it (including all its children) using a new ID (in case the old one still exists in the DB)
                        jsonFolder.ID = Guid.NewGuid();
                        folder = context.Folders.Add(jsonFolder);
                        context.SaveChanges();
                        Console.WriteLine("Added folder " + folder.Name);
                    }
                    else
                    {
                        // folder found - don't add it, but process all the children and add as appropriate to the found folder
                        Console.WriteLine("Found folder " + folder.Name);
                        var rootChildren = jsonFolder.Items.Where(i => i.ParentID == null).ToList();
                        ImportItems(context, folder, jsonFolder, rootChildren, null, 1);
                    }
                }
                context.SaveChanges();
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine("DataImporter: import failed; ex: ", ex.Message);
                return false;
            }
            return true;
        }

        static void ImportItems(UserStorageContext context, Folder folder, Folder jsonFolder, List<Item> jsonItems, Guid? parentID, int level)
        {
            // process each of the items passed in
            foreach (var jsonItem in jsonItems)
            {
                // reset some of the fields in the serialized structure to the database values
                jsonItem.UserID = folder.UserID;
                jsonItem.FolderID = folder.ID;

                // get the children of the current item from the serialized folder
                var jsonChildren = jsonFolder.Items.Where(i => i.ParentID == jsonItem.ID).ToList();
                if (jsonChildren.Count > 0 || jsonItem.IsList)
                {
                    // process the item as a list - find the database list by name
                    var list = folder.Items.FirstOrDefault(i => i.Name == jsonItem.Name && i.ParentID == parentID);
                    if (list != null)
                    {
                        // list found in database - change the ID of the serialized list to match the database
                        for (int i = 0; i < level; i++)
                            Console.Write("    ");
                        Console.Write(jsonItem.IsList ? "Found list: " : "Found item: ");
                        Console.WriteLine(jsonItem.Name);
                        jsonItem.ID = list.ID;
                    }
                    else
                    {
                        // list not found in database - add the list to the DB using a new ID (in case the old one still exists in the DB)
                        var id = Guid.NewGuid();
                        NewID[jsonItem.ID] = id;
                        jsonItem.ID = id;
                        list = context.Items.Add(jsonItem);
                        context.SaveChanges();

                        for (int i = 0; i < level; i++)
                            Console.Write("    ");
                        Console.Write(jsonItem.IsList ? "Added list: " : "Added item: ");
                        Console.WriteLine(jsonItem.Name);
                    }

                    // fix the parent ID's for all the children
                    foreach (var i in jsonChildren)
                        i.ParentID = jsonItem.ID;

                    // recursively import this list's children
                    ImportItems(context, folder, jsonFolder, jsonChildren, jsonItem.ID, level + 1);

                    // fix any fieldvalues that are pointing to old guids
                    foreach (var fv in jsonItem.FieldValues)
                    {
                        Guid guid;
                        if (Guid.TryParse(fv.Value, out guid))
                        {
                            Guid newID;
                            if (NewID.TryGetValue(new Guid(fv.Value), out newID))
                                fv.Value = newID.ToString();
                        }
                    }
                    context.SaveChanges();
                }
                else
                {
                    // this is a singleton (not a list) - add it to the DB using a new ID (in case the old one still exists in the DB)
                    var id = Guid.NewGuid();
                    NewID[jsonItem.ID] = id;

                    // if this is a reference, fix the ref ID
                    if (jsonItem.ItemTypeID == SystemItemTypes.Reference)
                    {
                        var refID = jsonItem.GetFieldValue(FieldNames.EntityRef);
                        if (refID != null && !String.IsNullOrEmpty(refID.Value))
                        {
                            Guid newID;
                            if (NewID.TryGetValue(new Guid(refID.Value), out newID))
                                refID.Value = newID.ToString();
                        }
                    }

                    jsonItem.ID = id;
                    context.Items.Add(jsonItem);
                    context.SaveChanges();
                    for (int i = 0; i < level; i++)
                        Console.Write("    ");
                    Console.WriteLine("Added item " + jsonItem.Name);
                }
            }
        }
    }
}
