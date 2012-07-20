using System;
using System.Drawing;
using MonoTouch.Dialog;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

namespace BuiltSteady.Zaplify.Devices.IPhone.Controls
{
    public class ThemedRootElement : RootElement 
    {    
        public ThemedRootElement(string caption) : base (caption)
        {
        }
        
        public ThemedRootElement(string caption, Func<RootElement, UIViewController> createOnSelected) : base (caption, createOnSelected)
        {
        }
        
        public ThemedRootElement(string caption, int section, int element) : base (caption, section, element)
        {
        }
        
        public ThemedRootElement(string caption, Group group) : base (caption, group)
        {
        }
        
        protected override void PrepareDialogViewController(UIViewController dvc)
        {
            base.PrepareDialogViewController(dvc);
            var prepare = OnPrepare;
            if (prepare != null)
                prepare(this, new PrepareEventArgs() { ViewController = (DialogViewController) dvc });
            dvc.View.BackgroundColor = UIColorHelper.FromString(App.ViewModel.Theme.PageBackground);
            if (dvc.NavigationController != null)
                dvc.NavigationController.NavigationBar.TintColor = UIColorHelper.FromString(App.ViewModel.Theme.ToolbarBackground);
        }
 
        public event EventHandler<PrepareEventArgs> OnPrepare;         
    }
    
    public class PrepareEventArgs : EventArgs
    {
        public DialogViewController ViewController { get; set; }
    }
}
