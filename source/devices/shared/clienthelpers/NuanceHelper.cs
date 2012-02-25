using System;
using System.Threading;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using com.nuance.nmdp.speechkit;
using com.nuance.nmdp.speechkit.oem;

namespace BuiltSteady.Zaplify.Devices.ClientHelpers
{
    public class NuanceServiceInfo
    {
        public static bool SpeechKitSsl = false;

        public static string SpeechKitServer = "sandbox.nmdp.nuancemobility.net";

        public static int SpeechKitPort = 443;

        public static readonly string SpeechKitAppId = "NMDPTRIAL_ogazitt20120220010133";

        public static readonly byte[] SpeechKitApplicationKey =
        {
            0x55, 0x83, 0xcd, 0x00, 0xa8, 0x8c, 0xc5, 0x8e, 0xa8, 0xa8, 0x93, 0xf1, 0x45, 0x40, 0x17, 0xfe, 0x25, 0x3c, 0x6d, 0xfc, 0x8c, 0xda, 0xaa, 0x2a, 0x33, 0x7d, 0xc0, 0x56, 0x2b, 0xbc, 0xd6, 0x80, 0x40, 0xa3, 0x81, 0xe8, 0x30, 0x46, 0x76, 0xd8, 0xee, 0x09, 0xc9, 0x33, 0x49, 0xe0, 0x31, 0x6e, 0x1c, 0x9e, 0x6a, 0xa8, 0x79, 0x14, 0xd3, 0xac, 0x91, 0x93, 0x02, 0xbd, 0x50, 0xd8, 0x3d, 0x90
        };
    }

    public class NuanceHelper
    {
        private static SpeechKit _speechKit = null;
        private static Recognizer _recognizer = null;
        private static Prompt _beep = null;
        private static OemConfig _oemconfig = new OemConfig();
        private static object _handler = null;

        private static bool speechOperationInProgress = false;
        private static Delegate networkDelegate;
        private static Delegate speechStateDelegate;
        private static Delegate speechToTextDelegate;

        private static NuanceHelperCallback nuanceHelperCallback;
        private static NuanceHelperCallback NuanceHelperCallbackInstance
        {
            get
            {
                if (nuanceHelperCallback == null)
                    nuanceHelperCallback = new NuanceHelperCallback();
                return nuanceHelperCallback;
            }
        }

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

        public static void Cancel(Delegate networkDel)
        {
            // update the speech state
            speechStateDelegate.DynamicInvoke(SpeechState.Finished, "Canceled speech operation");

            if (_recognizer != null)
            {
                _recognizer.cancel();
            }

            // cancel the current operation
            if (_speechKit != null)
            {
                _speechKit.cancelCurrent();
            } 

            speechOperationInProgress = false;
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

        public static void Start(User u, SpeechStateCallbackDelegate del, Delegate networkDel)
        {
            // trace the speech request
            TraceHelper.AddMessage("Starting Speech");

            // Start is not reentrant - make sure the caller didn't violate the contract
            if (speechOperationInProgress == true)
                return;

            // store the delegates passed in
            speechStateDelegate = del;
            networkDelegate = networkDel;

            // set the flag
            speechOperationInProgress = true;

            // initialize the connection
            if (SpeechkitInitialize() == false)
            {
                Cancel(networkDel);
                return;
            }

            // start a new thread that starts the dictation
            DictationStart(RecognizerRecognizerType.Dictation);
        }

        public static void Stop(SpeechToTextCallbackDelegate del)
        {
            // trace the operation
            TraceHelper.AddMessage("Stopping Speech");

            // save the delegate passed in
            speechToTextDelegate = del;

            // stop recording and start recognizing
            _recognizer.stopRecording();

            // update the speech state
            speechStateDelegate.DynamicInvoke(SpeechState.Recognizing, "Stopped recording");
        }

        #region Helpers

        private static void DictationStart(string type)
        {
            TraceHelper.AddMessage("DictationStart");
            Thread thread = new Thread(() =>
            {
                // update the speech state
                speechStateDelegate.DynamicInvoke(SpeechState.Initializing, "Initializing");

                //speechkitInitialize();
                _recognizer = _speechKit.createRecognizer(type, RecognizerEndOfSpeechDetection.Long, _oemconfig.defaultLanguage(), NuanceHelperCallbackInstance, _handler);
                _recognizer.start();
            });
            thread.Start();
        }

        private static bool SpeechkitInitialize()
        {
            try
            {
                _speechKit = SpeechKit.initialize(
                    NuanceServiceInfo.SpeechKitAppId, 
                    NuanceServiceInfo.SpeechKitServer,
                    NuanceServiceInfo.SpeechKitPort, 
                    NuanceServiceInfo.SpeechKitSsl,
                    NuanceServiceInfo.SpeechKitApplicationKey);
            }
            catch (Exception ex)
            {
                TraceHelper.AddMessage("Exception in SpeechKitInitialize: " + ex.Message);
                return false;
            }

            _beep = _speechKit.defineAudioPrompt("beep.wav");
            _speechKit.setDefaultRecognizerPrompts(_beep, null, null, null);
            _speechKit.connect();
            Thread.Sleep(10); // to guarantee the time to load prompt resource

            return true;
        }

        internal class NuanceHelperCallback : RecognizerListener
        {
            public void onRecordingBegin(Recognizer recognizer)
            {
                TraceHelper.AddMessage("onRecordingBegin");
                speechStateDelegate.DynamicInvoke(SpeechState.Listening, "Started recording");

                // trace a bad state
                if (recognizer != _recognizer)
                    TraceHelper.AddMessage("recognizer doesn't match");
            }

            public void onRecordingDone(Recognizer recognizer)
            {
                TraceHelper.AddMessage("onRecordingDone");
                speechStateDelegate.DynamicInvoke(SpeechState.Recognizing, "Recognizing");

                // trace a bad state
                if (recognizer != _recognizer)
                    TraceHelper.AddMessage("recognizer doesn't match");
            }

            public void onResults(Recognizer recognizer, Recognition results)
            {
                string text = results.getResult(0).getText();
                TraceHelper.AddMessage("onResults: " + text);
                speechStateDelegate.DynamicInvoke(SpeechState.Finished, "Finished: " + text);
                speechToTextDelegate.DynamicInvoke(text);

                // trace a bad state
                if (recognizer != _recognizer)
                    TraceHelper.AddMessage("recognizer doesn't match");

                _recognizer.cancel();
                _recognizer = null;

                speechOperationInProgress = false;
            }

            public void onError(Recognizer recognizer, SpeechError error)
            {
                string text = error.getErrorDetail();
                TraceHelper.AddMessage("onError: " + text);
                speechStateDelegate.DynamicInvoke(SpeechState.Finished, text);

                // trace a bad state
                if (recognizer != _recognizer)
                    TraceHelper.AddMessage("recognizer doesn't match");

                _recognizer.cancel();
                _recognizer = null;
                _speechKit.release();
                _speechKit = null;

                speechOperationInProgress = false;
            }
        }

        #endregion
    }
}
