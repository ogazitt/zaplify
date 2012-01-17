﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;
using AE.Net.Mail;
using AE.Net.Mail.Imap;
using BuiltSteady.Zaplify.ServerEntities;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Text;
using System.Configuration;
using BuiltSteady.Zaplify.ServiceHelpers;

namespace BuiltSteady.Zaplify.MailWorker
{
    public class WorkerRole : RoleEntryPoint
    {
        static string FolderMarker = @"#folder:";
        static string ListMarker = @"#list:";

        static ZaplifyStore ZaplifyStore
        {
            get
            {
                // use a cached context (to promote serving values out of EF cache) 
                return ZaplifyStore.Current;
            }
        }

        static Guid toDoItemType;
        static Guid ToDoItemType
        {
            get
            {
                if (toDoItemType == Guid.Empty)
                    toDoItemType = ZaplifyStore.ItemTypes.Single(lt => lt.Name == "To Do" && lt.UserID == null).ID;
                return toDoItemType;
            }
        }

        public override bool OnStart()
        {
            // Log function entrance
            LoggingHelper.TraceFunction();

            // Set the maximum number of concurrent connections 
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            return base.OnStart();
        }

        public override void Run()
        {
            TraceLine("BuiltSteady.Zaplify.MailWorker started", "Information");
            string hostname = ConfigurationManager.AppSettings["Hostname"];
            int port = Int32.Parse(ConfigurationManager.AppSettings["Port"]);
            string username = ConfigurationManager.AppSettings["Username"];
            string password = ConfigurationManager.AppSettings["Password"];

#if IDLE
            // idle support means we can use an event-based programming model to get informed when 
            var mre = new System.Threading.ManualResetEvent(false);
            using (var imap = new ImapClient(hostname, username, password, ImapClient.AuthMethods.Login, port, true))
                TraceLine("Connected", "Information");
                imap.SelectMailbox("inbox");

                // a new message comes in
                imap.NewMessage += Imap_NewMessage;
                while (!mre.WaitOne(5000)) //low for the sake of testing; typical timeout is 30 minutes
                    imap.Noop();
            }
#else
            // no idle support means we need to poll the mailbox
            while (true)
            {
                try
                {
                    using (var imap = new ImapClient(hostname, username, password, ImapClient.AuthMethods.Login, port, true))
                    {
                        imap.SelectMailbox("inbox");

                        int count = imap.GetMessageCount();
                        if (count > 0)
                        {
                            MailMessage[] messages = imap.GetMessages(0, count, false);
                            foreach (var m in messages)
                            {
                                TraceLine("BuiltSteady.Zaplify.MailWorker processing message " + m.Subject, "Information");
                                ProcessMessage(m);
                                imap.MoveMessage(m.Uid, "processed");
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    TraceLine("Can't contact mail server", "Error");
                }

                // sleep 30 seconds
                Thread.Sleep(30000);
            }
#endif
        }

        #region Helpers

        static Guid? GetFolder(User u, string body, bool html)
        {
            Folder folder = null;

            // a hash indicates a list name to add the new item to
            int index = body.IndexOf(FolderMarker);
            if (index >= 0)
            {
                string folderName = body.Substring(index + FolderMarker.Length);
                int folderNameEnd = folderName.IndexOf('\n');
                if (html == true)
                    folderNameEnd = folderName.IndexOf("</div>");
                if (folderNameEnd > 0)
                {
                    folderName = folderName.Substring(0, folderNameEnd);
                    folderName = folderName.Trim();
                    folder = ZaplifyStore.Folders.FirstOrDefault(f => f.UserID == u.ID && f.Name == folderName);
                    if (folder != null)
                        return folder.ID;
                }
            }

            folder = ZaplifyStore.Folders.FirstOrDefault(f => f.UserID == u.ID && f.Name == "Personal");
            if (folder != null)
                return folder.ID;
            else
                return null;
        }

        static Guid? GetList(User u, string body, bool html)
        {
            Item list = null;

            // a hash indicates a list name to add the new item to
            int index = body.IndexOf(ListMarker);
            if (index >= 0)
            {
                string listName = body.Substring(index + ListMarker.Length);
                int listNameEnd = listName.IndexOf('\n');
                if (html == true)
                    listNameEnd = listName.IndexOf("</div>");
                if (listNameEnd > 0)
                {
                    listName = listName.Substring(0, listNameEnd);
                    listName = listName.Trim();
                    list = ZaplifyStore.Items.FirstOrDefault(i => i.UserID == u.ID && i.Name == listName);
                    if (list != null)
                        return list.ID;
                }
            }

            list = ZaplifyStore.Items.FirstOrDefault(i => i.UserID == u.ID && i.IsList == true && i.ItemTypeID == ToDoItemType);
            if (list != null)
                return list.ID;
            else
                return null;
        }

        static string GetSubject(string subject)
        {
            bool found = true;
            string processedSubject = subject.Trim();

            while (found == true)
            {
                found = false;

                string[] stripArray = { "RE:", "Re:", "re:", "FW:", "Fwd:", "Fw:" };
                foreach (var str in stripArray)
                {
                    if (processedSubject.StartsWith(str, true, null))
                    {
                        processedSubject = processedSubject.Substring(str.Length);
                        processedSubject = processedSubject.Trim();
                        found = true;
                        break;
                    }
                }
            }
            return processedSubject;
        }

        static bool IsAlphaNum(char c)
        {
            if (c >= 'A' && c <= 'Z' ||
                c >= 'a' && c <= 'z' ||
                c >= '0' && c <= '9')
                return true;
            else
                return false;
        }

        static void Imap_NewMessage(object sender, MessageEventArgs e)
        {
            var imap = (sender as ImapClient);
            var msg = imap.GetMessage(e.MessageCount - 1);
            TraceLine(String.Format("Retrieved message {0}", msg.Subject), "Information");
        }

        static void ParseFields(Item item, string body)
        {
            string text = body;
            if (text == null || text == "")
                return;

            Match m;

            // parse the text for a phone number
            m = Regex.Match(text, @"(?:(?:\+?1\s*(?:[.-]\s*)?)?(?:\(\s*([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9])\s*\)|([2-9]1[02-9]|[2-9][02-8]1|[2-9][02-8][02-9]))\s*(?:[.-]\s*)?)?([2-9]1[02-9]|[2-9][02-9]1|[2-9][02-9]{2})\s*(?:[.-]\s*)?([0-9]{4})(?:\s*(?:#|x\.?|ext\.?|extension)\s*(\d+))?", RegexOptions.IgnoreCase);
            if (m != null && m.Value != null && m.Value != "")
                item.Phone = m.Value;

            // parse the text for an email address
            m = Regex.Match(text, @"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+(?:[A-Z]{2}|com|org|net|edu|gov|mil|biz|info|mobi|name|aero|asia|jobs|museum)\b", RegexOptions.IgnoreCase);
            if (m != null && m.Value != null && m.Value != "")
                item.Email = m.Value;

            // parse the text for a website
            m = Regex.Match(text, @"((http|https)(:\/\/))?([a-zA-Z0-9]+[.]{1}){2}[a-zA-z0-9]+(\/{1}[a-zA-Z0-9]+)*\/?", RegexOptions.IgnoreCase);
            if (m != null && m.Value != null && m.Value != "")
                item.Website = m.Value;

            // parse the text for a date
            m = Regex.Match(text, @"(0?[1-9]|1[012])([- /.])(0?[1-9]|[12][0-9]|3[01])\2(20|19)?\d\d", RegexOptions.IgnoreCase);
            if (m != null && m.Value != null && m.Value != "")
            {
                // convert to datetime, then back to string.  this is to canonicalize all dates into yyyy/MM/dd.
                item.DueDate = ((DateTime) Convert.ToDateTime(m.Value)).ToString("yyyy/MM/dd");
            }
        }

        static string PrintItem(Item item)
        {
            StringBuilder sb = new StringBuilder();
            bool comma = false;
            try
            {
                ItemType itemType = ZaplifyStore.ItemTypes.Include("Fields").Single(it => it.ID == item.ItemTypeID);

                foreach (Field f in itemType.Fields.OrderBy(f => f.SortOrder))
                {
                    FieldType fieldType;
                    // get the field type for this field
                    try
                    {
                        fieldType = ZaplifyStore.FieldTypes.Single(ft => ft.FieldTypeID == f.FieldTypeID);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    // already printed out the item name
                    if (fieldType.DisplayName == "Name")
                        continue;

                    PropertyInfo pi;
                    // make sure the property exists on the local type
                    try
                    {
                        pi = item.GetType().GetProperty(fieldType.Name);
                        if (pi == null)
                            continue;  // see comment below
                    }
                    catch (Exception)
                    {
                        // we can't do anything with this property since we don't have it on the local type
                        // this indicates that the phone software isn't caught up with the service version
                        // but that's ok - we can keep going
                        continue;
                    }

                    // skip the uninteresting fields
                    if (pi.CanWrite == false ||
                        pi.PropertyType == typeof(Guid) ||
                        pi.PropertyType == typeof(Guid?) ||
                        pi.Name == "ItemTags" ||
                        pi.Name == "Created" ||
                        pi.Name == "LastModified")
                        continue;

                    // get the value of the property
                    var val = pi.GetValue(item, null);
                    if (val != null)
                    {
                        if (comma)
                            sb.Append(",");
                        else
                            comma = true;
                        sb.AppendFormat("{0}: {1}", pi.Name, val.ToString());
                    }
                }
                
                // return the string
                return sb.ToString();
            }
            catch (Exception ex)
            {
                TraceLine("Exception while Printing Item: " + ex.Message, "Error");
                return "no fields parsed";
            }
        }

        static void ProcessMessage(MailMessage m)
        {
            bool html = false;

            string from = m.From.Address;
            if (from == null || from == "")
                return;

            string itemName = GetSubject(m.Subject);
            string body = m.Body;
            if (body == null || body == "")
            {
                body = m.BodyHtml;
                html = true;
            }
            
            var users = ZaplifyStore.Users.Where(u => u.Email == from).ToList();
            foreach (var u in users)
            {
                Guid? folder = GetFolder(u, body, html);
                Guid? list = GetList(u, body, html);
                if (folder != null && list != null)
                {
                    DateTime now = DateTime.Now;
                    Item t = new Item()
                    {
                        ID = Guid.NewGuid(),
                        FolderID = (Guid)folder,
                        ItemTypeID = ToDoItemType,
                        Name = itemName,
                        ParentID = (Guid)list,
                        Created = now,
                        LastModified = now,
                    };
                    
                    // extract structured fields such as due date, e-mail, website, phone number
                    ParseFields(t, body);

                    var item = ZaplifyStore.Items.Add(t);
                    int rows = ZaplifyStore.SaveChanges();

                    if (rows > 0)
                        TraceLine(String.Format("Added Item: {0} ({1})", t.Name, PrintItem(t)), "Information");
                }
            }
        }

        static void TraceLine(string message, string level)
        {
            Trace.WriteLine(
                String.Format(
                    "{0}: {1}", 
                    DateTime.Now.ToString(), 
                    message), 
                level);
            Trace.Flush();
        }

        #endregion
    }
}
