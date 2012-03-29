using System;
using MonoTouch.UIKit;
using MonoTouch.Foundation;

namespace BuiltSteady.Zaplify.Devices.IPhone.Controls
{
	public enum MessageBoxResult
    {
        OK = 1,
        Cancel = 2,
    }
	
    public enum MessageBoxButton
    {
		OK = 0,
        OKCancel = 1,
    }
	
    public class MessageBox
    {
        public static MessageBoxResult Show(string messageBoxText)
		{
			return Show (messageBoxText, "", MessageBoxButton.OK);
		}

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
		{
			UIAlertView messageBox;
			if (button == MessageBoxButton.OKCancel)
				messageBox = new UIAlertView (caption, messageBoxText, null, "OK", "Cancel");
			else
				messageBox = new UIAlertView (caption, messageBoxText, null, "OK");
            messageBox.Show ();

			int clicked = -1;

			messageBox.Clicked += (s, buttonArgs) => 
			{
				clicked = buttonArgs.ButtonIndex;
			};    
			while (clicked == -1)
			{
    			NSRunLoop.Current.RunUntil (NSDate.FromTimeIntervalSinceNow (0.5));
                if (messageBox.BecomeFirstResponder() == false)
                    break;
			}
			
			if (clicked == 0)
				return MessageBoxResult.OK;
			else
				return MessageBoxResult.Cancel;
		}
    }

}

