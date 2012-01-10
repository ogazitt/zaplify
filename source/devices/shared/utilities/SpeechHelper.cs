using System;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using TaskStoreClientEntities;
using System.IO;
using System.Collections.Generic;
using NSpeex;
using System.Windows.Resources;
using Microsoft.Phone.Net.NetworkInformation;
using System.Threading;

namespace TaskStoreWinPhoneUtilities
{
    public class SpeechHelper
    {
        private static Microphone mic = Microphone.Default;
        private static bool initializedBufferReadyEvent = false;
        private static bool speechOperationInProgress = false;
        private static byte[] speechBuffer;
        private static List<byte[]> speechBufferList = new List<byte[]>();
        private static Delegate networkDelegate;
        private static Delegate speechStateDelegate;
        private static int numBytes = 0;
        private static int frameCounter = 0;
        private static string encoding = "application/pcm";
        private static bool encode = false;
        private static List<AutoResetEvent> bufferMutexList = new List<AutoResetEvent>();
        private static User user;

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

        public static void CancelStreamed(Delegate networkDel)
        {
            // stop listening
            mic.Stop();

            NetworkHelper.CancelSpeech();
            networkDel.DynamicInvoke(false, null);

            speechOperationInProgress = false;
        }

        public static string SpeechStateString(SpeechState state)
        {
            switch (state)
            {
                case SpeechHelper.SpeechState.Initializing:
                    return "Initializing";
                case SpeechHelper.SpeechState.Listening:
                    return "Listening";
                case SpeechHelper.SpeechState.Recognizing:
                    return "Recognizing";
                case SpeechHelper.SpeechState.Finished:
                    return "Finished";
                default:
                    return "Unrecognized";
            }
        }

        public static void Start(User u, SpeechStateCallbackDelegate del, Delegate networkDel)
        {
            // StartStreamed is not reentrant - make sure the caller didn't violate the contract
            if (speechOperationInProgress == true)
                return;

            // set the flag
            speechOperationInProgress = true;

            // store the delegates passed in
            speechStateDelegate = del;
            networkDelegate = networkDel;
            user = u;

            // initialize the microphone information and speech buffer
            mic.BufferDuration = TimeSpan.FromSeconds(1);
            int length = mic.GetSampleSizeInBytes(mic.BufferDuration);
            speechBuffer = new byte[length];
            speechBufferList.Clear();
            bufferMutexList.Clear();
            numBytes = 0;

            // trace the speech request
            TraceHelper.AddMessage("Starting Speech");

            // initialize frame index
            frameCounter = 0;

            // callback when the mic gathered 1 sec worth of data
            if (initializedBufferReadyEvent == false)
            {
                mic.BufferReady += new EventHandler<EventArgs>(MicBufferReady);
                initializedBufferReadyEvent = true;
            }

            // connect to the web service, and once that completes successfully,
            // it will invoke the NetworkInterfaceCallback delegate to indicate the network quality 
            // this delegate will then turn around and send the appropriate encoding in the SendPost call
            NetworkHelper.BeginSpeech(
                new NetworkInformationCallbackDelegate(NetworkInformationCallback),
                new NetworkDelegate(NetworkCallback));
        }

        public static void Stop(SpeechToTextCallbackDelegate del)
        {
            // get the last chunk of speech 
            int len = mic.GetData(speechBuffer);

            // stop listening
            mic.Stop();

            // remove the mic eventhandler
            mic.BufferReady -= MicBufferReady;
            initializedBufferReadyEvent = false;

            // trace the operation
            TraceHelper.AddMessage(String.Format("Final Frame: {0} bytes of speech", len));

            // create a properly sized copy of the last buffer
            byte[] speechChunk = new byte[len];
            Array.Copy(speechBuffer, speechChunk, len);

            // add the last speech buffer to the list
            speechBufferList.Add(speechChunk);

            // if the encode flag is set, encode the chunk before sending it
            if (encode)
            {
                // do this on a background thread because it is CPU-intensive
                ThreadPool.QueueUserWorkItem(delegate
                {
                    // create a new mutex object for this frame
                    AutoResetEvent bufferMutex = new AutoResetEvent(false);
                    bufferMutexList.Add(bufferMutex);

                    // encode the frame
                    TraceHelper.AddMessage(String.Format("Final Frame: About to encode speech"));
                    byte[] encodedBuf = EncodeSpeech(speechChunk, speechChunk.Length);
                    TraceHelper.AddMessage(String.Format("Final Frame: Encoded down to {0} bytes", encodedBuf.Length));

                    // wait until the previous frame has been sent
                    int frameIndex = bufferMutexList.Count - 1;
                    if (frameIndex > 0)
                        bufferMutexList[frameIndex - 1].WaitOne();

                    // send the last frame and retrieve the response
                    TraceHelper.AddMessage(String.Format("Sending Final Frame: {0} bytes", encodedBuf.Length));
                    NetworkHelper.EndSpeech(encodedBuf, encodedBuf.Length, del, new NetworkDelegate(NetworkCallback));

                    // repeat the sentence back to the user
                    PlaybackSpeech();
                });
            }
            else
            {
                // send the operation immediately 
                TraceHelper.AddMessage(String.Format("Sending Final Frame: {0} bytes", speechChunk.Length));
                NetworkHelper.EndSpeech(speechChunk, speechChunk.Length, del, new NetworkDelegate(NetworkCallback));

                // play back the speech immediately
                PlaybackSpeech();
            }
        }

