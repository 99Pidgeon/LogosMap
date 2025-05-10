using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LogosMap
{
    public class Util
    {
        public static SKTypeface? GetTypeface(string fullFontName)
        {
            SKTypeface result;

            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("LogosMap.Fonts." + fullFontName);
            if (stream == null)
                return null;

            result = SKTypeface.FromStream(stream);
            return result;
        }
    }
}
