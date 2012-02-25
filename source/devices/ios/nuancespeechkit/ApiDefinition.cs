using System;
using MonoTouch.Foundation;

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

    // SKEarcon.h
    [BaseType(typeof(NSObject))]
    interface SKEarcon 
    {
        [Export("initWithContentsOfFile:")]
        IntPtr Constructor(string path);
        
        [Static, Export("earconWithName:")]
        IntPtr FromName(string name);
    }   
    
    // SKRecognition.h
    [BaseType(typeof(NSObject))]
    interface SKRecognition
    {
        [Export("results")]
        string[] Results { get; }
        
        [Export("scores")]
        NSNumber[] Scores { get; }
        
        [Export("suggestion")]
        string Suggestion { get; }
        
        [Export("firstResult")]
        string FirstResult();
    }
	
    // SKRecognizer.h
    [BaseType(typeof(NSObject))]
    interface SKRecognizer
    {
        [Export("audioLevel")]
        float AudioLevel { get; }
        
        [Export ("initWithType:detection:language:delegate:")]
        IntPtr Constructor (string type, SKEndOfSpeechDetection detection, string language, SKRecognizerDelegate del);
        
        [Export("stopRecording")]
        void StopRecording();
        
        [Export("cancel")]
        void Cancel();
        
        /*
        [Field ("SKSearchRecognizerType", "__Internal")]
        NSString SKSearchRecognizerType { get; }
        
        [Field ("SKDictationRecognizerType", "__Internal")]
        NSString SKDictationRecognizerType { get; }
        */
    }

    [BaseType(typeof(NSObject))]
    [Model]
    interface SKRecognizerDelegate
    {
        [Export("recognizerDidBeginRecording:")]
        void OnRecordingBegin (SKRecognizer recognizer);
        
        [Export("recognizerDidFinishRecording:")]
        void OnRecordingDone (SKRecognizer recognizer);
        
        [Export("recognizer:didFinishWithResults:")]
        [Abstract]
        void OnResults (SKRecognizer recognizer, SKRecognition results);
        
        [Export("recognizer:didFinishWithError:suggestion:")]
        [Abstract]
        void OnError (SKRecognizer recognizer, NSError error, string suggestion);
    }   
	
    // speechkit.h
    [BaseType(typeof(NSObject))]
    interface SpeechKit
    {
        [Static, Export("setupWithID:host:port:useSSL:delegate:")]
        void Initialize(string id, string host, int port, bool useSSL, [NullAllowed] SpeechKitDelegate del);
        
        [Static, Export("destroy")]
        void Destroy();
        
        [Static, Export("sessionID")]
        string GetSessionID();
        
        [Static, Export("setEarcon:forType:")]
        void SetEarcon(SKEarcon earcon, SKEarconType type);     
    }
    
    [BaseType(typeof(NSObject))]
    [Model]
    interface SpeechKitDelegate
    {
        [Export("destroyed")]
        void Destroyed();   
    }   

    [BaseType(typeof(NSObject))]
    interface SpeechKitWrapper
    {
        [Export("initWithDelegate:")]
        IntPtr Constructor(SKRecognizerDelegate del);
	
        [Export("status")]
        string Status { get; set; }
    }
}

