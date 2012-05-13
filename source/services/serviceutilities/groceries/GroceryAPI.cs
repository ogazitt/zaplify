using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json.Linq;

namespace BuiltSteady.Zaplify.ServiceUtilities.Grocery
{
    public sealed class GroceryAPI
    {
        #region Constants

        private const string InvalidAsyncResult = "The IAsyncResult instance is invalid in the current context.";

        private const string DefaultEndpointBaseUri = "http://api.zaplify.com/Grocery/";

        #endregion Constants

        #region Constructor

        public GroceryAPI()
        {
            EndpointBaseUri = DefaultEndpointBaseUri;
        }

        #endregion Constructor

        #region Properties

        public string EndpointBaseUri { get; set; }

        #endregion Properties

        #region Query methods

        public IEnumerable<GroceryQueryResult> Query(string entity)
        {
            return Query(GroceryQueries.GroceryCategory, entity);
        }

        public IEnumerable<GroceryQueryResult> Query(string query, string entity)
        {
            IAsyncResult result = BeginQuery(query, entity, null, null);
            return EndQuery(result);
        }

        public IAsyncResult BeginQuery(string entity, AsyncCallback callback, object state)
        {
            return BeginQuery(GroceryQueries.GroceryCategory, entity, callback, state);
        }

        public IAsyncResult BeginQuery(string query, string entity, AsyncCallback callback, object state)
        {
            string uri = ConstructQueryUri(query, entity);
            WebRequest req = WebRequest.Create(uri);

            GroceryResult smResult = new GroceryResult(state);
            smResult.InnerResult = req.BeginGetResponse(
                (result) =>
                {
                    if (callback != null)
                        callback(smResult);
                },
                req);

            return smResult;
        }

        public IEnumerable<GroceryQueryResult> EndQuery(IAsyncResult result)
        {
            if (!result.IsCompleted)
                result.AsyncWaitHandle.WaitOne();

            GroceryResult groceryResult = result as GroceryResult;
            if (groceryResult == null)
                throw new ArgumentException(InvalidAsyncResult, "result");

            WebRequest req = groceryResult.InnerResult.AsyncState as WebRequest;
            if (req == null)
                throw new ArgumentException(InvalidAsyncResult, "result");

            JObject jObject = null;

            using (WebResponse resp = req.EndGetResponse(groceryResult.InnerResult))
            using (Stream stream = resp.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                jObject = JObject.Parse(json);
            }

            if ((int) jObject["StatusCode"] >= 400)
            {
                throw new ApplicationException(
                    String.Format(
                    "Grocery API returned status code {0}",
                    jObject["StatusCode"]));
            }

            var data = jObject["Groceries"] as JArray;
            if (data != null)
            {
                foreach (var queryResult in data)
                {
                    var jobjResult = queryResult as JObject;
                    yield return new GroceryQueryResult(jobjResult);
                }
            }
            else
            {
                var groceries = jObject["Groceries"] as JObject;
                yield return new GroceryQueryResult(groceries);
            }
        }

        #endregion Query methods

        #region Query construction

        private string ConstructQueryUri(string query, string entity)
        {
            string queryUri = string.Format(
                "{0}{1}{2}",
                EndpointBaseUri,
                query,
                entity);

            return queryUri;
        }

        #endregion Query construction
    }
}
