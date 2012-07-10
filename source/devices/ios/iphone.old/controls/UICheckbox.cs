using System;
using System.Drawing;
using MonoTouch.UIKit;

namespace BuiltSteady.Zaplify.Devices.IPhone.Controls
{
	public class UICheckbox : UIImageButton
	{
		private UIImage falseImage, trueImage;
		private bool state;
		
		// user state
		public object UserState { get; set; }
		
		// value of the checkbox
		public bool Value 
		{
			get
			{
				return state;
			}
			set
			{
				if (state != value)
				{
					state = value;
					// set the right image
					this.Image = state ? trueImage : falseImage;
				}
			}
		}
				
		public UICheckbox() : base()
		{
			this.falseImage = UIImageCache.GetUIImage("Images/checkbox.off.png");
			this.trueImage = UIImageCache.GetUIImage("Images/checkbox.on.png");
			this.Image = falseImage;
			this.Value = false;
		}

		public UICheckbox(UIImage falseImage, UIImage trueImage) : base()
		{ 
			this.falseImage = falseImage;
			this.trueImage = trueImage;
			this.Image = falseImage;
			this.Value = false;
		}
		
		public override void TouchesEnded (MonoTouch.Foundation.NSSet touches, UIEvent evt)
		{
			// flip the state
			Value = !Value;
			
			// if this is a child of a UITableViewCell, flip the ImageView image as well
			UITableViewCell cell = this.Superview as UITableViewCell;
			if (cell != null)
				cell.ImageView.Image = Value ? trueImage : falseImage;
			
			// call the superclass handler (which will invoke the Clicked event handler)
			base.TouchesEnded(touches, evt);
		}

        protected override void Dispose(bool disposing)
        {
            falseImage = null;
            trueImage = null;
            UserState = null;
            base.Dispose(disposing);
        }
	}
}

