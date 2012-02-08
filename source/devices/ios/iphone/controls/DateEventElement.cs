using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using MonoTouch.Dialog;
using MonoTouch.UIKit;
using MonoTouch.Foundation;

namespace BuiltSteady.Zaplify.Devices.IPhone.Controls
{
	public class DateTimeEventElement : StringElement {
		public DateTime DateValue;
		public UIDatePicker datePicker;
		protected internal NSDateFormatter fmt = new NSDateFormatter () {
			DateStyle = NSDateFormatterStyle.Short
		};
		public event NSAction ValueSelected;

		public DateTimeEventElement (string caption, DateTime date) : base (caption)
		{
			DateValue = date;
			Value = FormatDate (date);
		}	

		public override UITableViewCell GetCell (UITableView tv)
		{
			Value = FormatDate (DateValue);
			var cell = base.GetCell (tv);
			cell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
			return cell;
		}
 
		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);
			if (disposing){
				if (fmt != null){
					fmt.Dispose ();
					fmt = null;
				}
				if (datePicker != null){
					datePicker.Dispose ();
					datePicker = null;
				}
			}
		}

		public virtual string FormatDate (DateTime dt)
		{
			return fmt.ToString (dt) + " " + dt.ToLocalTime ().ToShortTimeString ();
		}

		public virtual UIDatePicker CreatePicker ()
		{
			var picker = new UIDatePicker (RectangleF.Empty){
				AutoresizingMask = UIViewAutoresizing.FlexibleWidth,
				Mode = UIDatePickerMode.DateAndTime,
				Date = DateValue
			};
			return picker;
		}

		static RectangleF PickerFrameWithSize (SizeF size)
		{                                                                                                                                    
			var screenRect = UIScreen.MainScreen.ApplicationFrame;
			float fY = 0, fX = 0;

			switch (UIApplication.SharedApplication.StatusBarOrientation){
			case UIInterfaceOrientation.LandscapeLeft:
			case UIInterfaceOrientation.LandscapeRight:
				fX = (screenRect.Height - size.Width) /2;
				fY = (screenRect.Width - size.Height) / 2 -17;
				break;

			case UIInterfaceOrientation.Portrait:
			case UIInterfaceOrientation.PortraitUpsideDown:
				fX = (screenRect.Width - size.Width) / 2;
				fY = (screenRect.Height - size.Height) / 2 - 25;
				break;
			}

			return new RectangleF (fX, fY, size.Width, size.Height);
		}                                                                                                                                    

		class MyViewController : UIViewController {
			DateTimeEventElement container;

			public MyViewController (DateTimeEventElement container)
			{
				this.container = container;
			}

			public override void ViewWillDisappear (bool animated)
			{
				base.ViewWillDisappear (animated);
				container.DateValue = container.datePicker.Date;
				if (container.ValueSelected != null)
					container.ValueSelected();
			}

			public override void DidRotate (UIInterfaceOrientation fromInterfaceOrientation)
			{
				base.DidRotate (fromInterfaceOrientation);
				container.datePicker.Frame = PickerFrameWithSize (container.datePicker.SizeThatFits (SizeF.Empty));
			}

			public bool Autorotate { get; set; }

			public override bool ShouldAutorotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation)
			{
				return Autorotate;
			}
		}

		public override void Selected (DialogViewController dvc, UITableView tableView, NSIndexPath path)
		{
			var vc = new MyViewController (this) {
				Autorotate = dvc.Autorotate
			};
			datePicker = CreatePicker ();
			datePicker.Frame = PickerFrameWithSize (datePicker.SizeThatFits (SizeF.Empty));

			vc.View.BackgroundColor = UIColor.Black;
			vc.View.AddSubview (datePicker);
			dvc.ActivateController (vc);
		}
	}

	public class DateEventElement : DateTimeEventElement {
		public DateEventElement (string caption, DateTime date) : base (caption, date)
		{
			fmt.DateStyle = NSDateFormatterStyle.Medium;
		}

		public override string FormatDate (DateTime dt)
		{
			return fmt.ToString (dt);
		}

		public override UIDatePicker CreatePicker ()
		{
			var picker = base.CreatePicker ();
			picker.Mode = UIDatePickerMode.Date;
			return picker;
		}
	}

	public class TimeEventElement : DateTimeEventElement {
		public TimeEventElement (string caption, DateTime date) : base (caption, date)
		{
		}

		public override string FormatDate (DateTime dt)
		{
			return dt.ToLocalTime ().ToShortTimeString ();
		}

		public override UIDatePicker CreatePicker ()
		{
			var picker = base.CreatePicker ();
			picker.Mode = UIDatePickerMode.Time;
			return picker;
		}
	}

}

