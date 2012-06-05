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
	public class MultilineEntryElement : Element, IElementSizing 
    {
		UITextView entry;
		static UIFont font = UIFont.SystemFontOfSize (17);
        string val;
		
		/// <summary>
		/// Constructs an EntryElement with the given caption, placeholder and initial value.
		/// </summary>
		/// <param name="caption">
		/// The caption to use
		/// </param>
		/// <param name="value">
		/// Initial value.
		/// </param>
		public MultilineEntryElement (string caption, string value) : base(caption)
		{ 
            Caption = caption;
			Value = value;
            Lines = 1;
            AcceptReturns = false;
		}

        /// <summary>
        ///   The number of lines in the MultilineEntryElement
        /// </summary>
        public int Lines { get; set; }

        public bool AcceptReturns { get; set; }

        public string Value
        {
            get
            {
                return val;
            }
            set
            {
                val = value;
                if (entry != null && entry.Text != value)
                    entry.Text = value;
            }
        }
		
        public override string Summary ()
		{
			return Value;
		}
        
        public event EventHandler Changed;
		

		// 
		// Computes the X position for the entry by aligning all the entries in the Section
		//
		SizeF ComputeEntryPosition (UITableView tv, UITableViewCell cell)
		{
			SizeF max = new SizeF (-15, GetTextViewHeight(this.GetContainerTableView()));
	        if (Caption != null) {
				var size = tv.StringSize (Caption, font);
				if (size.Width > max.Width)
					max.Width = size.Width;
			}
			
			var entryAlignment = new SizeF (25 + Math.Min (max.Width, 160), max.Height);
			return entryAlignment;
		}

		protected virtual UITextView CreateTextView (RectangleF frame)
		{
			return new UITextView (frame) {
				AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleLeftMargin, 
				Text = Value ?? "",
				Tag = 1,
				Font = font,
				BackgroundColor = UIColor.FromRGB (247, 247, 247),
			};
		}

		static string cellkey = "MultilineEntryElement";

		protected override NSString CellKey {
			get {
				return new NSString(cellkey + Lines.ToString());
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
                if (AcceptReturns == false)
                    entry.ReturnKeyType = UIReturnKeyType.Done;
                else
                    entry.ReturnKeyType = UIReturnKeyType.Default;

                entry.KeyboardType = UIKeyboardType.Default;
                entry.AutocorrectionType = UITextAutocorrectionType.Default;

                entry.Changed += delegate {
					FetchValue ();
                    if (Changed != null)
                        Changed(this, new EventArgs());   
				};
				entry.Ended += delegate {
					FetchValue ();
                    if (Changed != null)
                        Changed(this, new EventArgs());
   					entry.ResignFirstResponder();
				};

				entry.Started += delegate {
					tv.ScrollToRow (IndexPath, UITableViewScrollPosition.Middle, true);
				};
			}

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
		///  Copies the value from the UITextView in the EntryElement to the
		//   Value property and raises the Changed event if necessary.
		/// </summary>
		public void FetchValue ()
		{
			if (entry == null)
				return;

			var newValue = entry.Text;
			if (newValue == Value)
				return;

            if (AcceptReturns == false)
            {
                // check for return key and resign responder if it is    
                if (newValue.IndexOf('\n') >= 0)
                {
                    entry.Text = entry.Text.Replace("\n", "");
                    entry.ResignFirstResponder();
                    return;
                }
            }

			Value = newValue;
		}
	}	
}

