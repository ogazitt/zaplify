using System;
using MonoTouch.ObjCRuntime;

[assembly: LinkWith ("libSpeechKitLibraryUniversal.a", LinkTarget.Simulator | LinkTarget.ArmV6 | LinkTarget.ArmV7, ForceLoad = true)]
