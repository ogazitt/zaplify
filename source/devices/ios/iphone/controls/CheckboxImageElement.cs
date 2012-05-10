using System;
using System.Drawing;
using MonoTouch.Dialog;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace BuiltSteady.Zaplify.Devices.IPhone.Controls
{
    public class CheckboxImageElement : BooleanImageElement 
    {    
        static UIImage onImage = UIImage.FromFile("Images/checkbox.on.png");
        static UIImage offImage = UIImage.FromFile("Images/checkbox.off.png");

        public CheckboxImageElement(string caption, bool value) : base (caption, value, onImage, offImage)
        {
        }   

        public override UITableViewCell GetCell(UITableView tv)
        {
           var cell = base.GetCell(tv);
            cell.SelectionStyle = UITableViewCellSelectionStyle.None;
            return cell;
        } 
    }
}
