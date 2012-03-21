using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace BuiltSteady.Zaplify.ServiceUtilities.Bing
{
    public sealed class BingSearch
    {
        #region Constants

        private const string InvalidAsyncResult = "The IAsyncResult instance is invalid in the current context.";

        private const string AppIDDefault = "7E4F1CB4F1F70B16F38E4C98B64530AD6C658BB2";
        private const AdultFilter AdultFilterDefault = AdultFilter.Moderate;
        private const string MarketDefault = "en-us";
        private const SearchOption OptionsDefault = SearchOption.None;
        private const SourceType SourcesDefault = SourceType.Web;
        private const string VersionDefault = "2.0";

        private const string EndpointBaseUri = "http://api.bing.net/xml.aspx";

        private const string SearchApiNS = "http://schemas.microsoft.com/LiveSearch/2008/04/XML/element";
        private const string SearchApiPrefix = "api";
        private const string SearchErrorXPath = "./api:Errors/api:Error";

        private const string SearchWebNS = "http://schemas.microsoft.com/LiveSearch/2008/04/XML/web";
        private const string SearchWebPrefix = "web";
        private const string SearchWebResultXPath = "./web:Web/web:Results/web:WebResult";
        private const string SearchWebResultTitle = "./web:Title";
        private const string SearchWebResultUrl = "./web:Url";
        private const string SearchWebResultDescription = "./web:Description";

        #endregion Constants

        #region Properties

        public AdultFilter AdultFilter { get; set; }
        public string AppID { get; set; }
        public string Market { get; set; }
        public SearchOption Options { get; set; }
        public string Version { get; set; }

        #endregion Properties

        #region Constructor

        public BingSearch()
        {
            AdultFilter = AdultFilterDefault;
            Market = MarketDefault;
            Options = OptionsDefault;
            Version = VersionDefault;
            AppID = AppIDDefault;
        }

        #endregion Constructor

        #region Query methods

        public IEnumerable<SearchResult> Query(string query)
        {
            return Query(query, SourcesDefault);
        }

        public IEnumerable<SearchResult> Query(string query, SourceType sources)
        {
            IAsyncResult result = BeginQuery(query, sources, null, null);
            return EndQuery(result);
        }

        public IAsyncResult BeginQuery(string query, AsyncCallback callback, object state)
        {
            return BeginQuery(query, SourcesDefault, callback, state);
        }

        public IAsyncResult BeginQuery(string query, SourceType sources, AsyncCallback callback, object state)
        {
            string uri = ConstructQueryUri(query, sources);
            WebRequest req = WebRequest.Create(uri);

            BingResult bingResult = new BingResult(state);
            bingResult.InnerResult = req.BeginGetResponse(
                (result) =>
                {
                    if (callback != null)
                        callback(bingResult);
                },
                req);

            return bingResult;
        }

        public IEnumerable<SearchResult> EndQuery(IAsyncResult result)
        {
            if (!result.IsCompleted)
                result.AsyncWaitHandle.WaitOne();

            BingResult bingResult = result as BingResult;
            if (bingResult == null)
                throw new ArgumentException(InvalidAsyncResult, "result");

            WebRequest req = bingResult.InnerResult.AsyncState as WebRequest;
            if (req == null)
                throw new ArgumentException(InvalidAsyncResult, "result");

            XElement root;
            XmlNamespaceManager nsmgr;

            using (WebResponse resp = req.EndGetResponse(bingResult.InnerResult))
            using (Stream stream = resp.GetResponseStream())
            {
                XmlReader reader = XmlReader.Create(stream);
                root = XElement.Load(reader);
                nsmgr = new XmlNamespaceManager(reader.NameTable);
            }

            // Extend the namespace manager with some prefixes needed for processing results.
            nsmgr.AddNamespace(SearchApiPrefix, SearchApiNS);
            nsmgr.AddNamespace(SearchWebPrefix, SearchWebNS);

            var errors = root.XPathSelectElements(SearchErrorXPath, nsmgr);
            if (errors.Count() > 0)
                // TODO:  Probably want to report these somewhere.
                throw new ApplicationException("Search service errors were returned.");

            var webResults = root.XPathSelectElements(SearchWebResultXPath, nsmgr);
            foreach (var webResult in webResults)
            {
                yield return new WebResult()
                {
                    Title = webResult.XPathSelectElement(SearchWebResultTitle, nsmgr).Value,
                    Url = webResult.XPathSelectElement(SearchWebResultUrl, nsmgr).Value,
                    Description = webResult.XPathSelectElement(SearchWebResultDescription, nsmgr).Value
                };
            }
        }

        #endregion Query methods

        #region Query construction

        private string ConstructQueryUri(string query, SourceType sources)
        {
            string queryUri = string.Format(
                "{0}?AppId={1}&Query={2}&Sources={3}&Version={4}&Market={5}&Adult={6}",
                EndpointBaseUri,
                AppID,
                query,
                BuildSourceTypeValue(sources),
                Version,
                Market,
                AdultFilter);

            if (Options != SearchOption.None)
                queryUri += "&Options=" + BuildSearchOptionsValue(Options);

            return queryUri;
        }

        private string BuildSearchOptionsValue(SearchOption options)
        {
            string value = options.ToString();
            return value.Replace(", ", "+");
        }

        private string BuildSourceTypeValue(SourceType sources)
        {
            string value = sources.ToString();
            return value.Replace(", ", "+");
        }

        #endregion Query construction
    }
}
