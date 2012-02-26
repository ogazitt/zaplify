using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using System.ComponentModel;

namespace BuiltSteady.Zaplify.Devices.WinPhone
{
    public partial class DebugPage : PhoneApplicationPage
    {
        public DebugPage()
        {
            InitializeComponent();

            // trace event
            TraceHelper.AddMessage("Debug: constructor");

            // Set the data context of the page to the main view model
            DataContext = App.ViewModel;

            Loaded += new RoutedEventHandler(DebugPage_Loaded);
            BackKeyPress += new EventHandler<CancelEventArgs>(DebugPage_BackKeyPress);
        }

        void DebugPage_Loaded(object sender, RoutedEventArgs e)
        {
            // trace event
            TraceHelper.AddMessage("Debug: Loaded");

            RenderDebugPanel();

            // trace event
            TraceHelper.AddMessage("Exiting Debug Loaded");
        }

        void DebugPage_BackKeyPress(object sender, CancelEventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("Debug: Navigate back");

            // navigate back
            NavigationService.GoBack();
        }

        // Event handlers for Debug page
        #region Event Handlers

        private void Debug_AddButton_Click(object sender, EventArgs e)
        {
            Item item;
            Item todoList, shoppingList;

            // create some debug records

            // create a to-do style item
            Folder folder = App.ViewModel.Folders.Single(f => f.Name == "Personal");
            todoList = App.ViewModel.Items.Single(i => i.ItemTypeID == ItemType.Task && i.IsList == true);
            shoppingList = App.ViewModel.Items.Single(i => i.ItemTypeID == ItemType.ListItem && i.IsList == true);
            folder.Items.Add(item = new Item() { FolderID = folder.ID, ParentID = todoList.ID, ItemTypeID = ItemType.Task, Name = "Check out Zaplify", Due = DateTime.Today, PriorityID = 1 });

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                    Body = item,
                    ID = item.ID
                });

            // create a shopping item
            string[] names = { "Milk", "OJ", "Cereal", "Coffee", "Bread" };
            foreach (var name in names)
            {
                folder.Items.Add(item = new Item() { FolderID = folder.ID, ParentID = shoppingList.ID, ItemTypeID = ItemType.ListItem, Name = name });

                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                        Body = item,
                        ID = item.ID
                    });
            }

            // re-render the debug tab
            RenderDebugPanel();
        }

        private void Debug_ClearButton_Click(object sender, EventArgs e)
        {
            // clear the trace log
            TraceHelper.ClearMessages();

            // re-render the debug tab
            RenderDebugPanel();
        }

        // Event handlers for Debug tab
        private void Debug_DeleteButton_Click(object sender, EventArgs e)
        {
            // clear the record queue
            RequestQueue.RequestRecord record = RequestQueue.DequeueRequestRecord();
            while (record != null)
            {
                record = RequestQueue.DequeueRequestRecord();
            }

            // re-render the debug tab
            RenderDebugPanel();
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            App.ViewModel.SyncWithService();

            // re-render the debug 
            RenderDebugPanel();
        }

        private void SendInfoMenuItem_Click(object sender, EventArgs e)
        {
            // Send the messages to the service
            TraceHelper.SendMessages(App.ViewModel.User);
        }

        #endregion

        #region Helpers

        private void RenderDebugPanel()
        {
            // remove all children and then add some debug spew
            DebugPanel.Children.Clear();
            DebugPanel.Children.Add(new TextBlock() { Text = "Connection Status and Server URL:" });
            DebugPanel.Children.Add(new TextBlock() { Text = String.Format("Connected: {0}", App.ViewModel.LastNetworkOperationStatus) });
            TextBox textBox = new TextBox() { Text = WebServiceHelper.BaseUrl };
            DebugPanel.Children.Add(textBox);
            Button button = new Button() { Content = "Store New Service URL" };
            button.Click += (s, e) => { WebServiceHelper.BaseUrl = textBox.Text; };
            DebugPanel.Children.Add(button);

            // render request queue
            DebugPanel.Children.Add(new TextBlock() { Text = "Request Queue:" });
            List<RequestQueue.RequestRecord> requests = RequestQueue.GetAllRequestRecords();
            if (requests != null)
            {
                foreach (var req in requests)
                {
                    string typename;
                    string reqtype;
                    string id;
                    string name;
                    RetrieveRequestInfo(req, out typename, out reqtype, out id, out name);
                    DebugPanel.Children.Add(new TextBlock() { Text = String.Format("  {0} {1} {2} (id {3})", reqtype, typename, name, id) });
                }
            }

            // render trace messages
            DebugPanel.Children.Add(new TextBlock() { Text = "Trace Messages:" });
            string trace = TraceHelper.GetMessages();
            DebugPanel.Children.Add(new TextBlock() { Text = trace });
        }

        private static void RetrieveRequestInfo(RequestQueue.RequestRecord req, out string typename, out string reqtype, out string id, out string name)
        {
            typename = req.BodyTypeName;
            reqtype = "";
            id = "";
            name = "";
            switch (req.ReqType)
            {
                case RequestQueue.RequestRecord.RequestType.Delete:
                    reqtype = "Delete";
                    id = ((ZaplifyEntity)req.Body).ID.ToString();
                    name = ((ZaplifyEntity)req.Body).Name;
                    break;
                case RequestQueue.RequestRecord.RequestType.Insert:
                    reqtype = "Insert";
                    id = ((ZaplifyEntity)req.Body).ID.ToString();
                    name = ((ZaplifyEntity)req.Body).Name;
                    break;
                case RequestQueue.RequestRecord.RequestType.Update:
                    reqtype = "Update";
                    switch (req.BodyTypeName)
                    {
                        case "Tag":
                            name = ((List<Tag>)req.Body)[0].Name;
                            id = ((List<Tag>)req.Body)[0].ID.ToString();
                            break;
                        case "Item":
                            name = ((List<Item>)req.Body)[0].Name;
                            id = ((List<Item>)req.Body)[0].ID.ToString();
                            break;
                        case "Folder":
                            name = ((List<Folder>)req.Body)[0].Name;
                            id = ((List<Folder>)req.Body)[0].ID.ToString();
                            break;
                        default:
                            name = "(unrecognized entity)";
                            break;
                    }
                    break;
                default:
                    reqtype = "Unrecognized";
                    break;
            }
        }

        #endregion
    }
}