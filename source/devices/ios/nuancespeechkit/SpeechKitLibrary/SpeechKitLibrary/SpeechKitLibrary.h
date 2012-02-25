//
//  SpeechKitLibrary.h
//  SpeechKitLibrary
//
//  Created by Omri Gazitt on 2/23/12.
//  Copyright (c) 2012 BuiltSteady. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <SpeechKit/SpeechKit.h>

@interface SpeechKitWrapper : NSObject //<SKRecognizerDelegate>
{
    NSString *status;
    SKRecognizer *voiceSearch;
    NSObject *del;
}

@property (nonatomic,retain) NSString *status;

@end
