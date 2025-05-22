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
        public static SKPaint WhitePaint = new()
        {
            Color = SKColors.White,
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };
        public static SKPaint TransparentPaint = new()
        {
            Color = SKColor.Parse("#4FFFFFFF"),
            StrokeWidth = 0.6f,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };
        public static SKPaint EditorPaint;
        public static SKPaint DarkOrangePaint = new()
        {
            Color = SKColors.DarkOrange,
            StrokeWidth = 2,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };

        public static SKTypeface? GetTypeface(string fullFontName)
        {
            SKTypeface result;

            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("LogosMap.resources.fonts." + fullFontName);
            if (stream == null)
                return null;

            result = SKTypeface.FromStream(stream);
            return result;
        }
    }
}
