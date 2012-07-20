using System;
using System.Collections.Generic;
using MonoTouch.UIKit;
using MonoTouch.Dialog;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Devices.ClientEntities;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	public partial class MoreViewController
	{
		public void DebugPage()
		{
            TraceHelper.AddMessage("Debug: constructor");

            // render URL and status
			var serviceUrl = new EntryElement("URL", "URL to connect to", WebServiceHelper.BaseUrl);
            var service = new Section("Service")
			{
				serviceUrl,
                new StringElement("Store New Service URL", delegate 
                { 
                    serviceUrl.FetchValue(); 
                    // validate that this is a good URL before storing it (a bad URL can hose the phone client)
                    Uri uri = null;
                    if (Uri.TryCreate(serviceUrl.Value, UriKind.RelativeOrAbsolute, out uri) && 
                        (uri.Scheme == "http" || uri.Scheme == "https"))
                        WebServiceHelper.BaseUrl = serviceUrl.Value; 
                    else
                        serviceUrl.Value = WebServiceHelper.BaseUrl;
                }),
				new StringElement("Connected", App.ViewModel.LastNetworkOperationStatus.ToString()),
			};

            // render user queue
			var userQueue = new Section("User Queue");
			userQueue.Add(new StringElement(
				"Clear Queue", 
				delegate 
			    { 
                    RequestQueue.DeleteQueue(RequestQueue.UserQueue);
					userQueue.Clear ();
				}));
			
			List<RequestQueue.RequestRecord> requests = RequestQueue.GetAllRequestRecords(RequestQueue.UserQueue);
            if (requests != null)
            {
                foreach (var req in requests)
                {
                    string typename;
                    string reqtype;
                    string id;
                    string name;
                    RequestQueue.RetrieveRequestInfo(req, out typename, out reqtype, out id, out name);
                    var sse = new StyledStringElement(String.Format("  {0} {1} {2} (id {3})", reqtype, typename, name, id))
					{
						Font = UIFont.FromName("Helvetica", UIFont.SmallSystemFontSize),
					};
					userQueue.Add (sse);
                }
            }

            // render system queue
            var systemQueue = new Section("System Queue");
            systemQueue.Add(new StringElement(
                "Clear Queue", 
                delegate 
                { 
                    RequestQueue.DeleteQueue(RequestQueue.SystemQueue);
                    systemQueue.Clear ();
                }));
            
            requests = RequestQueue.GetAllRequestRecords(RequestQueue.SystemQueue);
            if (requests != null)
            {
                foreach (var req in requests)
                {
                    string typename;
                    string reqtype;
                    string id;
                    string name;
                    RequestQueue.RetrieveRequestInfo(req, out typename, out reqtype, out id, out name);
                    var sse = new StyledStringElement(String.Format("  {0} {1} {2} (id {3})", reqtype, typename, name, id))
                    {
                        Font = UIFont.FromName("Helvetica", UIFont.SmallSystemFontSize),
                    };
                    systemQueue.Add (sse);
                }
            }

			var traceMessages = new Section("Trace Messages");
			traceMessages.Add(new StringElement(
				"Clear Trace", 
				delegate 
			    { 
					TraceHelper.ClearMessages();
					traceMessages.Clear ();
				}));
			traceMessages.Add(new StringElement(
				"Send Trace", 
				delegate 
			    { 
					TraceHelper.SendMessages(App.ViewModel.User);
				}));
			foreach (var m in TraceHelper.GetMessages().Split('\n'))
			{
				// skip empty messages
				if (m == "")
					continue;
				
				// create a new (small) string element with a detail indicator which 
				// brings up a message box with the entire message
				var sse = new StyledStringElement(m) 
				{ 
					Accessory = UITableViewCellAccessory.DetailDisclosureButton,
					Font = UIFont.FromName("Helvetica", UIFont.SmallSystemFontSize),
				};
				string msg = m;  // make a copy for the closure below
				sse.AccessoryTapped += delegate 
				{
					var alert = new UIAlertView ("Detail", msg, null, "Ok");
					alert.Show ();
				};
				traceMessages.Add(sse);
			};

			var root = new RootElement("Debug")
			{
				service,
				userQueue,
                systemQueue,
				traceMessages,
			};

			var dvc = new DialogViewController (root, true);
            dvc.TableView.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.PageBackground);
            //this.PushViewController (dvc, true);
            //this.NavigationController.PushViewController (dvc, true);
            controller.PushViewController(dvc, true);
		}
	}
}

