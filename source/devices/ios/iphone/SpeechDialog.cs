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

    public class SpeechEventArgs : EventArgs
    {
        public string Text;
    }

	public class SpeechDialog
	{
        private SpeechPopupDelegate SpeechPopupDelegate = new SpeechPopupDelegate();
        public UIActionSheet SpeechPopup;
        public UILabel SpeechTextLabel = new UILabel();
        public UIActivityIndicatorView ActivityIndicator = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.WhiteLarge) { Hidden = true };
        public UIViewController controller;
        public UIBarButtonItem speechBarButtonItem;

        public SpeechDialog(UIViewController vc)
        {
            controller = vc;
        }

        public delegate void SpeechEventHandler(object sender, SpeechEventArgs args);
        public event SpeechEventHandler OnResults;

        // handle events associated with the Speech Popup
        public void Open(UIBarButtonItem speechItem)
        {
            if (speechItem == null)
                return;

            speechBarButtonItem = speechItem;
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
                //this.TabBarController.PresentViewController(this.TabBarController.ViewControllers[3], true, null);
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

            if (SpeechPopupDelegate.speechState == NuanceHelper.SpeechState.Initializing)
            {
                // open the popup
                SetupSpeechPopup("initializing...");
                SpeechPopup.ShowFrom(speechBarButtonItem, true);
            }
            else
                MessageBox.Show("our apologies - speech is unavailable at this time.");
        }

        public void InvokeEvent(string text)
        {
            if (OnResults != null)
                OnResults(this, new SpeechEventArgs() { Text = text });
        }

        public void SetupSpeechPopup(string text)
        {
            SpeechPopup = new UIActionSheet(" ", SpeechPopupDelegate, "Cancel", "Done");

            // center and add the activity indicator             
            ActivityIndicator.Center = new System.Drawing.PointF(controller.View.Center.X, ActivityIndicator.Center.Y);
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
            SpeechTextLabel.Center = new PointF(controller.View.Center.X, ActivityIndicator.Center.Y);
            SpeechPopup.AddSubview(SpeechTextLabel);

            // start the animation
            ActivityIndicator.StartAnimating();
        }
    }   
    
    internal class SpeechPopupDelegate : UIActionSheetDelegate
    {
        public NuanceHelper.SpeechState speechState { get; set; }
        public string speechDebugString { get; set; }
        public DateTime speechStart { get; set; }        
        public SpeechDialog parent { get; set; }    
        
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
                    parent.controller.BeginInvokeOnMainThread(() =>
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
                    parent.SpeechPopup.ShowFrom(parent.speechBarButtonItem, true);
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
            parent.controller.BeginInvokeOnMainThread(() =>
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
                parent.InvokeEvent(textString);
                //parent.Name.Value = textString;
    
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
            parent.SpeechTextLabel.Center = new PointF(parent.controller.View.Center.X, parent.ActivityIndicator.Center.Y);
        }
        
        #endregion
    }
}

