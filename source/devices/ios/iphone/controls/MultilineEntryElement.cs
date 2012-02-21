using System;
using System.Drawing;
using MonoTouch.Dialog;
using MonoTouch.UIKit;
using MonoTouch.Foundation;

namespace BuiltSteady.Zaplify.Devices.IPhone.Controls
{
	/// <summary>
	/// An element that can be used to enter text.
	/// </summary>
	/// <remarks>
	/// This element can be used to enter text both regular and password protected entries. 
	///     
	/// The Text fields in a given section are aligned with each other.
	/// </remarks>
	public class MultilineEntryElement : EntryElement, IElementSizing {
		/// <summary>
		///   The number of lines in the MultilineEntryElement
		/// </summary>
		public int Lines { 
			get {
				return lines;
			}
			set {
				lines = value;
			}
		}
		protected int lines = 1;

		/// <summary>
		/// The key used for reusable UITableViewCells.
		/// </summary>
		static NSString entryKey = new NSString ("MultilineEntryElement");
		protected override NSString EntryKey {
			get {
				return entryKey;
			}
		}
		
		bool isPassword, becomeResponder;
		UITextView entry;
		static UIFont font = UIFont.BoldSystemFontOfSize (17);
		
		/// <summary>
		/// Constructs an EntryElement with the given caption, placeholder and initial value.
		/// </summary>
		/// <param name="caption">
		/// The caption to use
		/// </param>
		/// <param name="value">
		/// Initial value.
		/// </param>
		public MultilineEntryElement (string caption, string value) : base (caption, "", value)
		{ 
			Value = value;
		}

		/// <summary>
		/// Constructs an EntryElement for password entry with the given caption, placeholder and initial value.
		/// </summary>
		/// <param name="caption">
		/// The caption to use.
		/// </param>
		/// <param name="value">
		/// Initial value.
		/// </param>
		/// <param name="isPassword">
		/// True if this should be used to enter a password.
		/// </param>
		public MultilineEntryElement (string caption, string value, bool isPassword) : base (caption, "", value, isPassword)
		{
			Value = value;
			this.isPassword = isPassword;
		}

		public override string Summary ()
		{
			return Value;
		}
		
		public new string Value
		{
			get
			{
				return base.Value;
			}
			set
			{
				base.Value = value;
				if (entry != null)
					entry.Text = value;
			}
		}

		// 
		// Computes the X position for the entry by aligning all the entries in the Section
		//
		SizeF ComputeEntryPosition (UITableView tv, UITableViewCell cell)
		{
			Section s = Parent as Section;
			if (s.EntryAlignment.Width != 0)
				return s.EntryAlignment;

			// If all EntryElements have a null Caption, align UITextField with the Caption
			// offset of normal cells (at 10px).
			SizeF max = new SizeF (-15, tv.StringSize ("M", font).Height);
			foreach (var e in s.Elements){
				var ee = e as EntryElement;
				if (ee == null)
					continue;
				if (ee.Caption != null) {
					var size = tv.StringSize (ee.Caption, font);
					if (size.Width > max.Width)
						max = size;
				}
			}
			
			// set the height based on the Lines property
			max.Height = GetTextViewHeight(this.GetContainerTableView());
			
			s.EntryAlignment = new SizeF (25 + Math.Min (max.Width, 160), max.Height);
			return s.EntryAlignment;
		}

		protected virtual UITextView CreateTextView (RectangleF frame)
		{
			return new UITextView (frame) {
				AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleLeftMargin, 
				SecureTextEntry = isPassword,
				Text = Value ?? "",
				Tag = 1,
				Font = font,
				BackgroundColor = UIColor.FromRGB (247, 247, 247),
			};
		}

		static NSString cellkey = new NSString ("MultilineEntryElement");

		protected override NSString CellKey {
			get {
				return cellkey;
			}
		}

		public override UITableViewCell GetCell (UITableView tv)
		{
			var cell = tv.DequeueReusableCell (CellKey);
			if (cell == null){
				cell = new UITableViewCell (UITableViewCellStyle.Default, CellKey);
				cell.SelectionStyle = UITableViewCellSelectionStyle.None;
			} else 
				RemoveTag (cell, 1);

			if (entry == null){
				SizeF size = ComputeEntryPosition (tv, cell);
				float yOffset = (GetHeight(this.GetContainerTableView(), IndexPath) - size.Height) / 2;
				float width = cell.ContentView.Bounds.Width - size.Width;

				entry = CreateTextView (new RectangleF (size.Width, yOffset, width, size.Height));
				entry.Changed += delegate {
					FetchValue ();
				};
				entry.Ended += delegate {
					FetchValue ();
					
					RootElement root = this.GetImmediateRootElement ();
					EntryElement focus = null;

					if (root == null)
						return;

					foreach (var s in root) {
						foreach (var e in s.Elements) {
							if (e == this) {
								focus = this;
							} else if (focus != null && e is EntryElement) {
								focus = e as EntryElement;
								break;
							}
						}

						if (focus != null && focus != this)
							break;
					}

					if (focus != this)
						focus.BecomeFirstResponder (true);
					else 
						focus.ResignFirstResponder (true);

					return;
				};

				entry.Started += delegate {
					MultilineEntryElement self = null;

					if (!ReturnKeyType.HasValue) {
						var returnType = UIReturnKeyType.Default;

						foreach (var e in (Parent as Section).Elements){
							if (e == this)
								self = this;
							else if (self != null && e is EntryElement)
								returnType = UIReturnKeyType.Next;
						}
						entry.ReturnKeyType = returnType;
					} else
						entry.ReturnKeyType = ReturnKeyType.Value;

					tv.ScrollToRow (IndexPath, UITableViewScrollPosition.Middle, true);
				};
			}
			if (becomeResponder){
				entry.BecomeFirstResponder ();
				becomeResponder = false;
			}
			entry.KeyboardType = KeyboardType;
			entry.AutocapitalizationType = AutocapitalizationType;
			entry.AutocorrectionType = AutocorrectionType;

			cell.TextLabel.Text = Caption;
			cell.ContentView.AddSubview (entry);
			return cell;
		}
		
		public virtual float GetHeight (UITableView tableView, NSIndexPath indexPath)
		{
			return GetTextViewHeight(tableView) + 10;
		}
		
		private float GetTextViewHeight(UITableView tableView)
		{
			SizeF size = new SizeF (280, float.MaxValue);
			string str = "M\n";
			for (int i = 1; i < Lines; i++)
				str += "M\n";
			return tableView.StringSize (str, font, size, UILineBreakMode.WordWrap).Height + 10;
		}
		
		/// <summary>
		///  Copies the value from the UITextField in the EntryElement to the
		//   Value property and raises the Changed event if necessary.
		/// </summary>
		public new void FetchValue ()
		{
			if (entry == null)
				return;

			var newValue = entry.Text;
			if (newValue == Value)
				return;

			Value = newValue;
		}
	}	
}

