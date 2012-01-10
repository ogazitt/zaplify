using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Channels;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Diagnostics;
using System.Net.Http;
using Microsoft.ApplicationServer.Http.Channels;
using Microsoft.WindowsAzure.ServiceRuntime;
using ServiceHelpers;

namespace TaskStoreWeb.Helpers
{
    public class LoggingMessageTracer : IDispatchMessageInspector,
        IClientMessageInspector
    {
        private Message TraceHttpRequestMessage(HttpRequestMessage msg)
        {
            string tracemsg = String.Format(
                "Web Request on URL: {0}\n" +
                "Header: {1}\n" +
                "Body: {2}",
                msg.RequestUri.AbsoluteUri,
                msg,
                msg.Content != null ? msg.Content.ReadAsString() : "(empty)");

            LoggingHelper.TraceLine(tracemsg, LoggingHelper.LogLevel.Info);

            return msg.ToMessage();
        }

        private Message TraceHttpResponseMessage(HttpResponseMessage msg)
        {
            string tracemsg = String.Format(
                "Web Response Header: {0}\n" +
                "Web Response Body: {1}",
                msg,
                msg.Content != null ? msg.Content.ReadAsString() : "(empty)");

            LoggingHelper.TraceLine(tracemsg, LoggingHelper.LogLevel.Info);

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