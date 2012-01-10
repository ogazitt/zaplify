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
using System.IO.Compression;
using System.Text;

namespace TaskStoreWeb.Resources
{
    // singleton service, which manages thread-safety on its own
    [ServiceContract]
    public class TraceResource
    {
        public TraceResource()
        {
            // Log function entrance
            LoggingHelper.TraceFunction();
        }

        /// <summary>
        /// Store a client trace
        /// </summary>
        /// <returns>speech-to-text string</returns>
        [WebInvoke(UriTemplate = "", Method = "POST")]
        public HttpResponseMessageWrapper<string> Trace(HttpRequestMessage req)
        {
            // Log function entrance
            LoggingHelper.TraceFunction();

            // get the user credentials
            User user = ResourceHelper.GetUserPassFromMessage(req);

            try
            {
                Stream stream;
                if (req.Content.Headers.ContentType.MediaType == "application/x-gzip")
                    stream = new GZipStream(req.Content.ContentReadStream, CompressionMode.Decompress);
                else
                    stream = req.Content.ContentReadStream;

                string error = WriteFile(user, stream);
                var response = new HttpResponseMessageWrapper<string>(req, error != null ? error : "OK", HttpStatusCode.OK);
                response.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };

                // return the response
                return response;
            }
            catch (Exception ex)
            {
                // speech failed
                LoggingHelper.TraceError("Trace Write failed: " + ex.Message);
                return new HttpResponseMessageWrapper<string>(req, HttpStatusCode.InternalServerError);
            }
        }

        string WriteFile(User user, Stream traceStream)
        {
            // Log function entrance
            LoggingHelper.TraceFunction();

            try
            {
                string dir = HttpContext.Current.Server.MapPath(@"~/files");
                // if directory doesn't exist, create the directory
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                DateTime tod = DateTime.Now;
                string filename = String.Format("{0}-{1}.txt",
                    user.Name,
                    tod.Ticks);
                string path = Path.Combine(dir, filename);
                FileStream fs = File.Create(path);
                if (fs == null)
                    return "file not created";
                
                // copy the trace stream to the output file
                traceStream.CopyTo(fs);
                //fs.Write(bytes, 0, bytes.Length);
                fs.Flush();
                fs.Close();
                return null;
            }
            catch (Exception ex)
            {
                byte[] buffer = new byte[65536];
                int len = traceStream.Read(buffer, 0, buffer.Length);
                string s = Encoding.ASCII.GetString(buffer);
                LoggingHelper.TraceError("Write speech file failed: " + ex.Message);
                return ex.Message;
            }
        }
    }
}