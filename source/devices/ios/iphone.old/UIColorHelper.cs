using System;
using System.Globalization;
using MonoTouch.CoreGraphics;
using MonoTouch.UIKit;

namespace BuiltSteady.Zaplify.Devices.IPhone
{
	public static class UIColorHelper
	{
		public static UIColor FromString(string color)
		{
			if (color == null)
				return UIColor.Clear;
            if (color.StartsWith("Images/", true, CultureInfo.InvariantCulture))
                return UIColor.FromPatternImage(UIImageCache.GetUIImage(color));
                
			if (color.StartsWith("#"))
			{	// hex template
				// strip hash
				color = color.Substring(1);
				int red, green, blue, alpha;
				// handle #rrggbb
				if (color.Length == 6)
				{
					red = Convert.ToInt32(color.Substring(0, 2), 16);
					green = Convert.ToInt32(color.Substring(2, 2), 16);
					blue = Convert.ToInt32(color.Substring(4, 2), 16);
					return UIColor.FromRGB(red, green, blue);
				}
				// handle #AArrggbb
				if (color.Length == 8)
				{
					alpha = Convert.ToInt32(color.Substring(0, 2), 16);
					red = Convert.ToInt32(color.Substring(2, 2), 16);
					green = Convert.ToInt32(color.Substring(4, 2), 16);
					blue = Convert.ToInt32(color.Substring(6, 2), 16);
					return UIColor.FromRGBA(red, green, blue, alpha);
				}
				// can't recognize the format - return black
				return UIColor.Black;
			}
			else 
			{	// color string
				return FromName(color);
			}
		}
		
        /// <summary>
        /// Get UIColor from a string color name
        /// </summary>
        /// <param name="color"></param>
        /// <returns>UIColor corresponding to the string</returns>
        public static UIColor FromName(string color)
        {
            switch (color)
            {
                case "Black":
                    return UIColor.Black;
                case "White":
                    return UIColor.White;
                case "Blue":
                    return UIColor.Blue;
                case "Brown":
                    return UIColor.Brown;
                case "Green":
                    return UIColor.Green;
                case "Orange":
                    return UIColor.Orange;
                case "Purple":
                    return UIColor.Purple;
                case "Red":
                    return UIColor.Red;
                case "Yellow":
                    //return UIColor.Yellow;
					// yellow doesn't show up well on white
                    return UIColor.Orange;
                case "Gray":
                    return UIColor.Gray;
            }
            return UIColor.Gray;
        }
	}
}