        #region Delegates

        private static void MicBufferReady(object sender, EventArgs e)
        {
            // get the data from the mic
            int len = mic.GetData(speechBuffer);
            numBytes += len;

            // make a copy of the frame index and then increment it
            int frameIndex = frameCounter++;

            // trace the operation
            TraceHelper.AddMessage(String.Format("Frame {0}: {1} bytes of speech", frameIndex, len));

            // create a properly sized copy of the speech buffer
            byte[] speechChunk = new byte[len];
            Array.Copy(speechBuffer, speechChunk, len);

            // add the chunk to the buffer list
            speechBufferList.Add(speechChunk);

            // send the chunk to the service
            try
            {
                // if the encode flag is set, encode the chunk before sending it
                if (encode)
                {
                    // do this on a background thread because it is CPU-intensive
                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        // create a new mutex object for this frame
                        AutoResetEvent bufferMutex = new AutoResetEvent(false);
                        bufferMutexList.Add(bufferMutex);

                        // encode the frame
                        TraceHelper.AddMessage(String.Format("Frame {0}: About to encode speech", frameIndex));
                        byte[] encodedBuf = EncodeSpeech(speechChunk, speechChunk.Length);
                        TraceHelper.AddMessage(String.Format("Frame {0}: Encoded down to {1} bytes", frameIndex, encodedBuf.Length));

                        // wait until the previous frame has been sent
                        if (frameIndex > 0)
                            bufferMutexList[frameIndex - 1].WaitOne();

                        // send the frame
                        TraceHelper.AddMessage(String.Format("Sending Frame {0}: {1} bytes", frameIndex, encodedBuf.Length));
                        NetworkHelper.SendSpeech(encodedBuf, encodedBuf.Length, null, new NetworkDelegate(NetworkCallback));

                        // set the current frame's mutex
                        bufferMutex.Set();
                    });
                }
                else
                {
                    // just send the frame
                    TraceHelper.AddMessage(String.Format("Sending Frame {0}: {1} bytes", frameIndex, speechChunk.Length));
                    NetworkHelper.SendSpeech(speechChunk, speechChunk.Length, null, new NetworkDelegate(NetworkCallback));
                }
            }
            catch (Exception ex)
            {
                // stop listening
                mic.Stop();

                // remove the mic eventhandler
                mic.BufferReady -= MicBufferReady;
                initializedBufferReadyEvent = false;

                // trace the exception
                TraceHelper.AddMessage(String.Format("Mic buffer ready: ex: {0}", ex.Message));

                speechOperationInProgress = false;
                return;
            }

#if DEBUG
            // signal the caller that the chunk has been sent
            speechStateDelegate.DynamicInvoke(SpeechState.Listening, numBytes.ToString());
#endif
        }

        public delegate void NetworkDelegate(bool operationInProgress, bool? operationSuccessful);
        private static void NetworkCallback(bool operationInProgress, bool? operationSuccessful)
        {
            // if the network operation returned, clean up resources
            if (operationSuccessful != null)
            {
                if ((bool)operationSuccessful == false)
                {
                    // stop listening
                    mic.Stop();

                    // remove the mic eventhandler
                    mic.BufferReady -= MicBufferReady;
                    initializedBufferReadyEvent = false;
                }

                // indicate speech operation no longer in progress
                speechOperationInProgress = false;
            }

            // invoke the caller-supplied network delegate
            networkDelegate.DynamicInvoke(operationInProgress, operationSuccessful);
        }

        public delegate void NetworkInformationCallbackDelegate(NetworkInterfaceInfo netInfo);
        private static void NetworkInformationCallback(NetworkInterfaceInfo netInfo)
        {
            // set encoding behavior based on speed of network: wifi networks don't need encoding, 
            // but cellular networks do
            string encString;
            if (IsSlowNetwork(netInfo))
            {
                encode = true;
                encString = "application/speex";
            }
            else
            {
                encode = false;
                encString = "application/pcm";
            }

            // initialize the encoding
            encoding = String.Format("{0}-{1}-{2}-{3}",
                encString,
                mic.SampleRate,
                "16",  // 16 bits per sample
                ((int)AudioChannels.Mono).ToString());  // 1 audio channel (mono)

            // Trace the operation
            TraceHelper.AddMessage("POST: " + encoding);

            // send the HTTP POST to initialize the speech operation
            // upon completion it will invoke the StartMic delegate to start the microphone
            NetworkHelper.SendPost(
                user, 
                "TaskStore-Speech-Encoding: " + encoding, 
                new StartMicDelegate(StartMic), 
                new NetworkDelegate(NetworkCallback));
        }
                

        public delegate void StartMicDelegate();
        private static void StartMic()
        {
            // update the speech state
            speechStateDelegate.DynamicInvoke(SpeechState.Listening, "Mic started");

            // start getting data from the mic
            mic.Start();
        }
        
        #endregion

        #region Helpers

        private static byte[] EncodeSpeech(byte[] buf, int len)
        {
            BandMode mode = GetBandMode(mic.SampleRate);
            SpeexEncoder encoder = new SpeexEncoder(mode);
            
            // set encoding quality to lowest (which will generate the smallest size in the fastest time)
            encoder.Quality = 1;

            int inDataSize = len / 2;
            // convert to short array
            short[] data = new short[inDataSize];
            int sampleIndex = 0;
            for (int index = 0; index < len; index += 2, sampleIndex++)
            {
                data[sampleIndex] = BitConverter.ToInt16(buf, index);
            }

            // note: the number of samples per frame must be a multiple of encoder.FrameSize
            inDataSize = inDataSize - inDataSize % encoder.FrameSize;

            var encodedData = new byte[len];
            int encodedBytes = encoder.Encode(data, 0, inDataSize, encodedData, 0, len);
            if (encodedBytes != 0)
            {
                // each chunk is laid out as follows:
                // | 4-byte total chunk size | 4-byte encoded buffer size | <encoded-bytes> |
                byte[] inDataSizeBuf = BitConverter.GetBytes(inDataSize);
                byte[] sizeBuf = BitConverter.GetBytes(encodedBytes + inDataSizeBuf.Length);
                byte[] returnBuf = new byte[encodedBytes + sizeBuf.Length + inDataSizeBuf.Length];
                sizeBuf.CopyTo(returnBuf, 0);
                inDataSizeBuf.CopyTo(returnBuf, sizeBuf.Length);
                Array.Copy(encodedData, 0, returnBuf, sizeBuf.Length + inDataSizeBuf.Length, encodedBytes);
                return returnBuf;
            }
            else
                return buf;

        }

        private static BandMode GetBandMode(int sampleRate)
        {
            if (sampleRate <= 8000)
                return BandMode.Narrow;
            if (sampleRate <= 16000)
                return BandMode.Wide;
            return BandMode.UltraWide;
        }

        private static bool IsSlowNetwork(NetworkInterfaceInfo netInfo)
        {
            switch (netInfo.InterfaceType)
            {
                case NetworkInterfaceType.MobileBroadbandCdma:
                case NetworkInterfaceType.MobileBroadbandGsm:
                    return true;
                case NetworkInterfaceType.Ethernet:
                case NetworkInterfaceType.Wireless80211:
                    return false;
                default:
                    switch (netInfo.InterfaceSubtype)
                    {
                        case NetworkInterfaceSubType.WiFi:
                        case NetworkInterfaceSubType.Desktop_PassThru:
                            return false;
                        default:
                            return true;
                    }
            }
        }

        private static void PlaybackSpeech()
        {
            // trace the operation
            TraceHelper.AddMessage("About to playback speech");

            // create a sound effect instance
            DynamicSoundEffectInstance effect = new DynamicSoundEffectInstance(mic.SampleRate, AudioChannels.Mono);

            // submit all the buffers to the instance
            foreach (var buf in speechBufferList)
                if (buf.Length > 0)
                    effect.SubmitBuffer(buf);
            speechBufferList.Clear();

            // create an event handler to stop playback when all the buffers have been consumed
            effect.BufferNeeded += delegate
            {
                if (effect.PendingBufferCount == 0)
                {
                    effect.Stop();
                    TraceHelper.AddMessage("Finished playing back speech");
                }
            };

            // play the speech
            FrameworkDispatcher.Update();
            effect.Play();
        }

        #endregion
    }

    #region XNA scaffolding

    public class XNAFrameworkDispatcherService : IApplicationService
    {
        private DispatcherTimer frameworkDispatcherTimer;

        public XNAFrameworkDispatcherService()
        {
            this.frameworkDispatcherTimer = new DispatcherTimer();
            this.frameworkDispatcherTimer.Interval = TimeSpan.FromTicks(333333);
            this.frameworkDispatcherTimer.Tick += frameworkDispatcherTimer_Tick;
            FrameworkDispatcher.Update();
        }

        void frameworkDispatcherTimer_Tick(object sender, EventArgs e) { FrameworkDispatcher.Update(); }

        void IApplicationService.StartService(ApplicationServiceContext context) { this.frameworkDispatcherTimer.Start(); }

        void IApplicationService.StopService() { this.frameworkDispatcherTimer.Stop(); }
    }

    #endregion
}
