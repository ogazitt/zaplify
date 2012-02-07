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
                    RetrieveRequestInfo(req, out typename, out reqtype, out id, out name);
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
		
		private static void RetrieveRequestInfo(RequestQueue.RequestRecord req, out string typename, out string reqtype, out string id, out string name)
        {
            typename = req.BodyTypeName;
            reqtype = "";
            id = "";
            name = "";
            switch (req.ReqType)
            {
                case RequestQueue.RequestRecord.RequestType.Delete:
                    reqtype = "Delete";
                    id = ((ZaplifyEntity)req.Body).ID.ToString();
                    name = ((ZaplifyEntity)req.Body).Name;
                    break;
                case RequestQueue.RequestRecord.RequestType.Insert:
                    reqtype = "Insert";
                    id = ((ZaplifyEntity)req.Body).ID.ToString();
                    name = ((ZaplifyEntity)req.Body).Name;
                    break;
                case RequestQueue.RequestRecord.RequestType.Update:
                    reqtype = "Update";
                    switch (req.BodyTypeName)
                    {
                        case "Tag":
                            name = ((List<Tag>)req.Body)[0].Name;
                            id = ((List<Tag>)req.Body)[0].ID.ToString();
                            break;
                        case "Item":
                            name = ((List<Item>)req.Body)[0].Name;
                            id = ((List<Item>)req.Body)[0].ID.ToString();
                            break;
                        case "Folder":
                            name = ((List<Folder>)req.Body)[0].Name;
                            id = ((List<Folder>)req.Body)[0].ID.ToString();
                            break;
                        default:
                            name = "(unrecognized entity)";
                            break;
                    }
                    break;
                default:
                    reqtype = "Unrecognized";
                    break;
            }
        }
	}
}

