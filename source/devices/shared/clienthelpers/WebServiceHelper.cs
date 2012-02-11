namespace BuiltSteady.Zaplify.Devices.ClientHelpers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.IO.IsolatedStorage;
    using System.Net;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;

    using SharpCompress.Compressor;
    using SharpCompress.Compressor.Deflate;
    using SharpCompress.Writer.GZip;
    using BuiltSteady.Zaplify.Devices.ClientEntities;

    public enum OperationStatus
    {
        Started = 0,
        Success = 1,
        Failed = 2,
        Retry = 3
    }

    public class WebServiceHelper
    {
        const string authResponseHeader = "Set-Cookie";
        const string authRequestHeader = "Cookie";
        static string authCookie = null;
        static HttpWebRequest request = null;
        static bool isRequestInProgress = false;        // only one network operation at a time

        public static string BaseUrl { get { return baseUrl; } }
        private static string baseUrl
        {
            get
            {
#if IOS
                return "http://api.zaplify.com";
#else
                return (Microsoft.Devices.Environment.DeviceType == Microsoft.Devices.DeviceType.Emulator) ? "http://localhost:8888" : "http://api.zaplify.com";
#endif
            }
        }

#region // Web Service calls

        public static void CreateTag(User user, Tag tag, Delegate del, Delegate netOpInProgressDel)
        {
            InvokeWebServiceRequest(user, baseUrl + "/tags", "POST", tag, del, netOpInProgressDel, new AsyncCallback(ProcessResponse<Tag>));
        }

        public static void CreateItem(User user, Item item, Delegate del, Delegate netOpInProgressDel)
        {
            InvokeWebServiceRequest(user, baseUrl + "/items", "POST", item, del, netOpInProgressDel, new AsyncCallback(ProcessResponse<Item>));
        }

        public static void CreateFolder(User user, Folder folder, Delegate del, Delegate netOpInProgressDel)
        {
            InvokeWebServiceRequest(user, baseUrl + "/folders", "POST", folder, del, netOpInProgressDel, new AsyncCallback(ProcessResponse<Folder>));
        }

        public static void CreateUser(User user, Delegate del, Delegate netOpInProgressDel)
        {
            InvokeWebServiceRequest(user, baseUrl + "/users", "POST", user, del, netOpInProgressDel, new AsyncCallback(ProcessUser));
        }

        public static void DeleteTag(User user, Tag tag, Delegate del, Delegate netOpInProgressDel)
        {
            InvokeWebServiceRequest(user, baseUrl + "/tags/" + tag.ID, "DELETE", tag, del, netOpInProgressDel, new AsyncCallback(ProcessResponse<Tag>));
        }

        public static void DeleteItem(User user, Item item, Delegate del, Delegate netOpInProgressDel)
        {
            InvokeWebServiceRequest(user, baseUrl + "/items/" + item.ID, "DELETE", item, del, netOpInProgressDel, new AsyncCallback(ProcessResponse<Item>));
        }

        public static void DeleteFolder(User user, Folder folder, Delegate del, Delegate netOpInProgressDel)
        {
            InvokeWebServiceRequest(user, baseUrl + "/folders/" + folder.ID, "DELETE", folder, del, netOpInProgressDel, new AsyncCallback(ProcessResponse<Folder>));
        }

        public static void GetConstants(User user, Delegate del, Delegate netOpInProgressDel)
        {
            InvokeWebServiceRequest(user, String.Format("{0}/constants", baseUrl), "GET", null, del, netOpInProgressDel, ProcessResponse<Constants>);
        }

        public static void GetUser(User user, Delegate del, Delegate netOpInProgressDel)
        {
            InvokeWebServiceRequest(user, baseUrl + "/users", "GET", null, del, netOpInProgressDel, new AsyncCallback(ProcessResponse<User>));
        }

        public static void SendTrace(User user, byte[] bytes, Delegate del, Delegate netOpInProgressDel)
        {
            InvokeWebServiceRequest(user, baseUrl + "/trace", "POST", bytes, del, netOpInProgressDel, new AsyncCallback(ProcessResponse<string>));
        }

        public static void SpeechToText(User user, byte[] bytes, Delegate del, Delegate netOpInProgressDel)
        {
            InvokeWebServiceRequest(user, baseUrl + "/speech", "POST", bytes, del, netOpInProgressDel, new AsyncCallback(ProcessResponse<string>));
        }

        public static void SpeechToTextStream(User user, Delegate streamDel, Delegate del, Delegate netOpInProgressDel)
        {
            InvokeWebServiceRequest(user, baseUrl + "/speech", "POST", streamDel, del, netOpInProgressDel, new AsyncCallback(ProcessResponse<string>));
        }

        public static void UpdateTag(User user, List<Tag> originalAndNewTags, Delegate del, Delegate netOpInProgressDel)
        {
            if (originalAndNewTags == null || originalAndNewTags.Count != 2)
                return;
            InvokeWebServiceRequest(user, baseUrl + "/tags/" + originalAndNewTags[0].ID, "PUT", originalAndNewTags, del, netOpInProgressDel, new AsyncCallback(ProcessResponse<Tag>));
        }

        public static void UpdateItem(User user, List<Item> originalAndNewItems, Delegate del, Delegate netOpInProgressDel)
        {
            if (originalAndNewItems == null || originalAndNewItems.Count != 2)
                return;
            InvokeWebServiceRequest(user, baseUrl + "/items/" + originalAndNewItems[0].ID, "PUT", originalAndNewItems, del, netOpInProgressDel, new AsyncCallback(ProcessResponse<Item>));
        }

        public static void UpdateFolder(User user, List<Folder> originalAndNewFolders, Delegate del, Delegate netOpInProgressDel)
        {
            if (originalAndNewFolders == null || originalAndNewFolders.Count != 2)
                return;
            InvokeWebServiceRequest(user, baseUrl + "/folders/" + originalAndNewFolders[0].ID, "PUT", originalAndNewFolders, del, netOpInProgressDel, new AsyncCallback(ProcessResponse<Folder>));
        }
        
        public static void VerifyUserCredentials(User user, Delegate del, Delegate netOpInProgressDel)
        {
            InvokeWebServiceRequest(user, baseUrl + "/users", "GET", null, del, netOpInProgressDel, new AsyncCallback(ProcessUser));
        }

#endregion

#region // Helper methods

        private static HttpWebResponseWrapper<T> GetWebResponse<T>(IAsyncResult result)
        {
            HttpWebResponse resp = null;

            // get response and mark request as not in progress
            try
            {
                resp = (HttpWebResponse)request.EndGetResponse(result);
                isRequestInProgress = false;
                if (resp == null)
                    return null;
            }
            catch (Exception ex)
            {
                // trace the exception
                TraceHelper.AddMessage("GetWebResponse: ex: " + ex.Message);

                // communication exception
                isRequestInProgress = false;
                return null;
            }

            // put auth cookie in static memory
            if (resp.Headers[authResponseHeader] != null)
            {
                authCookie = resp.Headers[authResponseHeader];
            }

            // create and initialize a new response wrapper
            HttpWebResponseWrapper<T> wrapper = new HttpWebResponseWrapper<T>(resp);

            return wrapper;
        }


        // Common code for invoking all the web service calls.  
        // GET requests will be served directly from this method,
        // POST/PUT/DELETE requests are served from the InvokeWebServiceRequest_Inner method (which is an async callback)
        private static void InvokeWebServiceRequest(User user, string url, string verb, object obj, Delegate del, Delegate netOpInProgressDel, AsyncCallback callback)
        {
            // this code is non-reentrant
            if (isRequestInProgress == true)
                return;

            // signal that a network operation is starting
            if (netOpInProgressDel != null)
                netOpInProgressDel.DynamicInvoke(true, OperationStatus.Started);

#if IOS
			request = (HttpWebRequest) WebRequest.Create(url);
#else
			request = WebRequest.CreateHttp(url);
#endif
            request.Accept = "application/json";
            request.UserAgent = "Zaplify-WinPhone";
            request.Method = verb == null ? "GET" : verb;

            if (authCookie != null)
            {
                request.Headers[authRequestHeader] = authCookie;
            }
            else if (user != null)
            {
                request.Headers["Zaplify-Username"] = user.Name;
                request.Headers["Zaplify-Password"] = user.Password;
            }

            // if this is a GET request, we can execute from here
            if (request.Method == "GET")
            {
                // execute the web request and get the response
                try
                {
                    WebServiceState reqState = new WebServiceState() 
                    { 
                        Delegate = del, 
                        NetworkOperationInProgressDelegate = netOpInProgressDel
                    };
                    IAsyncResult result = request.BeginGetResponse(callback, reqState);
                    if (result != null)
                        isRequestInProgress = true;
                }
                catch (Exception)
                {
                    isRequestInProgress = false;

                    // signal that a network operation is done and unsuccessful
                    if (netOpInProgressDel != null)
                    {
                        netOpInProgressDel.DynamicInvoke(false, OperationStatus.Failed);
                    }
                }
            }
            else
            {
                // this is a request that contains a body (PUT, POST, DELETE)
                // need to nest another async call - this time to get the request stream
                try
                {
                    IAsyncResult result = request.BeginGetRequestStream(
                        new AsyncCallback(InvokeWebServiceRequest_Inner),
                        new WebInvokeServiceState()
                        {
                            Callback = callback,
                            Delegate = del,
                            NetworkOperationInProgressDelegate = netOpInProgressDel,
                            RequestBody = obj
                        });
                   if (result != null)
                        isRequestInProgress = true;
                }
                catch (Exception)
                {
                    isRequestInProgress = false;
                    // signal that a network operation is done and unsuccessful
                    if (netOpInProgressDel != null)
                        netOpInProgressDel.DynamicInvoke(false, OperationStatus.Failed);
                }
            }
        }

        private static void InvokeWebServiceRequest_Inner(IAsyncResult res)
        {
            WebInvokeServiceState state = res.AsyncState as WebInvokeServiceState;
            if (state == null)
                throw new Exception("Web Service State not found");

            Stream stream = request.EndGetRequestStream(res);

            // serialize a request body if one was passed in (and the verb will take it)
            if (state.RequestBody != null && request.Method != "GET")
            {
                request.UserAgent = "Zaplify-WinPhone";

                // a null request body means that the caller wants to get the stream back and write to it directly
                if (state.RequestBody as Delegate != null)
                {
                    Delegate streamDel = (Delegate)state.RequestBody;
                    // invoke the delegate passed in with the request stream, so that the external caller
                    // can push data into the stream as it becomes available
                    streamDel.DynamicInvoke(stream);
                }
                else
                {
                    // the caller passed the complete object - so just serialize it onto the stream
                    if (state.RequestBody.GetType() == typeof(byte[]))
                    {
                        byte[] bytes = (byte[])state.RequestBody;
                        stream = new GZipStream(stream, CompressionMode.Compress);
                        request.ContentType = "application/x-gzip";
                        stream.Write(bytes, 0, bytes.Length);
                    }
                    else
                    {
                        request.ContentType = "application/json";
                        DataContractJsonSerializer ser = new DataContractJsonSerializer(state.RequestBody.GetType());
                        ser.WriteObject(stream, state.RequestBody);
                    }
                    // we got all the data into the stream, so flush/close it
                    stream.Flush();
                    stream.Close();
                }
            }
            
            // complete the invocation (this is not done inline because the InvokeWebServiceRequest_Inner_Complete() method
            // is reused by external callers that want to write to a stream directly and then invoke the operation)
            InvokeWebServiceRequest_Invoke(state.Delegate, state.NetworkOperationInProgressDelegate, state.Callback);
        }

        private static void InvokeWebServiceRequest_Invoke(Delegate del, Delegate netOpInProgressDel, AsyncCallback callback)
        {
            // execute the web request and get the response
            try
            {
                WebServiceState reqState = new WebServiceState()
                {
                    Delegate = del,
                    NetworkOperationInProgressDelegate = netOpInProgressDel
                };
                IAsyncResult result = request.BeginGetResponse(callback, reqState);
                if (result != null)
                    isRequestInProgress = true;
            }
            catch (Exception)
            {
                isRequestInProgress = false;

                // signal the operation is done and unsuccessful
                if (netOpInProgressDel != null)
                    netOpInProgressDel.DynamicInvoke(false, OperationStatus.Failed);
            }
        }


        // Common code to process the response from any web service call.  This is invoked from the callback 
        // method for the web service, and passed a Type for deserializing the response body. 
        // This method will also invoke the delegate with the result of the Web Service call
        private static void ProcessResponse<T>(IAsyncResult result)
        {
            WebServiceState state = result.AsyncState as WebServiceState;
            if (state == null)
                return;

            // get the network operation status delegate
            Delegate netOpInProgressDel = state.NetworkOperationInProgressDelegate as Delegate;

            // get the web response and make sure it's not null (failed)
            HttpWebResponseWrapper<T> resp = GetWebResponse<T>(result);
            if (resp == null)
            {
                // signal that the network operation completed unsuccessfully
                if (netOpInProgressDel != null)
                {
                    netOpInProgressDel.DynamicInvoke(false, OperationStatus.Failed);
                }
                return;
            }
            else
            {
                OperationStatus status = AsOperationStatus(resp.StatusCode);
                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {   // using this status code to indicate cookie has expired or is invalid
                    if (authCookie != null)
                    {   // remove cookie and retry with credentials 
                        status = OperationStatus.Retry;
                        authCookie = null;
                    }
                }
                if (resp.StatusCode == HttpStatusCode.Forbidden)
                {   // remove cookie and force authentication on next request
                    authCookie = null;
                }                
                
                if (netOpInProgressDel != null)
                {   // signal the network operation completed and whether it completed successfully
                    netOpInProgressDel.DynamicInvoke(false, status);
                    if (status == OperationStatus.Retry)
                    {   // delegate will retry, exit now
                        return;
                    }
                }
            }

            // get the method-specific delegate
            Delegate del = state.Delegate as Delegate;
            if (del == null)
                return;  // if no delegate was passed, the results can't be processed

            // invoke the delegate with the response body
            try
            {
                T resultObject = resp.GetBody();
                del.DynamicInvoke(resultObject);
            }
            catch (Exception)
            {
                del.DynamicInvoke(null);
            }
        }

        private static void ProcessUser(IAsyncResult result)
        {
            WebServiceState state = result.AsyncState as WebServiceState;
            if (state == null)
                return;

            // get the network operation status delegate
            Delegate netOpInProgressDel = state.NetworkOperationInProgressDelegate as Delegate;

            // get the web response
            HttpWebResponseWrapper<User> resp = GetWebResponse<User>(result);
            if (resp == null)
            {
                // signal that the network operation completed unsuccessfully
                if (netOpInProgressDel != null)
                {
                    netOpInProgressDel.DynamicInvoke(false, OperationStatus.Failed);
                }
                return;
            }
            else
            {
                if (netOpInProgressDel != null)
                {   // signal that the network operation completed and whether it completed successfully
                    netOpInProgressDel.DynamicInvoke(false, AsOperationStatus(resp.StatusCode));
                }
            }

            // get the method-specific delegate
            Delegate del = state.Delegate as Delegate;
            if (del == null)
                return;  // if no delegate was passed, the results can't be processed

            // invoke the operation-specific delegate
            if (resp == null)
            {
                del.DynamicInvoke(null, null);
            }
            else
                del.DynamicInvoke(resp.GetBody(), resp.StatusCode);
        }

        private static OperationStatus AsOperationStatus(HttpStatusCode statusCode)
        {
            if (statusCode == HttpStatusCode.OK || statusCode == HttpStatusCode.Created || statusCode == HttpStatusCode.Accepted)
            {
                return OperationStatus.Success;
            }
            return OperationStatus.Failed;
        }

        private class WebInvokeServiceState
        {
            public AsyncCallback Callback { get; set; }  // callback for the GetResponse
            public Delegate Delegate { get; set; }  // delegate passed in by the caller
            public Delegate NetworkOperationInProgressDelegate { get; set; }  // delegate passed in by the caller
            public object RequestBody { get; set; }  // object to serialize on the request
        }

        private class WebServiceState
        {
            public Delegate Delegate { get; set; }  // delegate passed in by the caller
            public Delegate NetworkOperationInProgressDelegate { get; set; }  // delegate passed in by the caller
        }

#endregion
    }
}