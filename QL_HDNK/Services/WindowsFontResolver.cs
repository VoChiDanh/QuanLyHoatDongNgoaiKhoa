using System;
using System.IO;
using PdfSharp.Fonts;

namespace QL_HDNK.Services
{
    public class WindowsFontResolver : IFontResolver
    {
        private const string RegularFace = "Arial#Regular";
        private const string BoldFace = "Arial#Bold";

        public byte[] GetFont(string faceName)
        {
            var fontsPath = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            var fileName = faceName == BoldFace ? "arialbd.ttf" : "arial.ttf";
            var fontPath = Path.Combine(fontsPath, fileName);

            if (!File.Exists(fontPath))
            {
                fontPath = Path.Combine(fontsPath, "segoeui.ttf");
            }

            return File.ReadAllBytes(fontPath);
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            return new FontResolverInfo(isBold ? BoldFace : RegularFace);
        }
    }
}
