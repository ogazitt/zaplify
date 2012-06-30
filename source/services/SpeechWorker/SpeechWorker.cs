using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Web.Http;
using System.Web.Http.SelfHost;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using NSpeex;
using BuiltSteady.Zaplify.ServiceHost;
using System.Web;

namespace BuiltSteady.Zaplify.SpeechWorker
{
    public class SpeechWorker : IWorker
    {
        private int? timeout;
        public int Timeout
        {
            get
            {
                if (!timeout.HasValue)
                {
                    timeout = ConfigurationSettings.GetAsNullableInt(HostEnvironment.SpeechWorkerTimeoutConfigKey);
                    if (timeout == null)
                        timeout = 500000;  // default to 500 seconds
                    else
                        timeout *= 1000;  // convert to ms
                }
                return timeout.Value;
            }
        }

        private static int engines = 1;  // number of speech recognition engines to cache
        private static SpeechRecognitionEngine[] sreArray = new SpeechRecognitionEngine[engines];
        private static bool[] sreInUseArray = new bool[engines];
        private static object sreLock = new Object();

        public void Start()
        {
            engines = GetEngineCount();
            TraceLog.TraceDetail(String.Format("Starting speech service - warming up {0} speech engine instances", engines)); 

            // load the speech recognition engines into memory
            var sres = new SpeechRecognitionEngine[engines];
            for (int i = 0; i < engines; i++)
                sres[i] = GetSpeechEngine();

            // mark the engine instances as "released" so that they can be used by a caller
            for (int i = 0; i < engines; i++)
                ReleaseSpeechEngine(sres[i]);

            // set up a WebApi route 
            var baseAddress = HostEnvironment.SpeechEndpoint;
            TraceLog.TraceDetail("Hosting speech endpoint at " + baseAddress);
            var maxsize = 1024 * 1024;  // 1MB = 32sec of speech
            var config = new HttpSelfHostConfiguration(baseAddress) { MaxBufferSize = maxsize, MaxReceivedMessageSize = maxsize };
#if DEBUG   // include errors if in debug mode
            config.IncludeErrorDetailPolicy = IncludeErrorDetailPolicy.Always; 
#endif
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Create and open the server
            var server = new HttpSelfHostServer(config);
            server.OpenAsync().Wait();
            
            // keep the worker thread alive
            while (true)
                Thread.Sleep(Timeout);
        }

        public static SpeechRecognitionEngine GetSpeechEngine()
        {
            // Log function entrance
            TraceLog.TraceFunction();

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
                TraceLog.TraceLine(String.Format("Using SpeechEngine[{0}]", i), TraceLog.LogLevel.Detail);

                // return speech engine
                return sreArray[i];
            }
        }

        public static void ReleaseSpeechEngine(SpeechRecognitionEngine sre)
        {
            // Log function entrance
            TraceLog.TraceFunction();

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

        #region Helpers

        static void InitializeSpeechEngine(SpeechRecognitionEngine sre)
        {
            // Log function entrance
            TraceLog.TraceFunction();

            try
            {
                // initialize and cache speech engine
                sre.UpdateRecognizerSetting("AssumeCFGFromTrustedSource", 1);

                string fileName = @"TELLME-SMS-LM.cfgp";
                string appDataPath = Path.Combine(Directory.GetCurrentDirectory(), @"grammars");
                string grammarPath = Path.Combine(appDataPath, fileName);
                TraceLog.TraceInfo("Grammar path: " + grammarPath);

                // make sure the grammar files are copied over from the approot directory to the appDataPath
                //InitializeGrammar(grammarPath, appDataPath, fileName);

                // initialize and load the grammar
                Grammar grammar = new Grammar(grammarPath);
                grammar.Enabled = true;
                sre.LoadGrammar(grammar);
            }
            catch (Exception ex)
            {
                TraceLog.TraceError("Speech Engine initialization failed: " + ex.Message);
            }
        }

        static int GetEngineCount()
        {
            switch (HostEnvironment.AzureRoleSize)
            {
                case HostEnvironment.RoleSize.ExtraSmall:
                case HostEnvironment.RoleSize.Small:
                case HostEnvironment.RoleSize.Unknown:
                default:
                    return 1;
                case HostEnvironment.RoleSize.Medium:
                    return 2;
                case HostEnvironment.RoleSize.Large:
                case HostEnvironment.RoleSize.ExtraLarge:
                    return 3;
            }
        }

        #endregion Helpers
    }

    public class SpeechController : BaseResource
    {
        // some default values for speech
        const int defaultSampleRate = 16000;
        const AudioBitsPerSample defaultBitsPerSample = AudioBitsPerSample.Sixteen;
        const AudioChannel defaultAudioChannels = AudioChannel.Mono;
        
