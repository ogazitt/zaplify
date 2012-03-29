using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Devices.ClientViewModels;
using BuiltSteady.Zaplify.Devices.IPhone.Controls;
using BuiltSteady.Zaplify.Shared.Entities;
using MonoTouch.Dialog;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
    public class NuanceServiceInfo
    {
        public static bool SpeechKitSsl = false;
        public static string SpeechKitServer = "sandbox.nmdp.nuancemobility.net";
        public static int SpeechKitPort = 443;
        public static readonly string SpeechKitAppId = "NMDPTRIAL_ogazitt20120220010133";
    }
    
	public class AddPage : UINavigationController
	{
        private DialogViewController dialogViewController;
		private RootElement ListsRootElement;
        private ButtonListElement[] AddButtons = new ButtonListElement[3];
		private List<Item> lists;
        private List<Button> buttonList;
        private Section listsSection = null;
        public MultilineEntryElement Name;
        
        private SpeechPopupDelegate SpeechPopupDelegate = new SpeechPopupDelegate();
        public UIActionSheet SpeechPopup;
        public UILabel SpeechTextLabel = new UILabel();
        public UIActivityIndicatorView ActivityIndicator = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.WhiteLarge) { Hidden = true };
		
        // number of lists to generate "Add" buttons for
        const int MaxLists = 4;
        
		public AddPage()
		{
			// trace event
            TraceHelper.AddMessage("Add: constructor");

			this.Title = NSBundle.MainBundle.LocalizedString ("Add", "Add");
			this.TabBarItem.Image = UIImage.FromBundle ("Images/180-stickynote.png");
		}
		
		public override void ViewDidAppear(bool animated)
		{
			// trace event
            TraceHelper.AddMessage("Add: ViewDidAppear");
			
            if (dialogViewController == null)
                InitializeComponent();

            // initialize controls 
            Name.Value = "";
            
            // populate the lists section dynamically
            listsSection.Clear();
            CreateAddButtons();
            listsSection.AddAll(AddButtons);

            base.ViewDidAppear(animated);
		}
		
        public override void ViewDidDisappear(bool animated)
        {
            // trace event
            TraceHelper.AddMessage("Add: ViewDidDisappear");

            dialogViewController.ReloadComplete();
            App.ViewModel.SyncComplete -= RefreshHandler;
            App.ViewModel.SyncCompleteArg = null;
            NuanceHelper.Cleanup();
            base.ViewDidDisappear(animated);
        }
        
        #region Event Handlers
        
        private void AddButton_Click(object sender, EventArgs e)
        {
            // determine which button was clicked
            Button clickedButton = sender as Button ?? AddButtons[0][0];
            int listIndex = buttonList.IndexOf(clickedButton);
            
            Item list = lists[listIndex];
            Folder folder = App.ViewModel.Folders.Single(f => f.ID == list.FolderID);
   
            AddItem(folder, list);
        }     

        private void RefreshHandler(object sender, MainViewModel.SyncCompleteEventArgs e)
        {
            this.BeginInvokeOnMainThread(() =>
            {    
                dialogViewController.ReloadComplete();
            });
        }
        
        // handle events associated with the Speech Popup
        private void SpeechButton_Click(object sender, EventArgs e)
        {
            // require a connection
//            if (DeviceNetworkInformation.IsNetworkAvailable == false ||
//                NetworkInterface.GetIsNetworkAvailable() == false)
//            {
//                MessageBox.Show("apologies - a network connection is required for this feature, and you appear to be disconnected :-(");
//                return;
//            }
            
            // require an account
            if (App.ViewModel.User == null)
            {
                MessageBoxResult result = MessageBox.Show(
                    "the speech feature requires an account.  create a free account now?",
                    "create account?",
                    MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.Cancel)
                    return;

                // trace page navigation
                TraceHelper.StartMessage("Add: Navigate to Settings");

                // Navigate to the settings page
                this.TabBarController.PresentViewController(this.TabBarController.ViewControllers[3], true, null);
                return;
            }
   
            // initialize the speech popup delegate
            SpeechPopupDelegate.parent = this;
            SpeechPopupDelegate.speechState = NuanceHelper.SpeechState.Initializing;

            // store debug / timing info
            SpeechPopupDelegate.speechStart = DateTime.Now;
            SpeechPopupDelegate.speechDebugString = "";

            // store debug / timing info
            TimeSpan ts = DateTime.Now - SpeechPopupDelegate.speechStart;
            string stateString = NuanceHelper.SpeechStateString(SpeechPopupDelegate.speechState);
            string traceString = String.Format("New state: {0}; Time: {1}; Message: {2}", stateString, ts.TotalSeconds, "Connecting Socket");
            TraceHelper.AddMessage(traceString);
            SpeechPopupDelegate.speechDebugString += traceString + "\n";
   
            // cancel any existing speech operation
            NuanceHelper.Cleanup();
            
            // initialize the connection to the speech service
            NuanceHelper.Start(
                App.ViewModel.User,
                new NuanceHelper.SpeechToTextCallbackDelegate(SpeechPopupDelegate.SpeechPopup_SpeechToTextCallback),
                new NuanceHelper.SpeechStateCallbackDelegate(SpeechPopupDelegate.SpeechPopup_SpeechStateCallback),
                new MainViewModel.NetworkOperationInProgressCallbackDelegate(SpeechPopupDelegate.SpeechPopup_NetworkOperationInProgressCallBack));

            // open the popup
            SetupSpeechPopup("initializing...");
            SpeechPopup.ShowFromTabBar(((UITabBarController)this.ParentViewController).TabBar);
        }
        
        #endregion
        
        #region Helpers
        
        private void AddItem(Folder folder, Item list)
        {
            string name = Name.Value;

                // don't add empty items - instead, navigate to the list
            if (name == null || name == "")
            {
                UITableViewController nextController = new ListViewController(this, folder, list != null ? list.ID : Guid.Empty);  
                TraceHelper.StartMessage("AddPage: Navigate to List");
                this.PushViewController(nextController, true);
                return;
            }
   
            Guid itemTypeID;
            Guid parentID;
            if (list == null)
            {
                itemTypeID = folder.ItemTypeID;
                parentID = Guid.Empty;
            }
            else
            {
                itemTypeID = list.ItemTypeID;
                parentID = list.ID;
            }
            
            // get a reference to the item type
            ItemType itemType = App.ViewModel.ItemTypes.Single(it => it.ID == itemTypeID);
            
            // create the new item
            Item item = new Item()
            {
                Name = name,
                FolderID = folder.ID,
                ItemTypeID = itemTypeID,
                ParentID = parentID,
            };

            // hack: special case processing for item types that have a Complete field
            // if it exists, set it to false
            if (itemType.HasField("Complete"))
                item.Complete = false;

            // enqueue the Web Request Record
            RequestQueue.EnqueueRequestRecord(
                new RequestQueue.RequestRecord()
                {
                    ReqType = RequestQueue.RequestRecord.RequestType.Insert,
                    Body = item
                });

            // add the item to the folder
            folder.Items.Add(item);

            // save the changes to local storage
            StorageHelper.WriteFolder(folder);

            // trigger a sync with the Service 
            App.ViewModel.SyncWithService();             

            // reset the name field to make it easy to add the next one
            Name.Value = "";
        }
        
        private void AddItemToFolder(string folderName)
        {
            Folder folder = App.ViewModel.Folders.Single(f => f.Name == folderName);
            AddItem(folder, null);
        }
 
        public void SetupSpeechPopup(string text)
        {
            SpeechPopup = new UIActionSheet(" ", SpeechPopupDelegate, "Cancel", "Done");
            
            // center and add the activity indicator             
            ActivityIndicator.Center = new System.Drawing.PointF(this.View.Center.X, ActivityIndicator.Center.Y);
            SpeechPopup.AddSubview(ActivityIndicator);

            // make a label with a bigger font and add it as a subview to overlay the current label
            //RectangleF oldFrame = SpeechPopup.Subviews[0].Frame;
            SpeechTextLabel.Font = UIFont.BoldSystemFontOfSize(17);
            SpeechTextLabel.TextAlignment = UITextAlignment.Center;
            SpeechTextLabel.BackgroundColor = UIColor.Clear;
            SpeechTextLabel.TextColor = UIColor.White;
            SpeechTextLabel.Text = text;
            SpeechTextLabel.SizeToFit();
            //SpeechTextLabel.Frame.Width += 20;  // fudge factor to take care of differences between "listening" and "recognizing"
            SpeechTextLabel.Center = new PointF(this.View.Center.X, ActivityIndicator.Center.Y);
            SpeechPopup.AddSubview(SpeechTextLabel);

            // start the animation
            ActivityIndicator.StartAnimating();
        }
        
        #endregion
        
        #region Helpers

        private void InitializeComponent()
        {
            // initialize controls 
            var pushToTalkButton = new ButtonListElement() 
            { 
                new Button() 
                { 
                    Background = "Images/redbutton.png", 
                    Caption = "Touch to speak", 
                    Clicked = SpeechButton_Click
                }, 
            };
            pushToTalkButton.Margin = 0f;
            
            var addButton = new ButtonListElement() 
            { 
                new Button() 
                { 
                    Background = "Images/darkgreybutton.png", 
                    Caption = "Add", 
                    Clicked = AddButton_Click 
                }, 
            };
            addButton.Margin = 0f;
            
            Name = new MultilineEntryElement("Name", "") { Lines = 3 };
        
            listsSection = new Section("Add to list:");
                
            // create the dialog
            var root = new RootElement("Add Item")
            {
                new Section()
                {
                    Name,
                },
                listsSection,
                new Section()
                {
                    pushToTalkButton
                },
            };
         
            // create and push the dialog view onto the nav stack
            dialogViewController = new DialogViewController(root);
            dialogViewController.NavigationItem.HidesBackButton = true;  
            dialogViewController.Title = NSBundle.MainBundle.LocalizedString("Add", "Add");

            // set up the "pull to refresh" feature
            App.ViewModel.SyncCompleteArg = dialogViewController;
            App.ViewModel.SyncComplete += RefreshHandler;
            dialogViewController.RefreshRequested += delegate 
            {
                App.ViewModel.SyncWithService();
            };
                    
            this.PushViewController(dialogViewController, false);
        }
        
        private void CreateAddButtons()
        {
            // get all the lists
            lists = (from it in App.ViewModel.Items 
                          where it.IsList == true && it.ItemTypeID != SystemItemTypes.Reference
                          orderby it.Name ascending
                          select it).ToList();
            // create a list of buttons - one for each list
            buttonList = (from it in lists
                          select new Button() 
                          {
                              Background = "Images/darkgreybutton.png", 
                              Caption = it.Name, 
                              Clicked = AddButton_Click 
                          }).ToList();
            
            // clear the button rows
            for (int i = 0; i < AddButtons.Length; i++) 
                AddButtons[i] = null;
            
            // assemble the buttons into rows (maximum of six buttons and two rows)
            // if there are three or less buttons, one row
            // otherwise distribute evenly across two rows
            int count = Math.Min(buttonList.Count, MaxLists);
            int firstrow = count, secondrow = 0, addButtonsRow = 0;
            if (count > MaxLists / 2)
            {
                firstrow = count / 2;
                secondrow = count - firstrow;
            }
            if (firstrow > 0) 
            {
                AddButtons[addButtonsRow++] = new ButtonListElement()
                {
                    buttonList.Take(firstrow)
                };
            }
            if (secondrow > 0)
            {
                AddButtons[addButtonsRow++] = new ButtonListElement()
                {
                    buttonList.Skip(firstrow).Take(secondrow)
                };
            }
            
            // create a last "row" of buttons containing only one "More..." button which will bring up the folder/list page
            AddButtons[addButtonsRow] = new ButtonListElement() 
            { 
                new Button() 
                {
                    Background = "Images/darkgreybutton.png", 
                    Caption = "More...", 
                    Clicked = (s, e) => 
                    {
                        // assemble a page which contains a hierarchy of every folder and list, grouped by folder 
                        ListsRootElement = new RootElement("Add to list:")              
                        {
                            from f in App.ViewModel.Folders
                                orderby f.Name ascending
                                group f by f.Name into g
                                select new Section() 
                                {
                                    new StyledStringElement(g.Key, delegate { AddItemToFolder(g.Key); }) { Image = new UIImage("Images/appbar.folder.rest.png") },                                      
                                    from hs in g 
                                        from it in App.ViewModel.Items 
                                            where it.FolderID == hs.ID && it.IsList == true && it.ItemTypeID != SystemItemTypes.Reference
                                            orderby it.Name ascending
                                            select (Element) new StyledStringElement("        " + it.Name, delegate { AddItem(hs, it); }) { Image = new UIImage("Images/179-notepad.png") }
                                }
                        };
                        var dvc = new DialogViewController(ListsRootElement);
                        dvc.Title = (Name.Value == null || Name.Value == "") ? "Navigate to:" : "Add " + Name.Value + " to:";
                        dialogViewController.NavigationController.PushViewController(dvc, true); 
                    }
                }
            };                        
        }
        
        #endregion
    }   
    
    internal class SpeechPopupDelegate : UIActionSheetDelegate
    {
        public NuanceHelper.SpeechState speechState { get; set; }
        public string speechDebugString { get; set; }
        public DateTime speechStart { get; set; }        
        public AddPage parent { get; set; }    
        
        public override void Canceled (UIActionSheet actionSheet)
        {
            // cancel the current operation / close the socket to the service
            NuanceHelper.Cancel(
                new MainViewModel.NetworkOperationInProgressCallbackDelegate(SpeechPopup_NetworkOperationInProgressCallBack));
        }
        
        public override void Clicked (UIActionSheet actionSheet, int buttonIndex)
        {
            switch (buttonIndex)
            {
                case 0: // Done
                    SpeechPopup_SpeakButton_Click(null, null);
                    break;
                case 1: // Cancel
                    SpeechPopup_CancelButton_Click(null, null);
                    break;
            }
        }
        
        private void SpeechPopup_CancelButton_Click(object sender, EventArgs e)
        {
            switch (speechState)
            {
                case NuanceHelper.SpeechState.Initializing:
                case NuanceHelper.SpeechState.Listening:
                case NuanceHelper.SpeechState.Recognizing:
                    // user tapped the cancel button

                    // cancel the current operation / close the socket to the service
                    NuanceHelper.Cancel(
                        new MainViewModel.NetworkOperationInProgressCallbackDelegate(SpeechPopup_NetworkOperationInProgressCallBack));

                    // reset the text in the textbox
                    //parent.Name.Value = "";
                    parent.SpeechTextLabel.Text = "";
                    break;
                case NuanceHelper.SpeechState.Finished:
                    // user tapped the OK button

                    // set the text in the popup textbox
                    //parent.Name.Value = SpeechLabelText.Trim('\'');
                    //parent.Name.Value = "";
                    break;
            }
 
            SpeechPopup_Close();
        }

        private void SpeechPopup_Close()
        {
            // UIActionSheet is automatically closed
        }

        public void SpeechPopup_NetworkOperationInProgressCallBack(bool operationInProgress, OperationStatus status)
        {
            // call the MainViewModel's routine to make sure global network status is reset
            App.ViewModel.NetworkOperationInProgressCallback(operationInProgress, status);

            // signal whether the net operation is in progress or not
            //NetworkOperationInProgress = (operationInProgress == true ? Visibility.Visible : Visibility.Collapsed);

            // if the operationSuccessful flag is null, no new data; otherwise, it signals the status of the last operation
            if (status != OperationStatus.Started)
            {
                if (status != OperationStatus.Success)
                {   // the server wasn't reachable
                    parent.BeginInvokeOnMainThread(() =>
                    {
                        MessageBox.Show("Unable to access the speech service at this time.");
                        SpeechPopup_Close();
                    });
                }
            }
        }

        private void SpeechPopup_SpeakButton_Click(object sender, EventArgs e)
        {
            TimeSpan ts;
            string stateString;
            string traceString;

            switch (speechState)
            {
                case NuanceHelper.SpeechState.Initializing:
                    // can't happen since the button isn't enabled
#if DEBUG
                    //MessageBox.Show("Invalid state SpeechState.Initializing reached");
#endif
                    //break;
                case NuanceHelper.SpeechState.Listening:
                    // done button tapped

                    // set the UI state to recognizing state
                    speechState = NuanceHelper.SpeechState.Recognizing;
                    SpeechSetUIState(speechState);

                    // store debug / timing info
                    ts = DateTime.Now - speechStart;
                    stateString = NuanceHelper.SpeechStateString(speechState);
                    traceString = String.Format("New state: {0}; Time: {1}; Message: {2}", stateString, ts.TotalSeconds, "Stopping mic");
                    TraceHelper.AddMessage(traceString);
                    speechDebugString += traceString + "\n";

                    // stop listening and get the recognized text from the speech service
                    NuanceHelper.Stop(); 
                    
                    // put the SpeechPopup back up to show the recognition phase
                    parent.SetupSpeechPopup("recognizing...");
                    parent.SpeechPopup.ShowFromTabBar(((UITabBarController)parent.ParentViewController).TabBar);
                    break;
                case NuanceHelper.SpeechState.Recognizing:
                    // clicking done while recognizing means cancel the operation
                    parent.SpeechPopup.DismissWithClickedButtonIndex(1, true);
                    break;
                case NuanceHelper.SpeechState.Finished:
                    // should never happen
                    parent.SpeechPopup.DismissWithClickedButtonIndex(1, true);
                    break;
            }
        }

        public void SpeechPopup_SpeechStateCallback(NuanceHelper.SpeechState state, string message)
        {
            speechState = state;
            SpeechSetUIState(speechState);

            // store debug / timing info
            TimeSpan ts = DateTime.Now - speechStart;
            string stateString = NuanceHelper.SpeechStateString(state);
            string traceString = String.Format("New state: {0}; Time: {1}; Message: {2}", stateString, ts.TotalSeconds, message);
            TraceHelper.AddMessage(traceString);
            speechDebugString += traceString + "\n";
        }

        public void SpeechPopup_SpeechToTextCallback(string textString)
        {
            parent.BeginInvokeOnMainThread(() =>
            {
                // set the UI state to finished state
                speechState = NuanceHelper.SpeechState.Finished;
                SpeechSetUIState(speechState);

                // store debug / timing info
                TimeSpan ts = DateTime.Now - speechStart;
                string stateString = NuanceHelper.SpeechStateString(speechState);
                string traceString = String.Format("New state: {0}; Time: {1}; Message: {2}", stateString, ts.TotalSeconds, textString);
                TraceHelper.AddMessage(traceString);
                speechDebugString += traceString + "\n";

                // strip any timing / debug info 
                textString = textString == null ? "" : textString;
                string[] words = textString.Split(' ');
                if (words[words.Length - 1] == "seconds")
                {
                    textString = "";
                    // strip off last two words - "a.b seconds"
                    for (int i = 0; i < words.Length - 2; i++)
                    {
                        textString += words[i];
                        textString += " ";
                    }
                    textString = textString.Trim();
                }

                // set the speech label text as well as the popup text
                //SpeechLabelText = textString == null ? "recognition failed" : String.Format("'{0}'", textString);
                parent.Name.Value = textString;
    
                // dismiss the SpeechPopup
                parent.SpeechPopup.DismissWithClickedButtonIndex(1, true);
                
#if DEBUG && KILL
                MessageBox.Show(speechDebugString);
#endif
            });
        }
        
        #region Helpers
        
        /// <summary>
        /// Set the UI based on the current state of the speech state machine
        /// </summary>
        /// <param name="state"></param>
        private void SpeechSetUIState(NuanceHelper.SpeechState state)
        {
            switch (state)
            {
                case NuanceHelper.SpeechState.Initializing:
                    //parent.SpeechPopup.Title = "initializing...";
                    parent.SpeechTextLabel.Text = "initializing...";
                    break;
                case NuanceHelper.SpeechState.Listening:
                    //parent.SpeechPopup.Title = "listening...";
                    parent.SpeechTextLabel.Text = "listening...";
                    break;
                case NuanceHelper.SpeechState.Recognizing:
                    //parent.SpeechPopup.Title = "recognizing...";
                    //parent.ActivityIndicator.Hidden = false;
                    parent.SpeechTextLabel.Text = "recognizing...";
                    break;
                case NuanceHelper.SpeechState.Finished:
                    //parent.SpeechPopup.Title = "";
                    parent.SpeechTextLabel.Text = "";
                    break;
            }
            
            // resize and recenter the label
            parent.SpeechTextLabel.SizeToFit();
            parent.SpeechTextLabel.Center = new PointF(parent.View.Center.X, parent.ActivityIndicator.Center.Y);
        }
        
        #endregion
    }
}

