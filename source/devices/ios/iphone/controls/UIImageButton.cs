using System;
using System.Drawing;
using MonoTouch.UIKit;

namespace BuiltSteady.Zaplify.Devices.IPhone.Controls
{
	public class UIImageButton : UIImageView
	{
		public event EventHandler Clicked;
		
		public UIImageButton() : base() { }

		public UIImageButton(RectangleF frame) : base (frame) { }

		public UIImageButton(UIImage image) : base (image) { }

		public UIImageButton(UIImage image, UIImage highlightedImage) : base (image, highlightedImage) { }
		
		public override void TouchesEnded (MonoTouch.Foundation.NSSet touches, UIEvent evt)
		{
			UITableViewCell cell = this.Superview as UITableViewCell;
			if (cell != null)
				cell.SetHighlighted(false, false);
			
			if (Clicked != null)
				Clicked(this, new EventArgs());
			//base.TouchesEnded(touches, evt);
		}

        protected override void Dispose(bool isDisposing)
        {
            Clicked = null;
            base.Dispose(isDisposing);
        }
	}
}

