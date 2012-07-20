using System;
using System.Drawing;
using MonoTouch.Dialog;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace BuiltSteady.Zaplify.Devices.IPhone.Controls
{
    public class DummyElement : Element, IElementSizing 
    {    
        public DummyElement() : base ("empty")
        {
        }   

        public float GetHeight (UITableView tableView, NSIndexPath indexPath)
        {
            return 0;
        }
    }
}
