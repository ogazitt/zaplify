using System;
using System.Threading;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using Nuance.SpeechKit;
using MonoTouch.Foundation;
using System.IO;

namespace BuiltSteady.Zaplify.Devices.ClientHelpers
{
    public class NuanceServiceInfo
    {
        public static bool SpeechKitSsl = false;
        public static string SpeechKitServer = "sandbox.nmdp.nuancemobility.net";
        public static int SpeechKitPort = 443;
        public static readonly string SpeechKitAppId = "NMDPTRIAL_ogazitt20120220010133";
    }

    public class NuanceHelper
    {
        private static bool speechOperationInProgress = false;
        private static Delegate networkDelegate;
        private static Delegate speechStateDelegate;
        private static Delegate speechToTextDelegate;
  
        private static SKRecognizer recognizer = null;
        private static SKEarcon beep = null;

        /// <summary>
        /// State of the speech state machine
        /// </summary>
        public enum SpeechState
        {
            Initializing,
            Listening,
            Recognizing,
            Finished,
        }
        
        // delegate to call when the speech state changes
        public delegate void SpeechStateCallbackDelegate(SpeechState speechState, string message);

        // delegate to call with the recognized string
        public delegate void SpeechToTextCallbackDelegate(string textString);
  
        /// <summary>
        /// Cancel the current speech operation
        /// </summary>
        /// <param name='networkDel'>
        /// Network delegate to signal
        /// </param>
        public static void Cancel(Delegate networkDel)
        {
            // update the speech state
            if (speechStateDelegate != null)
                speechStateDelegate.DynamicInvoke(SpeechState.Finished, "Canceled speech operation");
   
            // cleanup
            CleanupSpeechKit();
        }
        
        public static void Cleanup()
        {
            CleanupSpeechKit();
            //SpeechKit.Destroy();
        }

        public static string SpeechStateString(SpeechState state)
        {
            switch (state)
            {
                case SpeechState.Initializing:
                    return "Initializing";
                case SpeechState.Listening:
                    return "Listening";
                case SpeechState.Recognizing:
                    return "Recognizing";
                case SpeechState.Finished:
                    return "Finished";
                default:
                    return "Unrecognized";
            }
        }

        public static void Start(User u, SpeechToTextCallbackDelegate speechToTextDel, SpeechStateCallbackDelegate speechStateDel, Delegate networkDel)
        {
            // trace the speech request
            TraceHelper.AddMessage("Starting Speech");

            // Start is not reentrant - make sure the caller didn't violate the contract
            if (speechOperationInProgress == true)
                return;

            // store the delegates passed in
            speechToTextDelegate = speechToTextDel;
            speechStateDelegate = speechStateDel;
            networkDelegate = networkDel;
   
            // initialize the connection
            if (InitializeSpeechKit() == false)
            {
                Cancel(networkDel);
                return;
            }
            
            // start a new thread that starts the dictation
            DictationStart(SKRecognizerType.SKDictationRecognizerType);
        }

        public static void Stop()
        {
            // trace the operation
            TraceHelper.AddMessage("Stopping Speech");

            // stop recording and start recognizing
            if (recognizer != null)
            {
                recognizer.StopRecording();
            
                // update the speech state
                speechStateDelegate.DynamicInvoke(SpeechState.Recognizing, "Stopped recording");
            }
            else
                CleanupSpeechKit();
        }

        #region Helpers
        
        private static void CleanupSpeechKit()
        {
            if (recognizer != null)
            {
                recognizer.Cancel();
                recognizer.Dispose();
            }
            recognizer = null;
            speechOperationInProgress = false;
        }
        
        private static void DictationStart(string type)
        {
            // trace the operation
            TraceHelper.AddMessage("DictationStart");
            
            // cleanup the current recognizer
            CleanupSpeechKit();
            
            // set the flag
            speechOperationInProgress = true;
            
            // create a new recognizer instance
            recognizer = new SKRecognizer(type, SKEndOfSpeechDetection.SKLongEndOfSpeechDetection, "en_US", new RecognizerDelegate());
        }

        private static bool InitializeSpeechKit()
        {
            try
            {
                // initialize the SpeechKit
                // the App Key is embedded in the SpeechKit objective-C library
                // to change App Keys this library needs to be updated and recompiled
                SpeechKit.Initialize(
                    NuanceServiceInfo.SpeechKitAppId, 
                    NuanceServiceInfo.SpeechKitServer,
                    NuanceServiceInfo.SpeechKitPort, 
                    NuanceServiceInfo.SpeechKitSsl,
                    new NuanceSpeechKitDelegate());
            }
            catch (Exception ex)
            {
                TraceHelper.AddMessage("Exception in SpeechKitInitialize: " + ex.Message);
                return false;
            }
            
            //beep = (SKEarcon) SKEarcon.FromName(p);
            string path = Path.GetFullPath("beep.wav");
            beep = new SKEarcon(path);
            SpeechKit.SetEarcon(beep, SKEarconType.SKStartRecordingEarconType);

            return true;
        }
  
        /// <summary>
        /// Nuance SpeechKit delegate.  This class handles the Destroyed
        /// event
        /// </summary>
        internal class NuanceSpeechKitDelegate : SpeechKitDelegate
        {          
            public override void Destroyed()
            {
                // cleanup
                CleanupSpeechKit();
            }
        }
        
        /// <summary>
        /// Recognizer delegate.  This class handles
        /// Begin/End events for recording, as well as results and errors
        /// </summary>
        public class RecognizerDelegate : SKRecognizerDelegate
        {          
            public override void OnRecordingBegin(SKRecognizer reco)
            {
                TraceHelper.AddMessage("OnRecordingBegin");
                speechStateDelegate.DynamicInvoke(SpeechState.Listening, "Started recording");

                // trace a bad state
                if (reco != recognizer)
                    TraceHelper.AddMessage("recognizer doesn't match");
            }

            public override void OnRecordingDone(SKRecognizer reco)
            {
                TraceHelper.AddMessage("OnRecordingDone");
                speechStateDelegate.DynamicInvoke(SpeechState.Recognizing, "Recognizing");

                // trace a bad state
                if (reco != recognizer)
                    TraceHelper.AddMessage("Recognizer doesn't match");
            }

            public override void OnResults(SKRecognizer reco, SKRecognition results)
            {
                string text = null;
                if (results.Results.Length > 0)
                    text = results.FirstResult();
                else
                    text = "didn't hear you - please try again";
                TraceHelper.AddMessage("OnResults: " + text);
                speechStateDelegate.DynamicInvoke(SpeechState.Finished, "Finished: " + text);
                speechToTextDelegate.DynamicInvoke(text);

                // trace a bad state
                if (reco != recognizer)
                    TraceHelper.AddMessage("recognizer doesn't match");
    
                // cleanup
                CleanupSpeechKit();
                speechOperationInProgress = false;
            }

            public override void OnError(SKRecognizer reco, NSError error, string suggestion)
            {
                string text = error.LocalizedDescription;
                TraceHelper.AddMessage("onError: " + text);
                speechStateDelegate.DynamicInvoke(SpeechState.Finished, text);

                // trace a bad state
                if (reco != recognizer)
                    TraceHelper.AddMessage("recognizer doesn't match");

                // cleanup
                CleanupSpeechKit();

                speechOperationInProgress = false;
            }
        }
        
        #endregion
    }
}
