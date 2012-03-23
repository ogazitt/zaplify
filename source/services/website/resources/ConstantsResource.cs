namespace BuiltSteady.Zaplify.Website.Resources
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using System.Net.Http;
    using System.Net;
    using System.ServiceModel;
    using System.ServiceModel.Web;

    using BuiltSteady.Zaplify.ServerEntities;
    using BuiltSteady.Zaplify.ServiceHost;
    using BuiltSteady.Zaplify.Website.Helpers;
    using BuiltSteady.Zaplify.Website.Models;

    [ServiceContract]
    [LogMessages]
    public class ConstantsResource : BaseResource
    {
        public ConstantsResource()
        { }

        [WebGet(UriTemplate="")]
        [LogMessages]
        public HttpResponseMessageWrapper<Constants> Get(HttpRequestMessage req)
        {
            // constant values are not protected, no authentication required
            try
            {
                return new HttpResponseMessageWrapper<Constants>(req, ConstantsModel.Constants, HttpStatusCode.OK);
            }
            catch (Exception)
            {
                // constants not found - return 404 Not Found
                TraceLog.TraceError("ConstantsResource.Get: not found");
                return new HttpResponseMessageWrapper<Constants>(req, HttpStatusCode.NotFound);
            }
        }
    }
}