// WARNING
//
// This file has been generated automatically by MonoDevelop to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using MonoTouch.Foundation;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	[Register ("SettingsViewController")]
	partial class SettingsViewController
	{
		[Outlet]
		MonoTouch.UIKit.UITextField Username { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField Password { get; set; }

		[Outlet]
		MonoTouch.UIKit.UITextField Email { get; set; }

		[Outlet]
		MonoTouch.UIKit.UISwitch MergeCheckbox { get; set; }

		[Action ("CreateUserButton_Click:")]
		partial void CreateUserButton_Click (MonoTouch.Foundation.NSObject sender);

		[Action ("SyncUserButton_Click:")]
		partial void SyncUserButton_Click (MonoTouch.Foundation.NSObject sender);
	}
}
