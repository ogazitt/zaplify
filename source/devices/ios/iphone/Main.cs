using System;
using System.Collections.Generic;
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using BuiltSteady.Zaplify.Devices.ClientHelpers;
using BuiltSteady.Zaplify.Devices.IPhone.Controls;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	public class Application
	{
		// This is the main entry point of the application.
		static void Main (string[] args)
		{
			// if you want to use a different Application Delegate class from "App"
			// you can specify it here.
			try
            {
                UIApplication.Main (args, null, "App");
            }
            catch (Exception ex)
            {
                TraceHelper.AddMessage(String.Format("Unhandled Exception in iOS client; ex: {0}\nStackTrace: {1}", ex.Message, ex.StackTrace));
                TraceHelper.StoreCrashReport();
            }
		}
	}
}
