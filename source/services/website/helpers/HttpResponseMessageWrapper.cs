namespace BuiltSteady.Zaplify.Website.Helpers
{ 
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;

    using BuiltSteady.Zaplify.ServiceHost;
    using BuiltSteady.Zaplify.Shared.Entities;

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

            if (HttpHeaderHelper.IsPhoneDevice(msg))
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
            if (HttpHeaderHelper.IsPhoneDevice(msg))
            {
                this.StatusCode = HttpStatusCode.OK;
            }

            string messageText = this.Content != null ? msg.Content.ReadAsStringAsync().Result : "(empty)";
            string tracemsg = String.Format(
                "\nWeb Response: Status: {0} {1}; Content-Type: {2}\n" +
                "Web Response Body: {3}",
                (int) statusCode,
                statusCode,
                msg.Content.Headers.ContentType,
                messageText);

            TraceLog.TraceLine(tracemsg, TraceLog.LogLevel.Detail);
        }

    }

    public static class HttpHeaderHelper
    {
        public static bool IsPhoneDevice(HttpRequestMessage msg = null)
        {
            bool isPhone = false;
            if (msg == null && System.Web.HttpContext.Current != null)
            {   // check UserAgent in HttpContext.Current
                string userAgent = System.Web.HttpContext.Current.Request.UserAgent;
                if (userAgent != null)
                {
                    isPhone = (userAgent.Contains(UserAgents.WinPhone) ||
                            userAgent.Contains(UserAgents.IOSPhone));
                }
            }
            else
            {
                try
                {
                    var phoneAgent = msg.Headers.UserAgent.FirstOrDefault(pi => pi.Product != null &&
                            (pi.Product.Name == UserAgents.WinPhone || pi.Product.Name == UserAgents.IOSPhone));
                    isPhone = (phoneAgent != null);
                }
                catch (Exception)
                {
                    isPhone = false;
                }
            }
            return isPhone;
        }
    }

}