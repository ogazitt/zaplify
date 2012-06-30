namespace BuiltSteady.Zaplify.Devices.ClientHelpers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.IO.IsolatedStorage;
    using System.Net;
    using System.Net.Browser;
    using System.Net.Sockets;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Text;

    using Microsoft.Phone.Net.NetworkInformation;
    using BuiltSteady.Zaplify.Devices.ClientEntities;

    public class NetworkHelper
    {
        static Socket socket = null;
        static EndPoint endPoint = null;
        static bool isRequestInProgress = false;    // only one network operation at a time

        static string SpeechUrl
        {
            get
            {
                var url = WebServiceHelper.SpeechUrl;
                bool test = false;
                if (test)
                    url = "http://omrig-air:8081";
                return url;
            }
        }

#region // Network calls

        public static void BeginSpeech(Delegate del, Delegate netOpInProgressDel)
        {
            InvokeNetworkRequest(del, netOpInProgressDel);
        }

        public static void CancelSpeech()
        {
            // clean up the socket
            CleanupSocket();
        }

        public static void EndSpeech(byte[] buffer, int len, Delegate del, Delegate netOpInProgressDel)
        {
            // send the last chunk of the speech file
            SendData(
                buffer,
                len,
                new EventHandler<SocketAsyncEventArgs>(delegate(object o, SocketAsyncEventArgs e)
                {
                    if (e.SocketError != SocketError.Success)
                    {
                        // signal that a network operation is done and unsuccessful
                        netOpInProgressDel.DynamicInvoke(false, OperationStatus.Failed);

                        // clean up the socket
                        CleanupSocket();

                        return;
                    }

                    // send the terminator chunk to the service
                    SendData(
                        null,
                        0,
                        new EventHandler<SocketAsyncEventArgs>(delegate(object obj, SocketAsyncEventArgs ea)
                        {
                            if (ea.SocketError != SocketError.Success)
                            {
                                // signal that a network operation is done and unsuccessful
                                netOpInProgressDel.DynamicInvoke(false, OperationStatus.Failed);

                                // clean up the socket
                                CleanupSocket();

                                return;
                            }

                            // when the last send has completed, receive and process the response
                            ProcessNetworkResponse(del, netOpInProgressDel);
                        }),
                        netOpInProgressDel);
                }),
                netOpInProgressDel);
        }


        // Send an HTTP POST to start a new speech recognition transaction
        public static void SendPost(User user, string encoding, Delegate del, Delegate netOpInProgressDel)
        {
            string url = SpeechUrl + "/api/speech";
            string verb = "POST";

            // get a Uri for the service - this will be used to decode the host / port
            Uri uri = new Uri(url);

            string host = uri.Host;
            if (uri.Port != 80)
                host = String.Format("{0}:{1}", uri.Host, uri.Port);

            // construct the HTTP POST buffer
            string request = String.Format(
                "{0} {1} HTTP/1.1\r\n" +
                "User-Agent: Zaplify-WinPhone\r\n" +
                "Zaplify-Username: {2}\r\n" +
                "Zaplify-Password: {3}\r\n" +
                "Host: {4}\r\n" +
                "Content-Type: application/json\r\n" +
                "{5}\r\n" +
                "Transfer-Encoding: chunked\r\n\r\n",
                verb != null ? verb : "POST",
                url,
                user.Name,
                user.Password,
                host,
                encoding);

            byte[] buffer = Encoding.UTF8.GetBytes(request);

            // send the request HTTP header
            SendData(
                buffer,
                -1,
                new EventHandler<SocketAsyncEventArgs>(delegate(object o, SocketAsyncEventArgs e)
                {
                    if (e.SocketError != SocketError.Success)
                    {
                        // signal that a network operation is done and unsuccessful
                        netOpInProgressDel.DynamicInvoke(false, OperationStatus.Failed);

                        // clean up the socket
                        CleanupSocket();

                        return;
                    }

                    // when the socket setup and HTTP POST + headers have been completed, 
                    // signal the caller
                    del.DynamicInvoke();
                }),
                netOpInProgressDel);
        }

        public static void SendSpeech(byte[] buffer, int len, Delegate callback, Delegate netOpInProgressDel)
        {
            EventHandler<SocketAsyncEventArgs> eh = null;
            if (callback != null)
                eh = new EventHandler<SocketAsyncEventArgs>(delegate(object o, SocketAsyncEventArgs e)
                {
                    callback.DynamicInvoke();
                });

            // send the data and include a callback if the delegate passed in isn't null
            SendData(
                buffer,
                len,
                eh,
                netOpInProgressDel);
        }

#endregion

#region // Helper methods

        private static void CleanupSocket()
        {
            isRequestInProgress = false;

            if (socket != null)
            {
                if (socket.Connected == true)
                {
                    socket.Shutdown(SocketShutdown.Both);
                }
                socket.Close();

                socket = null;
            }
        }

        private static byte[] CreateBuffer(byte[] buffer, int len)
        {
            // buffer will be formatted as:
            //   length in hex\r\n
            //   data\r\n
            string hexlen = String.Format("{0:X}\r\n", len);
            string crlf = "\r\n";
            byte[] lenbuffer = Encoding.UTF8.GetBytes(hexlen);
            byte[] crlfbuffer = Encoding.UTF8.GetBytes(crlf);
            byte[] sendbuf = new byte[len + lenbuffer.Length + crlfbuffer.Length];
            lenbuffer.CopyTo(sendbuf, 0);
            for (int i = 0; i < len; i++)
                sendbuf[lenbuffer.Length + i] = buffer[i];
            crlfbuffer.CopyTo(sendbuf, lenbuffer.Length + len);
            return sendbuf;
        }

        private static byte[] EncodeString(string str)
        {
            char[] unicode = str.ToCharArray();
            byte[] buffer = new byte[unicode.Length];
            int i = 0;
            foreach (char c in unicode)
                buffer[i++] = (byte)c;
            return buffer;
        }

        private static void InvokeNetworkRequest(Delegate del, Delegate netOpInProgressDel)
        {
            // this code is non-reentrant
            if (isRequestInProgress == true)
                return;

            // set the request in progress flag
            isRequestInProgress = true;

            // signal that a network operation is starting
            netOpInProgressDel.DynamicInvoke(true, OperationStatus.Started);

            // get a Uri for the service - this will be used to decode the host / port
            Uri uri = new Uri(SpeechUrl);

            // create the socket
            if (socket == null)
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                if (uri.Host == "localhost")
                    endPoint = new IPEndPoint(IPAddress.Loopback, uri.Port);
                else
                    endPoint = new DnsEndPoint(uri.Host, uri.Port, AddressFamily.InterNetwork);
            }

            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
            socketEventArg.RemoteEndPoint = endPoint;

            // set the connect completion delegate
            socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object o, SocketAsyncEventArgs e)
            {
                if (e.SocketError != SocketError.Success)
                {
                    // signal that a network operation is done and unsuccessful
                    netOpInProgressDel.DynamicInvoke(false, OperationStatus.Failed);

                    // clean up the socket
                    CleanupSocket();

                    return;
                }

                // get the current network interface info
                NetworkInterfaceInfo netInterfaceInfo = socket.GetCurrentNetworkInterface();

                // invoke the completion delegate with the network type info
                del.DynamicInvoke(netInterfaceInfo);
            });

            // if the socket isn't connected, connect now
            if (socket.Connected == false)
            {
                // connect to the service
                try
                {
                    bool ret = socket.ConnectAsync(socketEventArg);
                    if (ret == false)
                    {
                        // signal that a network operation is done and unsuccessful
                        netOpInProgressDel.DynamicInvoke(false, OperationStatus.Failed);

                        // clean up the socket
                        CleanupSocket();
                    }
                }
                catch (Exception ex)
                {
                    // trace network error
                    TraceHelper.AddMessage("InvokeNetworkRequest: ex: " + ex.Message);

                    // signal that a network operation is done and unsuccessful
                    netOpInProgressDel.DynamicInvoke(false, OperationStatus.Failed);

                    // clean up the socket
                    CleanupSocket();
                }
            }
            else
            {
                // socket already connected                 

                // get the current network interface info
                NetworkInterfaceInfo netInterfaceInfo = socket.GetCurrentNetworkInterface();

                // invoke the completion delegate with the network type info
                del.DynamicInvoke(netInterfaceInfo);
            }
        }

        private static void ProcessNetworkResponse(Delegate del, Delegate netOpInProgressDel)
        {
            if (isRequestInProgress == false)
                return;

            SocketAsyncEventArgs socketReceiveEventArg = new SocketAsyncEventArgs();
            socketReceiveEventArg.RemoteEndPoint = endPoint;
            socketReceiveEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object o, SocketAsyncEventArgs e)
            {
                if (e.SocketError != SocketError.Success)
                {
                    // signal that a network operation is done and unsuccessful
                    netOpInProgressDel.DynamicInvoke(false, OperationStatus.Failed);

                    // clean up the socket
                    CleanupSocket();

                    return;
                }

                // response was received
                int num = e.BytesTransferred;
                if (num > 0)
                {
                    // get the response as a string
                    string resp = Encoding.UTF8.GetString(e.Buffer, 0, num);

                    string[] lines = resp.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                    if (lines == null || lines.Length < 2 || 
                        lines[0] != "HTTP/1.1 200 OK")
                    {
                        // signal that a network operation is done and unsuccessful
                        netOpInProgressDel.DynamicInvoke(false, OperationStatus.Failed);

                        // clean up the socket
                        CleanupSocket();
                    }

                    // signal that a network operation is done and successful
                    netOpInProgressDel.DynamicInvoke(false, OperationStatus.Success);

                    // discover the content type (default to json)
                    string contentType = "application/json";
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("Content-Type:"))
                        {
                            string compositeContentType = line.Split(':')[1];
                            contentType = compositeContentType.Split(';')[0].Trim();
                            break;
                        }
                    }

                    // get a stream over the last component of the network response
                    MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(lines[lines.Length - 1]));

                    // deserialize the response string
                    HttpMessageBodyWrapper<string> body = 
                        (HttpMessageBodyWrapper<string>) HttpWebResponseWrapper<string>.DeserializeResponseBody(
                        stream, contentType, typeof(HttpMessageBodyWrapper<string>));

                    // signal that a network operation is done and successful
                    netOpInProgressDel.DynamicInvoke(false, OperationStatus.Success);

                    // reset the request in progress flag
                    isRequestInProgress = false;

                    // * leave the socket open for a potential next transaction *
                    // CleanupSocket();

                    // invoke the delegate passed in with the actual response text to return to the caller
                    del.DynamicInvoke(body == null ? "" : body.Value); 
                }
                else
                {
                    // signal that a network operation is done and unsuccessful
                    netOpInProgressDel.DynamicInvoke(false, OperationStatus.Failed);

                    // clean up the socket
                    CleanupSocket();
                }
            });

            // set the receive buffer
            byte[] buffer = new byte[32768];
            socketReceiveEventArg.SetBuffer(buffer, 0, buffer.Length);

            // receive the response
            try
            {
                bool ret = socket.ReceiveAsync(socketReceiveEventArg);
                if (ret == false)
                {
                    // signal that a network operation is done and unsuccessful
                    netOpInProgressDel.DynamicInvoke(false, OperationStatus.Failed);

                    // clean up the socket
                    CleanupSocket();
                }
            }
            catch (Exception ex)
            {
                // trace network error
                TraceHelper.AddMessage("ProcessNetworkResponse: ex: " + ex.Message);

                // signal that a network operation is done and unsuccessful
                netOpInProgressDel.DynamicInvoke(false, OperationStatus.Failed);

                // clean up the socket
                CleanupSocket();
            }
        }

        private static void SendData(byte[] buffer, int len, EventHandler<SocketAsyncEventArgs> eh, Delegate netOpInProgressDel)
        {
            // a request must be in progress
            if (isRequestInProgress == false)
                return;
            
            SocketAsyncEventArgs socketSendEventArg = new SocketAsyncEventArgs();
            socketSendEventArg.RemoteEndPoint = endPoint;
            if (eh != null)
                socketSendEventArg.Completed += eh;

            byte[] sendbuf = null;

            // if the buffer is null, it means that we need to send the terminating chunk
            if (buffer == null)
            {
                sendbuf = Encoding.UTF8.GetBytes("0\r\n\r\n");
            }
            else
            {
                // if the length was passed in as zero, compute it from the buffer length
                if (len == 0)
                    len = buffer.Length;

                // if the length is positive (it'll never be zero), we need to prefix the 
                // length (expressed in hex) to the buffer.  A length of -1 signals to send 
                // the buffer as-is (this is for sending the headers, which need no length)
                if (len > -1)
                {
                    sendbuf = CreateBuffer(buffer, len);
                }
                else
                    sendbuf = buffer;
            }

            // send the buffer
            try 
            {
                // set the buffer and send the chunk asynchronously
                socketSendEventArg.SetBuffer(sendbuf, 0, sendbuf.Length);
                bool ret = socket.SendAsync(socketSendEventArg);
                if (ret == false)
                {
                    // signal that a network operation is done and unsuccessful
                    netOpInProgressDel.DynamicInvoke(false, OperationStatus.Failed);

                    // clean up the socket
                    CleanupSocket();
                }
            }
            catch (Exception ex)
            {
                // trace network error
                TraceHelper.AddMessage("SendData: ex: " + ex.Message);

                // signal that a network operation is done and unsuccessful
                netOpInProgressDel.DynamicInvoke(false, OperationStatus.Failed);

                // clean up the socket
                CleanupSocket();
            }
        }

#endregion
    }
}