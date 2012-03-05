using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using MonoTouch.Dialog;
using MonoTouch.UIKit;
using MonoTouch.Foundation;

namespace BuiltSteady.Zaplify.Devices.IPhone.Controls
{
	public class Button
	{
		public string Caption { get; set; }
		public EventHandler Clicked { get; set; }
		public string Background { get; set; }
		public UIColor TextColor { get; set; }
		public UIFont Font { get; set; }
		public UIButton ButtonReference { get; set; }
	}
	
	public class ButtonListElement : Element, IEnumerable
	{
		private const float minSpacing = 5f;
        private const float defaultMargin = 5f;
        
        public List<Button> Buttons = new List<Button>();
		
		public float? Margin { get; set; }

		public ButtonListElement () : base (null) { }

		public void Add (Button button)
		{
			if (button == null)
				return;

			Buttons.Add (button);
		}
  
		public int AddAll (IEnumerable<Button> buttons)
		{
			int count = 0;
			foreach (var b in buttons){
				Add (b);
				count++;
			}
			return count;
		}

        public void Add(IEnumerable<Button> buttons) 
        {
            AddAll(buttons);
        }
     

		public override UITableViewCell GetCell (UITableView tv)
		{
			// always create a new cell instead of dequeuing a proper cell - this is because
			// each ButtonListElement may have a different number of buttons and so will have different
			// subviews - therefore not recyclable
			var cell = new UITableViewCell (UITableViewCellStyle.Default, "ButtonListElementCell");
			cell.Accessory = UITableViewCellAccessory.None;
			cell.SelectionStyle = UITableViewCellSelectionStyle.None;
			cell.BackgroundColor = UIColor.FromPatternImage(new UIImage("Images/background.png"));
			
			// if no child buttons, return a blank cell
			if (Buttons.Count == 0)
				return cell;
			
			float margin = Margin ?? defaultMargin;  
            float spacing = margin > minSpacing ? margin : minSpacing;
            
			// compute button width: 
			//   [margin] { [button] { [buttonspacing] [ button] }* [margin] } 
			// the total available width is the bounds minus the margins and button spacing (minus 20 fudge factor 
			// since CocoaTouch seems to pad the cell by 10 pixels on each side)
			// divide this available width by the number of buttons and deduct the margin 
			// to get the button width
            float availableWidth = cell.Bounds.Width - 20f - 2 * margin - (Buttons.Count - 1) * spacing;
			float buttonWidth = Convert.ToSingle(Math.Round(availableWidth / Buttons.Count));  
			float buttonHeight = (cell.Bounds.Height) - 2 * margin;
			
			int x = 0;
			foreach (var btn in Buttons)
			{
				UIButton button = UIButton.FromType(UIButtonType.RoundedRect);
   				button.Frame = new RectangleF(margin + x * (buttonWidth + spacing), margin, buttonWidth, buttonHeight);
   				button.SetTitle(btn.Caption, UIControlState.Normal);
				if (btn.Background != null)
				{
					// set the background image, and also change the font color to white 
					button.SetBackgroundImage(new UIImage(btn.Background), UIControlState.Normal);
					button.SetTitleColor(btn.TextColor ?? UIColor.White, UIControlState.Normal);
					button.Font = btn.Font ?? UIFont.BoldSystemFontOfSize(17);
				}
				Button savedButton = btn;
                if (btn.Clicked != null)
                {   
					button.TouchUpInside += (s, e) => { savedButton.Clicked(savedButton, e); };
                }
				// retain a reference to the button (otherwise it somehow falls out of scope and gets GC'd 
				// causing a SIGSEGV when the event handler is invoked)
				btn.ButtonReference = button;
				cell.ContentView.AddSubview(button);				
				x++;
			}

			return cell;
		}

		public override string Summary ()
		{
			//return Caption;
			return "foo";
		}
		
		#region IEnumerable implementation
		
		public IEnumerator GetEnumerator ()
		{
			foreach (var b in Buttons)
				yield return b;
		}

		public int Count {
			get {
				return Buttons.Count;
			}
		}

		public Button this [int idx] {
			get {
				return Buttons [idx];
			}
		}

		public void Clear ()
		{
			Buttons = new List<Button> ();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing){
				Clear ();
				Buttons = null;
			}
			base.Dispose (disposing);
		}
		
		#endregion
	}
}

