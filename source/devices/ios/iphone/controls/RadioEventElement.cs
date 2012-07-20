using System;
using MonoTouch.Dialog;
using MonoTouch.UIKit;
using MonoTouch.Foundation;

namespace BuiltSteady.Zaplify.Devices.IPhone.Controls
{
	public class RadioEventElement : RadioElement 
	{
    	public RadioEventElement (string caption, string group) : base (caption, group) { }

    	public RadioEventElement (string caption) : base (caption) { }

	    public override void Selected (DialogViewController dvc, UITableView tableView, NSIndexPath path)
	    {
	        base.Selected(dvc, tableView, path);
	        var selected = OnSelected;
	        if (selected != null)
	            selected(this, EventArgs.Empty);
	    }
	
	    public event EventHandler<EventArgs> OnSelected;
	}	
}

