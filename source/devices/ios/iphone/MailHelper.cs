using System;
using MonoTouch.MessageUI;
using MonoTouch.UIKit;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
    public class MailHelper
    {
        MFMailComposeViewController mail;
        UIViewController controller;

        public string Subject { get; set; }
        public string Body { get; set; }

        public event EventHandler<EventArgs> OnFinished;

        public MailHelper(UIViewController c)
        {
            controller = c;
        }

        public void SendMail() 
        {
            if (MFMailComposeViewController.CanSendMail) 
            {
                mail = new MFMailComposeViewController();
                mail.SetSubject(Subject);
                mail.SetMessageBody(Body, false);
                mail.Finished += (sender, e) => 
                {
                    var finished = OnFinished;
                    if (finished != null)
                        finished(this, EventArgs.Empty);
                    mail.Dispose();
                    mail = null;
                    controller.NavigationController.DismissModalViewControllerAnimated(true);
                };
                controller.NavigationController.PresentModalViewController(mail, true);
            }
        }
    }
}
