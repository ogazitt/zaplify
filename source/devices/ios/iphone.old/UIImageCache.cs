using System;
using System.Collections.Generic;
using MonoTouch.UIKit;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
    public class UIImageCache
    {
        static Dictionary<string, UIImage> imageDict = new Dictionary<string, UIImage>();

        public static UIImage GetUIImage(string imageName)
        {
            UIImage image = null;
            if (imageDict.TryGetValue(imageName, out image) == false)
            {
                lock(imageDict)
                {
                    image = new UIImage(imageName);
                    imageDict[imageName] = image;
                }
            }
            return image;
        }
    }
}

