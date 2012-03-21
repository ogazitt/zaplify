using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json.Linq;

namespace BuiltSteady.Zaplify.ServiceUtilities.FBGraph
{
    public sealed class FBGraphAPI
    {
        #region Constants

        private const string InvalidAsyncResult = "The IAsyncResult instance is invalid in the current context.";

        private const string EndpointBaseUri = "https://graph.facebook.com/";

        #endregion Constants

        #region Properties

        public string FacebookID { get; set; }
        public string AccessToken { get; set; }

        #endregion Properties

        #region Constructor

        public FBGraphAPI()
        {
        }

        #endregion Constructor

        #region Query methods

        public IEnumerable<FBQueryResult> Query(string entity)
        {
            return Query(entity, null);
        }

        public IEnumerable<FBQueryResult> Query(string entity, string query)
        {
            IAsyncResult result = BeginQuery(entity, query, null, null);
            return EndQuery(result);
        }

        public IAsyncResult BeginQuery(string entity, AsyncCallback callback, object state)
        {
            return BeginQuery(entity, null, null);
        }

        public IAsyncResult BeginQuery(string entity, string query, AsyncCallback callback, object state)
        {
            string uri = ConstructQueryUri(entity, query);
            WebRequest req = WebRequest.Create(uri);

            FBResult fbResult = new FBResult(state);
            fbResult.InnerResult = req.BeginGetResponse(
                (result) =>
                {
                    if (callback != null)
                        callback(fbResult);
                },
                req);

            return fbResult;
        }

        public IEnumerable<FBQueryResult> EndQuery(IAsyncResult result)
        {
            if (!result.IsCompleted)
                result.AsyncWaitHandle.WaitOne();

            FBResult fbResult = result as FBResult;
            if (fbResult == null)
                throw new ArgumentException(InvalidAsyncResult, "result");

            WebRequest req = fbResult.InnerResult.AsyncState as WebRequest;
            if (req == null)
                throw new ArgumentException(InvalidAsyncResult, "result");

            JObject jObject = null;

            using (WebResponse resp = req.EndGetResponse(fbResult.InnerResult))
            using (Stream stream = resp.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                jObject = JObject.Parse(json);
            }

            var errors = jObject["error"] as JObject;
            if (errors != null)
            {
                throw new ApplicationException(
                    String.Format(
                    "Facebook Graph service errors were returned: message: {0}; type: {1}; code: {2}",
                    (string)errors["message"],
                    (string)errors["type"],
                    (int)errors["code"]));
            }

            var data = jObject["data"] as JArray;
            if (data != null)
            {
                foreach (var queryResult in data)
                {
                    var jobjResult = queryResult as JObject;
                    yield return new FBQueryResult(jobjResult);
                }
            }
        }

        #endregion Query methods

        #region Query construction

        private string ConstructQueryUri(string entity, string query)
        {
            if (AccessToken == null)
                throw new ApplicationException("Facebook Access Token not set");

            string queryUri = string.Format(
                "{0}{1}{2}?access_token={3}",
                EndpointBaseUri,
                entity,
                query != null && query != "" ? (query.StartsWith("/") ? query : query.Substring(1)) : "",
                AccessToken);

            return queryUri;
        }

        #endregion Query construction
    }
}
