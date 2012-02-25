using System;

namespace Nuance.SpeechKit
{
    // SKEarcon.h
    public enum SKEarconType
    {
        SKStartRecordingEarconType = 1,
        SKStopRecordingEarconType = 2,
        SKCancelRecordingEarconType = 3,
    };

    // SKRecognizer.h
    public enum SKEndOfSpeechDetection 
    {
        SKNoEndOfSpeechDetection = 1,
        SKShortEndOfSpeechDetection = 2,
        SKLongEndOfSpeechDetection = 3,
    };

    public static class SKRecognizerType
    {
        public static string SKDictationRecognizerType = "dictation";
        public static string SKWebSearchRecognizerType = "websearch";
    };
	
    // SpeechKitErrors.h
    public enum SpeechKitErrors
    {
        SKServerConnectionError = 1,
        SKServerRetryError = 2,
        SKRecognizerError = 3,
        SKVocalizerError = 4,
        SKCancelledError = 5,
    };
}

