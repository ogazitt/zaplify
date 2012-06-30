using System;
using BuiltSteady.Zaplify.Devices.ClientEntities;

namespace BuiltSteady.Zaplify.Devices.ClientHelpers
{
    public class SpeechHelper
    {
        // Speech providers implement three methods - Cancel, Start, Stop.  
        public enum SpeechProviders
        {
            NativeSpeech,
            NuanceSpeech
        };
        
        // default to using the Nuance Speech provider
        private static SpeechProviders speechProvider = SpeechProviders.NuanceSpeech;
        public static SpeechProviders SpeechProvider { get { return speechProvider; } set { speechProvider = value; } }

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
            switch (SpeechProvider)
            {
                case SpeechProviders.NativeSpeech:
                    NativeSpeechHelper.Cancel(networkDel);
                    break;
                case SpeechProviders.NuanceSpeech:
                    NuanceSpeechHelper.Cancel(networkDel);
                    break;
            }
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
            switch (SpeechProvider)
            {
                case SpeechProviders.NativeSpeech:
                    NativeSpeechHelper.Start(u, del, networkDel);
                    break;
                case SpeechProviders.NuanceSpeech:
                    NuanceSpeechHelper.Start(u, del, networkDel);
                    break;
            }
        }

        public static void Stop(SpeechToTextCallbackDelegate del)
        {
            switch (SpeechProvider)
            {
                case SpeechProviders.NativeSpeech:
                    NativeSpeechHelper.Stop(del);
                    break;
                case SpeechProviders.NuanceSpeech:
                    NuanceSpeechHelper.Stop(del);
                    break;
            }
        }
    }
}
