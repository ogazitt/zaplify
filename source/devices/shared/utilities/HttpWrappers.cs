using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;
using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using TaskStoreClientEntities;
using System.Runtime.Serialization.Json;
using System.Net.Browser;

namespace TaskStoreWinPhoneUtilities
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

    public class HttpWebResponseWrapper<T> : HttpWebResponse
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
        public override HttpStatusCode StatusCode
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
                        return dcjs.ReadObject(stream);
                    case "text/xml":
                    case "application/xml":
                        DataContractSerializer dc = new DataContractSerializer(t);
                        return dc.ReadObject(stream);
                    default:  // unknown format (some debugging code below)
                        StreamReader sr = new StreamReader(stream);
                        string str = sr.ReadToEnd();
                        return null;
                }
            }
            catch
            {
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

        public override CookieCollection Cookies
        {
            get
            {
                return innerResponse.Cookies;
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

        public override string Method
        {
            get
            {
                return innerResponse.Method;
            }
        }

        public override Uri ResponseUri
        {
            get
            {
                return innerResponse.ResponseUri;
            }
        }

        public override string StatusDescription
        {
            get
            {
                return innerResponse.StatusDescription;
            }
        }

        public override bool SupportsHeaders
        {
            get
            {
                return innerResponse.SupportsHeaders;
            }
        }

        public override string ToString()
        {
            return innerResponse.ToString();
        }
    }
}