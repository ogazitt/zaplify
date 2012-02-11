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
		public void DebugPage ()
		{
			// render URL and status
			var service = new Section("Service")
			{
				new EntryElement("URL", "URL to connect to", WebServiceHelper.BaseUrl),
				new StringElement("Connected", App.ViewModel.LastNetworkOperationStatus.ToString()),
			};

            // render request queue
			var queue = new Section("Request Queue");
			List<RequestQueue.RequestRecord> requests = RequestQueue.GetAllRequestRecords();
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
					queue.Add (sse);
                }
            }

			var traceMessages = new Section("Trace Messages");
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
				sse.AccessoryTapped += delegate 
				{
					string msg = m;
					var alert = new UIAlertView ("Detail", msg, null, "Ok");
					alert.Show ();
				};
				traceMessages.Add(sse);
			};

			var root = new RootElement("Debug")
			{
				service,
				queue,
				traceMessages,
			};

			var dvc = new DialogViewController (root, true);
			this.NavigationController.PushViewController (dvc, true);
		}
		

	}
}

