using System;
using System.IO;

namespace BuiltSteady.Zaplify.Devices.ClientHelpers
{
    public static class AppResourcesHelper
    {
        /// <summary>
        /// Get a resource stream for a resource
        /// </summary>
        /// <param name="stream">Stream to deserialize</param>
        /// <returns>Resource stream</returns>
        public static Stream GetResourceStream(string resourceName)
        {
#if IOS
            return null;
#else
            System.Windows.Resources.StreamResourceInfo aboutFile =
              System.Windows.Application.GetResourceStream(new Uri("/BuiltSteady.Zaplify.Devices.WinPhone;component/" + resourceName, UriKind.Relative));
            Stream stream = aboutFile.Stream;
            return stream;
#endif
		}
    }
}
