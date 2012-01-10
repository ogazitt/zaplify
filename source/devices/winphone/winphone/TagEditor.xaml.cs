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
using TaskStoreClientEntities;
using Microsoft.Phone.Shell;
using TaskStoreWinPhoneUtilities;
using System.ComponentModel;

namespace TaskStoreWinPhone
{
    public partial class TagEditor : PhoneApplicationPage
    {
        private Tag tag;
        private Tag tagCopy;

        public TagEditor()
        {
            InitializeComponent();

            // trace event
            TraceHelper.AddMessage("TagEditor: constructor");

            // enable tabbing
            this.IsTabStop = true;

            this.Loaded += new RoutedEventHandler(TagEditor_Loaded);
            this.BackKeyPress += new EventHandler<CancelEventArgs>(TagEditor_BackKeyPress);
        }

        #region Event Handlers

        void TagEditor_BackKeyPress(object sender, CancelEventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("TagEditor: Navigate back");

            // navigate back
            NavigationService.GoBack();
        }

        void TagEditor_Loaded(object sender, RoutedEventArgs e)
        {
            // trace event
            TraceHelper.AddMessage("TagEditor: Loaded");

            ConnectedIconImage.DataContext = App.ViewModel;

            string tagIDString = "";

            if (NavigationContext.QueryString.TryGetValue("ID", out tagIDString))
            {
                if (tagIDString == "new")
                {
                    // new tag
                    tagCopy = new Tag();
                    DataContext = tagCopy;
                }
                else
                {
                    Guid tagID = new Guid(tagIDString);
                    tag = App.ViewModel.Tags.Single<Tag>(tl => tl.ID == tagID);

                    // make a deep copy of the tag for local binding
                    tagCopy = new Tag(tag);
                    DataContext = tagCopy;

                    // add the delete button to the ApplicationBar
                    var button = new ApplicationBarIconButton() { Text = "Delete", IconUri = new Uri("/Images/appbar.delete.rest.png", UriKind.Relative) };
                    button.Click += new EventHandler(DeleteButton_Click);
                    
                    // insert after the save button but before the cancel button
                    ApplicationBar.Buttons.Add(button);
                }
            }

            ColorListPicker.ItemsSource = App.ViewModel.Constants.Colors;
            ColorListPicker.DisplayMemberPath = "Name";
            try
            {
                TaskStoreClientEntities.Color color = App.ViewModel.Constants.Colors.Single(c => c.Name == tagCopy.Color);
                ColorListPicker.SelectedIndex = App.ViewModel.Constants.Colors.IndexOf(color);
            }
            catch (Exception)
            {
            }
            //ColorListPicker.ItemCountThreshold = App.ViewModel.Constants.Colors.Count;
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            // trace page navigation
            TraceHelper.StartMessage("TagEditor: Navigate back");

            // navigate back
            NavigationService.GoBack();
        }
        
        private void DeleteButton_Click(object sender, EventArgs e)
        {
            // if this is a new tag, delete just does the same thing as cancel
            if (tag == null)
            {
                CancelButton_Click(sender, e);
                return;
            }

            MessageBoxResult result = MessageBox.Show("delete this tag?", "confirm delete", MessageBoxButton.OKCancel);
            if (result != MessageBoxResult.OK)
                return;

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Delete,
                    Body = tag
                });

            // remove the tag
            App.ViewModel.Tags.Remove(tag);

            // save the changes to local storage
            StorageHelper.WriteTags(App.ViewModel.Tags);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // trace page navigation
            TraceHelper.StartMessage("TagEditor: Navigate back");

            // Navigate back to the main page
            NavigationService.GoBack();
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // get the name of the tag
            tagCopy.Name = TagName.Text;

            // get the color from the listpicker
            tagCopy.Color = App.ViewModel.Constants.Colors[ColorListPicker.SelectedIndex].Name;

            // check for appropriate values
            if (tagCopy.Name == "")
            {
                MessageBox.Show("tag name cannot be empty");
                return;
            }

            // if this is a new tag, create it
            if (tag == null)
            {
                // enqueue the Web Request Record (with a new copy of the tag)
                // need to create a copy because otherwise other tasks may be added to it
                // and we want the record to have exactly one operation in it (create the tag)
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                        Body = new Tag(tagCopy)
                    });

                // add the tag to the tag list
                App.ViewModel.Tags.Add(tagCopy);
            }
            else // this is an update
            {
                // enqueue the Web Request Record
                RequestQueue.EnqueueRequestRecord(
                    new RequestQueue.RequestRecord()
                    {
                        ReqType = RequestQueue.RequestRecord.RequestType.Update,
                        Body = new List<Tag>() { tag, tagCopy },
                        BodyTypeName = "Tag",
                        ID = tagCopy.ID
                    });

                // save the changes to the existing tag
                tag.Copy(tagCopy);
            }

            // save the changes to local storage
            StorageHelper.WriteTags(App.ViewModel.Tags);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();

            // trace page navigation
            TraceHelper.StartMessage("TagEditor: Navigate back");

            // Navigate back to the main page
            NavigationService.GoBack();
        }

        #endregion
    }
}