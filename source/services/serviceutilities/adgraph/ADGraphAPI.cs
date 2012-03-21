using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json.Linq;

namespace BuiltSteady.Zaplify.ServiceUtilities.ADGraph
{
    public sealed class ADGraphAPI
    {
        #region Constants

        private const string InvalidAsyncResult = "The IAsyncResult instance is invalid in the current context.";

        private const string EndpointBaseUri = "https://graph.windows.net/graphtestppe.ccsctp.net/persongraph/QueryPeople";
        private const int DefaultMaximumDepth = 2;

        #endregion Constants

        #region Properties

        public string FacebookAccessToken { get; set; }
        public string ADAccessToken { get; set; }
        public int MaximumDepth { get; set; }

        #endregion Properties

        #region Constructor

        public ADGraphAPI()
        {
            MaximumDepth = DefaultMaximumDepth;
        }

        #endregion Constructor

        #region Query methods

        public IEnumerable<ADQueryResult> Query(string query)
        {
            IAsyncResult result = BeginQuery(query, null, null);
            return EndQuery(result);
        }

        public IAsyncResult BeginQuery(string query, AsyncCallback callback, object state)
        {
            string uri = ConstructQueryUri(query);
            HttpWebRequest req = WebRequest.Create(uri) as HttpWebRequest;
            req.Accept = "application/json";

            ADResult fbResult = new ADResult(state);
            fbResult.InnerResult = req.BeginGetResponse(
                (result) =>
                {
                    if (callback != null)
                        callback(fbResult);
                },
                req);

            return fbResult;
        }

        public IEnumerable<ADQueryResult> EndQuery(IAsyncResult result)
        {
            if (!result.IsCompleted)
                result.AsyncWaitHandle.WaitOne();

            ADResult fbResult = result as ADResult;
            if (fbResult == null)
                throw new ArgumentException(InvalidAsyncResult, "result");

            WebRequest req = fbResult.InnerResult.AsyncState as WebRequest;
            if (req == null)
                throw new ArgumentException(InvalidAsyncResult, "result");

            JObject jObject = null;

            try 
	        {	        
                using (WebResponse resp = req.EndGetResponse(fbResult.InnerResult))
                using (Stream stream = resp.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    jObject = JObject.Parse(json);
                }
	        }
	        catch (WebException ex)
	        {
                // if this is a web exception (400 or 500 HTTP status codes), try 
                // to retrieve any error messages in the json payload
                HttpWebResponse response = ex.Response as HttpWebResponse;
                if (response != null)
                {
                    if (response.ContentEncoding.ToLower().Contains("json"))
                    {
                        using (Stream stream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string json = reader.ReadToEnd();
                            jObject = JObject.Parse(json);
                        }
                    }
                }
	        }

            var errors = jObject["error"] as JObject;
            if (errors != null)
            {
                string message = errors.ToString();
                int errorCode = 0;
                int.TryParse(errors.Value<string>("code"), out errorCode);
                switch (errorCode)
                {
                    case 190:
                        message = "Facebook authentication failed - need to renew token";
                        break;
                    case 1190:
                        message = "Directory authentication failed - need to renew token";
                        break;
                }
                throw new ApplicationException(
                    String.Format(
                    "AD Graph service errors were returned: message: {0}; code: {1}",
                    message,
                    errorCode),
                    new ApplicationException(errorCode.ToString()));
            }

            var d = jObject["d"] as JObject;
            var data = d["results"] as JArray;
            if (data != null)
            {
                foreach (var queryResult in data)
                {
                    var jobjResult = queryResult as JObject;
                    yield return new ADQueryResult(jobjResult);
                }
            }
        }

        #endregion Query methods

        #region Query construction

        private string ConstructQueryUri(string query)
        {
            if (ADAccessToken == null && FacebookAccessToken == null)
                throw new ApplicationException("Neith AD nor Facebook Access Tokens were set");

            string queryUri = string.Format(
                "{0}?searchTerm='{1}'&maximumDepth={2}",
                EndpointBaseUri,
                query,
                MaximumDepth);

            if (ADAccessToken != null)
                queryUri = String.Format("{0}&directoryToken='{1}'", queryUri, ADAccessToken);

            if (FacebookAccessToken != null)
                queryUri = String.Format("{0}&facebookToken='{1}'", queryUri, FacebookAccessToken);

            return queryUri;
        }

        #endregion Query construction
    }
}
