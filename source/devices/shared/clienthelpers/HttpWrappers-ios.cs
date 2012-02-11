using System;
using System.Net;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using BuiltSteady.Zaplify.Devices.ClientEntities;
using System.Runtime.Serialization.Json;

namespace BuiltSteady.Zaplify.Devices.ClientHelpers
{
    // some classes which assist in faking out an HttpWebResponse that can extract the StatusCode from the message body
    // this is to overcome limitations in the WP7 HttpWebResponse which can't handle StatusCodes outside of the 200 family
    [DataContract(Namespace="")]
    public class HttpMessageBodyWrapper<T>
    {
        // define one additional property - the status code from the message
        [DataMember]
        public HttpStatusCode StatusCode { get; set; }

        [DataMember]
        public T Value { get; set; }
    }

//    public class HttpWebResponseWrapper<T> : HttpWebResponse
    public class HttpWebResponseWrapper<T> 
    {
        public HttpWebResponseWrapper(HttpWebResponse resp)
        {
            // capture inner object
            innerResponse = resp;

            // deserialize the message body into the HttpMessageBodyWrapper clas
            DeserializeMessageBody();
        }

        public T GetBody()
        {
            return bodyWrapper.Value;
        }

        // status code extracted out of the message body
        private HttpMessageBodyWrapper<T> bodyWrapper;

        // inner object to delegate to
        private HttpWebResponse innerResponse;

        // delegate this property (and this property only) to the Wrapper implementation
        public HttpStatusCode StatusCode
        {
            get
            {
                return bodyWrapper.StatusCode;
            }
        }

        /// <summary>
        /// Common code to process a response body and deserialize the appropriate type
        /// </summary>
        /// <param name="resp">HTTP response</param>
        /// <param name="t">Type to deserialize</param>
        /// <returns>The deserialized object</returns>
        public static object DeserializeResponseBody(Stream stream, string contentType, Type t)
        {
            try
            {
                switch (contentType)
                {
                    case "application/json":
                        DataContractJsonSerializer dcjs = new DataContractJsonSerializer(t);
						bool debug = false;
						if (debug)
						{
							StreamReader sr = new StreamReader(stream);
							string str = sr.ReadToEnd ();
							TraceHelper.AddMessage("Debug path taken; message to deserialize: " + str);
							return null;
						}
						else
                        	return dcjs.ReadObject(stream);
                    case "text/xml":
                    case "application/xml":
                        DataContractSerializer dc = new DataContractSerializer(t);
                        return dc.ReadObject(stream);
                    default:  // unknown format (some debugging code below)
                        StreamReader sr = new StreamReader(stream);
                        string str = sr.ReadToEnd();
						TraceHelper.AddMessage(String.Format("Unrecognized Content-Type: {0}; message: {1}", contentType, str));
                        return null;
                }
            }
            catch (Exception ex)
            {
                TraceHelper.AddMessage("Exception in DeserializeResponseBody: " + ex.Message);
#if !IOS		// MonoTouch does not support resetting the response stream
                stream.Position = 0;
                StreamReader sr = new StreamReader(stream);
                string str = sr.ReadToEnd();
#endif
                return null;
            }
        }

        // deserialize the status code out of the message body, and reset the stream
        private void DeserializeMessageBody()
        {
            // get the status code out of the response
            bodyWrapper = (HttpMessageBodyWrapper<T>) DeserializeResponseBody(innerResponse, typeof(HttpMessageBodyWrapper<T>));
        }

        /// <summary>
        /// Common code to process a response body and deserialize the appropriate type
        /// </summary>
        /// <param name="resp">HTTP response</param>
        /// <param name="t">Type to deserialize</param>
        /// <returns>The deserialized object</returns>
        private object DeserializeResponseBody(HttpWebResponse resp, Type t)
        {
            if (resp == null || resp.ContentType == null)
                return null;

            // get the first component of the content-type header
            // string contentType = resp.Headers["Content-Type"].Split(';')[0];
            string contentType = resp.ContentType.Split(';')[0];
			return DeserializeResponseBody(resp.GetResponseStream(), contentType, t);
        }

		/*
        // delegate all other overridable public methods or properties to the inner object
        public override void Close()
        {
            innerResponse.Close();
        }

        public override long ContentLength
        {
            get
            {
                return innerResponse.ContentLength;
            }
        }

        public override string ContentType
        {
            get
            {
                return innerResponse.ContentType;
            }
        }

        public override bool Equals(object obj)
        {
            return innerResponse.Equals(obj);
        }

        public override int GetHashCode()
        {
            return innerResponse.GetHashCode();
        }
		 
        public override Stream GetResponseStream()
        {
            return innerResponse.GetResponseStream();
        }

        public override WebHeaderCollection Headers
        {
            get
            {
                return innerResponse.Headers;
            }
        }

        public override Uri ResponseUri
        {
            get
            {
                return innerResponse.ResponseUri;
            }
        }

        public override string ToString()
        {
            return innerResponse.ToString();
        }
        */
    }
}