        /// <summary>
        /// Convert the byte array representing the speech wav format to a text string
        /// </summary>
        /// <returns>speech-to-text string</returns>        
        public HttpResponseMessage Post(HttpRequestMessage req)
        {
            TraceLog.TraceFunction();
            /*
            HttpStatusCode code = AuthenticateUser(req);
            if (code != HttpStatusCode.OK)
            {   // user not authenticated
                return new HttpResponseMessageWrapper<string>(req, code);
            }
             * */

            // get a free instance of the speech recognition engine
            SpeechRecognitionEngine sre = null;
            while (sre == null)
            {
                sre = SpeechWorker.GetSpeechEngine();

                // if all SRE's are in use, wait one second and then try again
                if (sre == null)
                    Thread.Sleep(1000);
            }

            // initialize format info to default values
            var formatInfo = new SpeechAudioFormatInfo(defaultSampleRate, defaultBitsPerSample, defaultAudioChannels);

            try
            {
                // retrieve and set the stream to recognize
                Stream stream = req.Content.ReadAsStreamAsync().Result;
                IEnumerable<string> values = new List<string>();
                if (req.Headers.Contains("Zaplify-Speech-Encoding") == true)
                    stream = GetStream(req, ref formatInfo);
                sre.SetInputToAudioStream(stream, formatInfo);

#if WRITEFILE || DEBUG
                string msg = WriteSpeechFile(/*CurrentUser.Name*/ "anonymous", stream);
                if (msg != null)
                    return CreateResponse<string>(req, msg, HttpStatusCode.OK);
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
                TraceLog.TraceDetail(String.Format("Recognized '{0}' in{1}", responseString, timing));

                // construct the response
                responseString += timing;
                var response = CreateResponse<string>(req, responseString, HttpStatusCode.OK);
                response.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };

                // release engine instance
                SpeechWorker.ReleaseSpeechEngine(sre);

                // return the response
                return response;
            }
            catch (Exception ex)
            {
                // speech failed
                TraceLog.TraceException("Speech recognition failed", ex);

                // release engine instance
                SpeechWorker.ReleaseSpeechEngine(sre);

                return CreateResponse<string>(req, HttpStatusCode.InternalServerError);
            }
        }

        #region Helpers

        private Stream DecodeSpeexStream(Stream stream)
        {
            // Log function entrance
            TraceLog.TraceFunction();

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
                        TraceLog.TraceError(String.Format("Corrupted speex stream: len {0}, count {1}", len, count));
                        return ms;
                    }

                    // get the size of the buffer that the encoder used
                    // we need that exact size in order to properly decode
                    // the size is the first four bytes of the speexBuffer
                    int inDataSize = BitConverter.ToInt32(speexBuffer, 0);

                    // decode the chunk (starting at an offset of sizeof(int))
                    short[] decodedFrame = new short[inDataSize];
                    var speexDecoder = new SpeexDecoder(BandMode.Wide);
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
                TraceLog.TraceDetail(String.Format("Decoded {0} bytes into {1} bytes", totalEncoded, totalDecoded));

                // reset and return the new memory stream
                ms.Position = 0;
                return ms;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Corrupted speex stream", ex);
                return null;
            }
        }

        private Stream GetStream(HttpRequestMessage req, ref SpeechAudioFormatInfo formatInfo)
        {
            Stream stream = req.Content.ReadAsStreamAsync().Result;
            string contentType = null;

            // get the content type
            IEnumerable<string> values = new List<string>();
            if (req.Headers.TryGetValues("Zaplify-Speech-Encoding", out values) == true)
                contentType = values.ToArray<string>()[0];
            else
                return stream;

            // format for contentType string is: 
            //   application/<encoding>-<samplerate>-<bits/channel>-<audiochannels>
            string[] encoding = contentType.Split('-');
            string encodingType = encoding[0];
            int sampleRate = encoding.Length > 1 ? Convert.ToInt32(encoding[1]) : defaultSampleRate;
            int bitsPerSample = encoding.Length > 2 ? Convert.ToInt32(encoding[2]) : (int)defaultBitsPerSample;
            int audioChannels = encoding.Length > 3 ? Convert.ToInt32(encoding[3]) : (int)defaultAudioChannels;

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

        private string WriteSpeechFile(string username, Stream stream)
        {
            // Log function entrance
            TraceLog.TraceFunction();

            try
            {
                // create the directory for the wav files if it doesn't exist yet
                string wavDirectory = Path.Combine(Directory.GetCurrentDirectory(), @"wavfiles");
                if (!Directory.Exists(wavDirectory))
                    Directory.CreateDirectory(wavDirectory);
                
                // create files
                DateTime tod = DateTime.Now;
                string filename = Path.Combine(
                    wavDirectory, 
                    String.Format("{0}-{1}.wav",
                        username,
                        tod.Ticks));

                using (FileStream fs = File.Create(filename))
                {
                    stream.CopyTo(fs);
                }

                // trace the size of the file
                TraceLog.TraceDetail(String.Format("Write speech file: {0} bytes", stream.Position));

                // reset the stream position
                stream.Position = 0;

                return null;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Write speech file failed", ex);
                return ex.Message;
            }
        }

        #endregion Helpers
    }
}
