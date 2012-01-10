using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel;
using System.ServiceModel.Web;
using Microsoft.ApplicationServer.Http;
using System.Net.Http;
using System.Net;
using System.Reflection;
using TaskStoreWeb.Helpers;
using TaskStoreWeb.Models;
using System.Data.Entity;
using Microsoft.Speech.Recognition;
using Microsoft.Speech.AudioFormat;
using System.IO;
using System.Net.Http.Headers;
using TaskStoreServerEntities;
using System.Threading;
using ServiceHelpers;
using NSpeex;

namespace TaskStoreWeb.Resources
{
    // singleton service, which manages thread-safety on its own
    [ServiceContract]
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, InstanceContextMode = InstanceContextMode.Single)]
    public class SpeechResource
    {
        private const int engines = 3;  // number of speech recognition engines to cache
        private static SpeechRecognitionEngine[] sreArray = new SpeechRecognitionEngine[engines];
        private static bool[] sreInUseArray = new bool[engines];

        private static SpeechAudioFormatInfo formatInfo = null;
        private static bool isDebugEnabled = false;
        private static object sreLock = new Object();
        private static SpeexDecoder speexDecoder = new SpeexDecoder(BandMode.Wide);

        // some default values for speech
        private static int defaultSampleRate = 16000;
        private static AudioBitsPerSample defaultBitsPerSample = AudioBitsPerSample.Sixteen;
        private static AudioChannel defaultAudioChannels = AudioChannel.Mono;

        public SpeechResource()
        {
            // Log function entrance
            LoggingHelper.TraceFunction();

            // enable debug flag if this is a debug build
#if DEBUG
            isDebugEnabled = true;
#endif
        }

        private TaskStore TaskStore
        {
            get
            {
                // if in a debug build, always go to the database
                if (isDebugEnabled)
                    return new TaskStore();
                else // retail build
                {
                    // use a cached context (to promote serving values out of EF cache) 
                    return TaskStore.Current;
                }
            }
        }

        /// <summary>
        /// Convert the byte array representing the speech wav format to a text string
        /// </summary>
        /// <returns>speech-to-text string</returns>
        [WebInvoke(UriTemplate = "", Method = "POST")]
        public HttpResponseMessageWrapper<string> SpeechToText(HttpRequestMessage req)
        {
            // Log function entrance
            LoggingHelper.TraceFunction();

            HttpStatusCode code = ResourceHelper.AuthenticateUser(req, TaskStore);
            if (code != HttpStatusCode.OK)
                return new HttpResponseMessageWrapper<string>(req, code);  // user not authenticated

            // get a free instance of the speech recognition engine
            SpeechRecognitionEngine sre = null;
            while (sre == null)
            {
                sre = GetSpeechEngine();

                // if all SRE's are in use, wait one second and then try again
                if (sre == null)
                    Thread.Sleep(1000);
            }

            try
            {
                // retrieve and set the stream to recognize
                Stream stream = req.Content.ContentReadStream;
                IEnumerable<string> values = new List<string>();
                if (req.Headers.Contains("TaskStore-Speech-Encoding") == true)
                    stream = GetStream(req);
                sre.SetInputToAudioStream(stream, formatInfo);

#if WRITEFILE || DEBUG
                User user = ResourceHelper.GetUserPassFromMessage(req);
                string msg = WriteSpeechFile(user, stream);
                if (msg != null)
                    return new HttpResponseMessageWrapper<string>(req, msg, HttpStatusCode.OK);
#endif

                // initialize timing information
                DateTime start = DateTime.Now;
                string responseString = null;

                // recognize
                var result = sre.Recognize();
                if (result == null)
                    responseString = "[unrecognized]";
                else
                    responseString = result.Text;

                // get timing information
                DateTime end = DateTime.Now;
                TimeSpan ts = end - start;

                // trace the recognized speech
                string timing = String.Format(" {0}.{1} seconds", ts.Seconds.ToString(), ts.Milliseconds.ToString());
                LoggingHelper.TraceLine(String.Format("Recognized '{0}' in{1}", responseString, timing), LoggingHelper.LogLevel.Detail);

                // construct the response
                responseString += timing;
                var response = new HttpResponseMessageWrapper<string>(req, responseString, HttpStatusCode.OK);
                response.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };

                // reset the speech engine (this is time-consuming so do it on a new thread)
                //ParameterizedThreadStart threadStart = new ParameterizedThreadStart(ResetSpeechEngine);
                //Thread thread = new Thread(threadStart);
                //thread.Start(new SpeechInfo() { Engine = sre, SpeechByteArray = speechToParse });

                // release engine instance
                ReleaseSpeechEngine(sre);

                // return the response
                return response;
            }
            catch (Exception ex)
            {
                // speech failed
                LoggingHelper.TraceError("Speech recognition failed: " + ex.Message);

                // release engine instance
                ReleaseSpeechEngine(sre);

                return new HttpResponseMessageWrapper<string>(req, HttpStatusCode.InternalServerError);
            }
        }

        #region Helpers

        private static Stream DecodeSpeexStream(Stream stream)
        {
            // Log function entrance
            LoggingHelper.TraceFunction();

            try
            {
                int totalEncoded = 0;
                int totalDecoded = 0;
                
                // decode all the speex-encoded chunks
                // each chunk is laid out as follows:
                // | 4-byte total chunk size | 4-byte encoded buffer size | <encoded-bytes> |
                MemoryStream ms = new MemoryStream();
                byte[] lenBytes = new byte[sizeof(int)];

                // get the length prefix
                int len = stream.Read(lenBytes, 0, lenBytes.Length);

                // loop through all the chunks
                while (len == lenBytes.Length)
                {
                    // convert the length to an int
                    int count = BitConverter.ToInt32(lenBytes, 0);
                    byte[] speexBuffer = new byte[count];
                    totalEncoded += count + len;

                    // read the chunk
                    len = stream.Read(speexBuffer, 0, count);
                    if (len < count)
                    {
                        LoggingHelper.TraceError(String.Format("Corrupted speex stream: len {0}, count {1}", len, count));
                        return ms;
                    }

                    // get the size of the buffer that the encoder used
                    // we need that exact size in order to properly decode
                    // the size is the first four bytes of the speexBuffer
                    int inDataSize = BitConverter.ToInt32(speexBuffer, 0);

                    // decode the chunk (starting at an offset of sizeof(int))
                    short[] decodedFrame = new short[inDataSize];
                    count = speexDecoder.Decode(speexBuffer, sizeof(int), len - sizeof(int), decodedFrame, 0, false);

                    // copy to a byte array
                    byte[] decodedBuffer = new byte[2 * count];
                    for (int i = 0, bufIndex = 0; i < count; i++, bufIndex += 2)
                    {
                        byte[] frame = BitConverter.GetBytes(decodedFrame[i]);
                        frame.CopyTo(decodedBuffer, bufIndex);
                    }

                    // write decoded buffer to the memory stream
                    ms.Write(decodedBuffer, 0, 2 * count);
                    totalDecoded += 2 * count;

                    // get the next length prefix
                    len = stream.Read(lenBytes, 0, lenBytes.Length);
                }

                // Log decoding stats
                LoggingHelper.TraceDetail(String.Format("Decoded {0} bytes into {1} bytes", totalEncoded, totalDecoded));
                
                // reset and return the new memory stream
                ms.Position = 0;
                return ms;
            }
            catch (Exception ex)
            {
                LoggingHelper.TraceError(String.Format("Corrupted speex stream: {0}", ex.Message));
                return null;
            }
        }

        SpeechRecognitionEngine GetSpeechEngine()
        {
            // Log function entrance
            LoggingHelper.TraceFunction();

            // this code must be thread safe
            lock (sreLock)
            {
                int i;
                for (i = 0; i < sreInUseArray.Length; i++)
                    if (sreInUseArray[i] == false)
                        break;

                if (sreInUseArray[i] == true)
                    return null;

                if (sreArray[i] == null)
                {
                    sreArray[i] = new SpeechRecognitionEngine();
                    InitializeSpeechEngine(sreArray[i]);
                }

                // set the in use flag and return the SRE
                sreInUseArray[i] = true;

                // log the speech engine used
                LoggingHelper.TraceLine(String.Format("Using SpeechEngine[{0}]", i), LoggingHelper.LogLevel.Detail);

                // return speech engine
                return sreArray[i];
            }
        }

        private Stream GetStream(HttpRequestMessage req)
        {
            Stream stream = req.Content.ContentReadStream;
            string contentType = null;

            // get the content type
            IEnumerable<string> values = new List<string>();
            if (req.Headers.TryGetValues("TaskStore-Speech-Encoding", out values) == true)
                contentType = values.ToArray<string>()[0];
            else
                return stream;

            // format for contentType string is: 
            //   application/<encoding>-<samplerate>-<bits/channel>-<audiochannels>
            string[] encoding = contentType.Split('-');
            string encodingType = encoding[0];
            int sampleRate = encoding.Length > 1 ? Convert.ToInt32(encoding[1]) : defaultSampleRate;
            int bitsPerSample = encoding.Length > 2 ? Convert.ToInt32(encoding[2]) : (int) defaultBitsPerSample;
            int audioChannels = encoding.Length > 3 ? Convert.ToInt32(encoding[3]) : (int) defaultAudioChannels;

            // reset formatInfo based on retrieved info
            formatInfo = new SpeechAudioFormatInfo(sampleRate, (AudioBitsPerSample)bitsPerSample, (AudioChannel)audioChannels);

            switch (encodingType)
            {
                case "application/speex":
                    return DecodeSpeexStream(stream);
                default:
                    // return the original stream
                    return stream;
            }
        }

        void InitializeGrammar(string grammarPath, string appDataPath, string fileName)
        {
            // Log function entrance
            LoggingHelper.TraceFunction();

            // construct App Root directory using drive letter from appDataPath and the approot path
            string appRootDir = appDataPath.Substring(0, 1) + @":\approot\Content\grammars";
            try
            {
                // if the file exists, do nothing
                FileStream fs = File.OpenRead(grammarPath);
                fs.Close();
            }
            catch (DirectoryNotFoundException)
            {
                LoggingHelper.TraceError("Directory " + appDataPath + " not found");
                // if the directory doesn't exist, move it over from the approot
                if (Directory.Exists(appRootDir))
                {
                    LoggingHelper.TraceInfo("Creating " + appDataPath);
                    try
                    {
                        Directory.CreateDirectory(appDataPath);
                        InitializeGrammarCopyFiles(appDataPath, fileName, appRootDir);
                    }
                    catch (Exception ex)
                    {
                        LoggingHelper.TraceError("Create Directory " + appDataPath + " failed: " + ex.Message);
                    }
                }
                else
                    LoggingHelper.TraceError("Directory " + appRootDir + " does not exist - cannot initialize grammar");
            }
            catch (FileNotFoundException)
            {
                LoggingHelper.TraceError("File " + grammarPath + " not found");
                InitializeGrammarCopyFiles(appDataPath, fileName, appRootDir);
            }
            catch (Exception ex)
            {
                LoggingHelper.TraceError("Cannot find grammars: " + ex.Message);
            }
        }

        private static void InitializeGrammarCopyFiles(string appDataPath, string fileName, string appRootDir)
        {
            // Log function entrance
            LoggingHelper.TraceFunction();

            if (File.Exists(Path.Combine(appRootDir, fileName)))
            {                
                LoggingHelper.TraceInfo("Copying " + appRootDir + " to " + appDataPath);
                try
                {
                    foreach (var file in Directory.EnumerateFiles(appRootDir))
                    {
                        string fname = Path.GetFileName(file);
                        LoggingHelper.TraceInfo("Copying " + Path.Combine(appRootDir, fname) + " to " + Path.Combine(appDataPath, fname));
                        File.Copy(Path.Combine(appRootDir, fname), Path.Combine(appDataPath, fname));
                    }
                }
                catch (Exception ex)
                {
                    LoggingHelper.TraceError("Copy file failed: " + ex.Message);
                }
            }
            else
                LoggingHelper.TraceError("File " + Path.Combine(appRootDir, fileName) + " does not exist - cannot initialize grammar");
        }

        void InitializeSpeechEngine(SpeechRecognitionEngine sre)
        {
            // Log function entrance
            LoggingHelper.TraceFunction();

            try
            {
                // initialize and cache format info
                formatInfo = new SpeechAudioFormatInfo(defaultSampleRate, defaultBitsPerSample, defaultAudioChannels);

                // initialize and cache speech engine
                sre.UpdateRecognizerSetting("AssumeCFGFromTrustedSource", 1);

                string fileName = @"TELLME-SMS-LM.cfgp";
                string appDataPath = HttpContext.Current.Server.MapPath("~/Content/grammars");
                string grammarPath = Path.Combine(appDataPath, fileName);
                LoggingHelper.TraceInfo("Grammar path: " + grammarPath);

                // make sure the grammar files are copied over from the approot directory to the appDataPath
                InitializeGrammar(grammarPath, appDataPath, fileName);

                // initialize and load the grammar
                Grammar grammar = new Grammar(grammarPath);
                grammar.Enabled = true;
                sre.LoadGrammar(grammar);
            }
            catch (Exception ex)
            {
                LoggingHelper.TraceError("Speech Engine initialization failed: " + ex.Message);            
            }
        }

        static void ReleaseSpeechEngine(SpeechRecognitionEngine sre)
        {
            // Log function entrance
            LoggingHelper.TraceFunction();

            // this code must be thread safe
            lock (sreLock)
            {
                int i;
                for (i = 0; i < sreArray.Length; i++)
                    if (sreArray[i] == sre)
                        break;

                // this cannot happen, but check anyway
                if (sreArray[i] != sre)
                    return;

                // reset the in use flag on this SRE instance
                sreInUseArray[i] = false;
            }
        }

        static void ResetSpeechEngine(object obj)
        {
            // Log function entrance
            LoggingHelper.TraceFunction();

            SpeechInfo si = (SpeechInfo)obj;
            SpeechRecognitionEngine sre = si.Engine;
            byte[] speechByteArray = si.SpeechByteArray;
            MemoryStream ms = new MemoryStream(speechByteArray);
            sre.SetInputToAudioStream(ms, formatInfo);

            // run the recognition again (which will take longer, but then reset the recognizer to 
            // a state where it runs quickly on the next invocation)
            sre.Recognize();
            ReleaseSpeechEngine(sre);
        }

        string WriteSpeechFile(User user, Stream stream)
        {
            // Log function entrance
            LoggingHelper.TraceFunction();

            try
            {
                //Directory.CreateDirectory(HttpContext.Current.Server.MapPath("~/files"));
                DateTime tod = DateTime.Now;
                string filename = String.Format("{0}{1}-{2}.wav",
                    HttpContext.Current.Server.MapPath("~/files/"),
                    user.Name,
                    tod.Ticks);
                FileStream fs = File.Create(filename);
                if (fs == null)
                    return "file not created";

                stream.CopyTo(fs);
                //byte[] bytes = new byte[32000];
                //int total = 0;
                //int count = stream.Read(bytes, 0, bytes.Length);
                //while (count > 0)
                //{
                //    total += count;
                //    fs.Write(bytes, 0, count);
                //    if (count < bytes.Length)
                //        break;
                //    count = stream.Read(bytes, 0, bytes.Length);
                //}

                // flush and close the file stream
                fs.Flush();
                fs.Close();

                // trace the size of the file
                LoggingHelper.TraceDetail(String.Format("Write speech file: {0} bytes", stream.Position));

                // reset the stream position
                stream.Position = 0;

                return null;
            }
            catch (Exception ex)
            {
                LoggingHelper.TraceError("Write speech file failed: " + ex.Message);
                return ex.Message;
            }
        }

        class SpeechInfo
        {
            public SpeechRecognitionEngine Engine { get; set; }
            public byte[] SpeechByteArray { get; set; }
        }

        #endregion
    }
}