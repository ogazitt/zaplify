using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json.Linq;

namespace BuiltSteady.Zaplify.ServiceUtilities.Supermarket
{
    public sealed class SupermarketAPI
    {
        #region Constants

        private const string InvalidAsyncResult = "The IAsyncResult instance is invalid in the current context.";

        private const string APIKeyDefault = "0f573cb7cd";

        private const string EndpointBaseUri = "http://www.SupermarketAPI.com/api.asmx/";

        private const string SupermarketNS = "http://www.SupermarketAPI.com";
        private const string SupermarketPrefix = "sm";
        
        private const string SearchProductResultXPath = "./sm:Product";
        private const string SearchProductResultItemname = "./sm:Itemname";
        private const string SearchProductResultItemCategory = "./sm:ItemCategory";
        private const string SearchProductResultItemDescription = "./sm:ItemDescription";
        private const string SearchProductResultItemID = "./sm:ItemID";
        private const string SearchProductResultItemImage = "./sm:ItemImage";
        private const string SearchProductResultAisleNumber = "./sm:AisleNumber";

        private const string GetGroceriesResultXPath = "./sm:string";
        private const string GetGroceriesResultName = ".";

        #endregion Constants

        #region Properties

        public string APIKey { get; set; }

        #endregion Properties

        #region Constructor

        public SupermarketAPI()
        {
            APIKey = APIKeyDefault;
        }

        #endregion Constructor

        #region Query methods

        public IEnumerable<SupermarketQueryResult> Query(string entity)
        {
            return Query(SupermarketQueries.SearchByProductName, entity);
        }

        public IEnumerable<SupermarketQueryResult> Query(string query, string entity)
        {
            IAsyncResult result = BeginQuery(query, entity, null, null);
            return EndQuery(result);
        }

        public IAsyncResult BeginQuery(string entity, AsyncCallback callback, object state)
        {
            return BeginQuery(SupermarketQueries.SearchByProductName, entity, callback, state);
        }

        public IAsyncResult BeginQuery(string query, string entity, AsyncCallback callback, object state)
        {
            string uri = ConstructQueryUri(query, entity);
            WebRequest req = WebRequest.Create(uri);

            SupermarketResult smResult = new SupermarketResult(state);
            smResult.InnerResult = req.BeginGetResponse(
                (result) =>
                {
                    if (callback != null)
                        callback(smResult);
                },
                req);

            return smResult;
        }

        public IEnumerable<SupermarketQueryResult> EndQuery(IAsyncResult result)
        {
            if (!result.IsCompleted)
                result.AsyncWaitHandle.WaitOne();

            SupermarketResult supermarketResult = result as SupermarketResult;
            if (supermarketResult == null)
                throw new ArgumentException(InvalidAsyncResult, "result");

            WebRequest req = supermarketResult.InnerResult.AsyncState as WebRequest;
            if (req == null)
                throw new ArgumentException(InvalidAsyncResult, "result");

            XElement root;
            XmlNamespaceManager nsmgr;

            using (WebResponse resp = req.EndGetResponse(supermarketResult.InnerResult))
            using (Stream stream = resp.GetResponseStream())
            {
                XmlReader reader = XmlReader.Create(stream);
                root = XElement.Load(reader);
                nsmgr = new XmlNamespaceManager(reader.NameTable);
            }

            // Extend the namespace manager with some prefixes needed for processing results.
            nsmgr.AddNamespace(SupermarketPrefix, SupermarketNS);

            /* TODO: add error handling
            var errors = root.XPathSelectElements(SearchErrorXPath, nsmgr);
            if (errors.Count() > 0)
                // TODO:  Probably want to report these somewhere.
                throw new ApplicationException("Search service errors were returned.");
             */

            var searchProductResults = root.XPathSelectElements(SearchProductResultXPath, nsmgr);
            foreach (var webResult in searchProductResults)
            {
                var itemName = webResult.XPathSelectElement(SearchProductResultItemname, nsmgr);
                var description = webResult.XPathSelectElement(SearchProductResultItemDescription, nsmgr);
                var category = webResult.XPathSelectElement(SearchProductResultItemCategory, nsmgr);
                var id = webResult.XPathSelectElement(SearchProductResultItemID, nsmgr);
                var image = webResult.XPathSelectElement(SearchProductResultItemImage, nsmgr);
                var aisle = webResult.XPathSelectElement(SearchProductResultAisleNumber, nsmgr);
                var retval = new SupermarketQueryResult();
                retval[SupermarketQueryResult.Name] = itemName != null ? itemName.Value : null;
                retval[SupermarketQueryResult.Description] = description != null ? description.Value : null;
                retval[SupermarketQueryResult.Category] = category != null ? category.Value : null;
                retval[SupermarketQueryResult.ID] = id != null ? id.Value : null;
                retval[SupermarketQueryResult.Image] = image != null ? image.Value : null;
                retval[SupermarketQueryResult.Aisle] = aisle != null ? aisle.Value : null;
                yield return retval;
            }
        }

        #endregion Query methods

        #region Query construction

        private string ConstructQueryUri(string query, string entity)
        {
            if (APIKey == null)
                throw new ApplicationException("Supermarket API key not set");

            string queryUri = string.Format(
                "{0}{1}{2}&APIKEY={3}",
                EndpointBaseUri,
                query,
                entity,
                APIKey);

            return queryUri;
        }

        #endregion Query construction
    }
}
