//
//  SpeechKitLibrary.m
//  SpeechKitLibrary
//
//  Created by Omri Gazitt on 2/23/12.
//  Copyright (c) 2012 BuiltSteady. All rights reserved.
//

#import "SpeechKitLibrary.h"

const unsigned char SpeechKitApplicationKey[] = {0x55, 0x83, 0xcd, 0x00, 0xa8, 0x8c, 0xc5, 0x8e, 0xa8, 0xa8, 0x93, 0xf1, 0x45, 0x40, 0x17, 0xfe, 0x25, 0x3c, 0x6d, 0xfc, 0x8c, 0xda, 0xaa, 0x2a, 0x33, 0x7d, 0xc0, 0x56, 0x2b, 0xbc, 0xd6, 0x80, 0x40, 0xa3, 0x81, 0xe8, 0x30, 0x46, 0x76, 0xd8, 0xee, 0x09, 0xc9, 0x33, 0x49, 0xe0, 0x31, 0x6e, 0x1c, 0x9e, 0x6a, 0xa8, 0x79, 0x14, 0xd3, 0xac, 0x91, 0x93, 0x02, 0xbd, 0x50, 0xd8, 0x3d, 0x90};

// the SpeechKitWrapper isn't actually used - rather, it is a way to exercise all the API's that 
// the binding library needs from the SpeechKit framework, so that those can be linked into the generated .a file.

@implementation SpeechKitWrapper
@synthesize status;

- (id)initWithDelegate:(id <SKRecognizerDelegate>)delegate
{
    self = [super init];
    if (self) {
        del = delegate;
        [self setStatus:@"initializing"];
        [SpeechKit setupWithID:@"NMDPTRIAL_ogazitt20120220010133"
                          host:@"sandbox.nmdp.nuancemobility.net"
                          port:443
                        useSSL:NO
                      delegate:nil];
        
        NSString *text = [NSString stringWithFormat:@"initialized.  sessionid = %@", [SpeechKit sessionID]];
        [self setStatus:text];        
        
        SKEarcon* earconStart	= [SKEarcon earconWithName:@"beep.wav"];
        [SpeechKit setEarcon:earconStart forType:SKStartRecordingEarconType];
        
        voiceSearch = [[SKRecognizer alloc] initWithType:SKDictationRecognizerType
                                               detection:SKLongEndOfSpeechDetection
                                                language:@"en_US" 
                                                delegate:delegate];
        
        text = [NSString stringWithFormat:@"recognizer connecting.  sessionid = %@", [SpeechKit sessionID]];
        [self setStatus:text];  
    }
    
    return self;
}

@end
