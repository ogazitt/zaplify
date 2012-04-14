namespace BuiltSteady.Zaplify.Website.Resources
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.Text;
    using System.Web;

    using BuiltSteady.Zaplify.Website.Helpers;
    using BuiltSteady.Zaplify.Website.Models;
    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.ServiceHost;
    using System.Runtime.Serialization.Json;

    // singleton service, which manages thread-safety on its own
    [ServiceContract]
    public class TraceResource : BaseResource
    {
        public TraceResource()
        {
            // Log function entrance
            TraceLog.TraceFunction();
        }

        [WebInvoke(UriTemplate = "", Method = "POST")]
        public HttpResponseMessageWrapper<string> Trace(HttpRequestMessage req)
        {
            // Log function entrance
            TraceLog.TraceFunction();

            // get the username from the message if available 
            // the creds may not exist for a device that hasn't registered but is uploading a crash report
            string username = null;
            var creds = GetUserFromMessageHeaders(req);
            if (creds != null)
                username = creds.Name;

            try
            {
                Stream stream;
                switch (req.Content.Headers.ContentType.MediaType)
                {
                    case "application/x-gzip":
                        stream = new GZipStream(req.Content.ReadAsStreamAsync().Result, CompressionMode.Decompress);
                        break;
                    case "application/json":
                        stream = req.Content.ReadAsStreamAsync().Result;
                        DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(string));
                        string data = (string) ser.ReadObject(stream);
                        var ms = new MemoryStream();
                        var tw = new StreamWriter(ms);
                        tw.Write(data);
                        tw.Flush();
                        ms.Position = 0;
                        stream = ms;
                        break;
                    default:
                        stream = req.Content.ReadAsStreamAsync().Result;
                        break;
                }

                string error = WriteFile(username, stream);
                var response = new HttpResponseMessageWrapper<string>(req, error != null ? error : "OK", HttpStatusCode.OK);
                response.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };

                // return the response
                return response;
            }
            catch (Exception ex)
            {
                TraceLog.TraceException("Trace Write failed", ex);
                return new HttpResponseMessageWrapper<string>(req, HttpStatusCode.InternalServerError);
            }
        }

        string WriteFile(string username, Stream traceStream)
        {
            // Log function entrance
            TraceLog.TraceFunction();

            if (username == null)
                username = "anonymous";

            try
            {
                string dir = HttpContext.Current.Server.MapPath(@"~/files");
                // if directory doesn't exist, create the directory
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    TraceLog.TraceInfo("Created directory " + dir);
                }

                DateTime tod = DateTime.Now;
                string filename = String.Format("{0}-{1}.txt",
                    username,
                    tod.Ticks);
                string path = Path.Combine(dir, filename);
                FileStream fs = File.Create(path);
                if (fs == null)
                {
                    string error = "Error creating trace file " + path;
                    TraceLog.TraceError(error);
                    return "error";
                }
                TraceLog.TraceInfo("Created file " + path);
                
                // copy the trace stream to the output file
                traceStream.CopyTo(fs);
                fs.Flush();
                fs.Close();
                return null;
            }
            catch (Exception ex)
            {
                byte[] buffer = new byte[65536];
                int len = traceStream.Read(buffer, 0, buffer.Length);
                string s = Encoding.UTF8.GetString(buffer);
                TraceLog.TraceException("Writing trace file failed", ex);
                return ex.Message;
            }
        }
    }
}