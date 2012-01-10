using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Net.Http;
using Microsoft.ApplicationServer.Http;
using System.Net.Http.Headers;

namespace TaskStoreWeb.Helpers
{
    // wrapper around the type which contains the status code
    public class MessageWrapper<T>
    {
        public HttpStatusCode StatusCode { get; set; }

        public T Value { get; set; }
    }

    // custom HttpResponseMessage over a typed MessageWrapper
    public class HttpResponseMessageWrapper<T> : HttpResponseMessage<MessageWrapper<T>>
    {
        public HttpResponseMessageWrapper(HttpRequestMessage msg, HttpStatusCode statusCode)
            : base(statusCode)
        {
            MessageWrapper<T> messageWrapper = new MessageWrapper<T>() { StatusCode = statusCode };
            this.Content = new ObjectContent<MessageWrapper<T>>(messageWrapper);

            if (IsWinPhone7(msg))
            {
                this.StatusCode = HttpStatusCode.OK;

                // this constructor means no body, which indicates a non-200 series status code
                // since we switched the real HTTP status code to 200, we need to turn off caching 
                this.Headers.CacheControl = new CacheControlHeaderValue() { NoCache = true };
            }
        }

        public HttpResponseMessageWrapper(HttpRequestMessage msg, T type, HttpStatusCode statusCode)
            : base(statusCode)
        {
            MessageWrapper<T> messageWrapper = new MessageWrapper<T>() { StatusCode = statusCode, Value = type };
            this.Content = new ObjectContent<MessageWrapper<T>>(messageWrapper);

            if (IsWinPhone7(msg))
            {
                this.StatusCode = HttpStatusCode.OK;
            }
        }

        private static bool IsWinPhone7(HttpRequestMessage msg)
        {
            ProductInfoHeaderValue winPhone = null;

            try
            {
                if (msg.Headers.UserAgent != null)
                    winPhone = msg.Headers.UserAgent.FirstOrDefault(pi => pi.Product.Name == "TaskStore-WinPhone");
            }
            catch (Exception)
            {
                winPhone = null;
            }
            return winPhone != null ? true : false;
        }
    }
}