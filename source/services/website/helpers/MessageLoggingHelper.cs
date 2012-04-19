namespace BuiltSteady.Zaplify.Website.Helpers
{
    using System;
    using System.Net.Http;
    using System.ServiceModel.Dispatcher;
    using System.ServiceModel.Channels;
    using System.ServiceModel;
    using System.ServiceModel.Description;
    using Microsoft.ApplicationServer.Http.Channels;

    using BuiltSteady.Zaplify.ServiceHost;
    using System.IO;

    public class LoggingMessageTracer : IDispatchMessageInspector,
        IClientMessageInspector
    {
        private Message TraceHttpRequestMessage(HttpRequestMessage msg)
        {
            // trace request
            string messageText = msg.Content != null ? msg.Content.ReadAsStringAsync().Result : "(empty)";
            string tracemsg = String.Format(
                "\n{0} {1}; User-Agent: {2}; Content-Type: {3}; Content-Length: {4}\n" +
                "Body: {5}",
                msg.Method,
                msg.RequestUri.AbsoluteUri,
                msg.Headers.UserAgent,
                msg.Content.Headers.ContentType,
                msg.Content.Headers.ContentLength,
                messageText);
            TraceLog.TraceLine(tracemsg, TraceLog.LogLevel.Detail);
            return msg.ToMessage(); 
        }

        private Message TraceHttpResponseMessage(HttpResponseMessage msg)
        {
            // response tracing is done in BaseResource.ReturnResult
            return msg.ToMessage();
        }

        public object AfterReceiveRequest(ref Message request,
            IClientChannel channel,
            InstanceContext instanceContext)
        {
            request = TraceHttpRequestMessage(request.ToHttpRequestMessage());
            return null;
        }

        public void BeforeSendReply(ref Message reply, object
            correlationState)
        {
            reply = TraceHttpResponseMessage(reply.ToHttpResponseMessage());
        }

        public void AfterReceiveReply(ref Message reply, object
            correlationState)
        {
            reply = TraceHttpResponseMessage(reply.ToHttpResponseMessage());
        }

        public object BeforeSendRequest(ref Message request,
            IClientChannel channel)
        {
            request = TraceHttpRequestMessage(request.ToHttpRequestMessage());
            return null;
        }
    }

    public class LogMessages :
    Attribute, IEndpointBehavior, IServiceBehavior
    {
        void IEndpointBehavior.ApplyClientBehavior(ServiceEndpoint endpoint,
            ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(new LoggingMessageTracer());
        }
        void IEndpointBehavior.ApplyDispatchBehavior(ServiceEndpoint endpoint,
            EndpointDispatcher endpointDispatcher)
        {
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(
                new LoggingMessageTracer());
        }

        void IEndpointBehavior.AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
        }

        void IEndpointBehavior.Validate(ServiceEndpoint endpoint)
        {
        }

        void IServiceBehavior.ApplyDispatchBehavior(
            ServiceDescription desc, ServiceHostBase host)
        {
            foreach (
                ChannelDispatcher cDispatcher in host.ChannelDispatchers)
                foreach (EndpointDispatcher eDispatcher in
                    cDispatcher.Endpoints)
                    eDispatcher.DispatchRuntime.MessageInspectors.Add(
                        new LoggingMessageTracer());
        }

        void IServiceBehavior.AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, System.Collections.ObjectModel.Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {
        }

        void IServiceBehavior.Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
        }
    }
}