using PdfSharpCore.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PdfHelper
{
    class CustomFontResolver : IFontResolver
    {
        string IFontResolver.DefaultFontName => "Arial";
        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            // Ignore case of font names.
            var name = familyName.ToLower();
            // Deal with the fonts we know.
            switch (name)
            {
                case "arial": return new FontResolverInfo("Arial#");
                case "arial narrow":
                    if (isBold && isItalic)
                        return new FontResolverInfo("ArialNarrow#BI");
                    else if (isBold)
                        return new FontResolverInfo("ArialNarrow#B");
                    else if (isItalic)
                        return new FontResolverInfo("ArialNarrow#I");
                    return new FontResolverInfo("ArialNarrow#");
                case "bahnschrift": return new FontResolverInfo("Bahnschrift#");
                case "bahnschrift condensed": return isBold ? new FontResolverInfo("Bahnschrift#C_Bold") : new FontResolverInfo("Bahnschrift#C");
                case "bahnschrift condensed light": return new FontResolverInfo("Bahnschrift#CL");
                case "bahnschrift light": return new FontResolverInfo("Bahnschrift#L");
                case "bahnschrift light semicondensed": return isBold ? new FontResolverInfo("Bahnschrift#LSC_Bold") : new FontResolverInfo("Bahnschrift#LSC");
                case "code 128": return new FontResolverInfo("Code128#");
                default: return PlatformFontResolver.ResolveTypeface(familyName, isBold, isItalic);
            };
        }

        /// <summary>
        /// Return the font data for the fonts.
        /// </summary>
        public byte[] GetFont(string faceName)
        {
            return faceName switch
            {
                "Arial#" => CustomFontHelper.Arial,
                "ArialNarrow#" => CustomFontHelper.ArialNarrow,
                "ArialNarrow#B" => CustomFontHelper.ArialNarrowBold,
                "ArialNarrow#I" => CustomFontHelper.ArialNarrowItalic,
                "ArialNarrow#BI" => CustomFontHelper.ArialNarrowBoldItalic,
                "Bahnschrift#" => CustomFontHelper.Bahnschrift,
                "Bahnschrift#C" => CustomFontHelper.BahnschriftCondensed,
                "Bahnschrift#C_Bold" => CustomFontHelper.BahnschriftCondensedBold,
                "Bahnschrift#CL" => CustomFontHelper.BahnschriftCondensedLight,
                "Bahnschrift#L" => CustomFontHelper.BahnschriftLight,
                "Bahnschrift#LSC" => CustomFontHelper.BahnschriftLightSemiCondensed,
                "Bahnschrift#LSC_Bold" => CustomFontHelper.BahnschriftLightSemiCondensedBold,
                "Code128#" => CustomFontHelper.Code128,
                _ => throw new NotImplementedException()
            };
        }
    }

    /// <summary>
    /// Helper class that reads font data from embedded resources.
    /// </summary>
    public static class CustomFontHelper
    {
        // Tip: I used JetBrains dotPeek to find the names of the resources (just look how dots in folder names are encoded).
        // Make sure the fonts have compile type "Embedded Resource". Names are case-sensitive.
        public static byte[] Arial => LoadFontData("PdfHelper.Fonts.arial.ttf");
        public static byte[] ArialNarrow => LoadFontData("PdfHelper.Fonts.arialn.ttf");
        public static byte[] ArialNarrowBold => LoadFontData("PdfHelper.Fonts.arialnb.ttf");
        public static byte[] ArialNarrowItalic => LoadFontData("PdfHelper.Fonts.arialni.ttf");
        public static byte[] ArialNarrowBoldItalic => LoadFontData("PdfHelper.Fonts.arialnbi.ttf");
        public static byte[] Bahnschrift => LoadFontData("PdfHelper.Fonts.bahnschrift.ttf");
        public static byte[] BahnschriftCondensed => LoadFontData("PdfHelper.Fonts.bahnschrift-Condensed.ttf");
        public static byte[] BahnschriftCondensedBold => LoadFontData("PdfHelper.Fonts.bahnschrift-BoldCondensed.ttf");
        public static byte[] BahnschriftCondensedLight => LoadFontData("PdfHelper.Fonts.bahnschrift-CondensedLight.ttf");
        public static byte[] BahnschriftLight => LoadFontData("PdfHelper.Fonts.bahnschrift-LightCondensed.ttf");
        public static byte[] BahnschriftLightSemiCondensed => LoadFontData("PdfHelper.Fonts.bahnschrift-LightSemiCondensed.ttf");
        public static byte[] BahnschriftLightSemiCondensedBold => LoadFontData("PdfHelper.Fonts.bahnschrift-BoldSemiCondensed.ttf");
        public static byte[] Code128 => LoadFontData("PdfHelper.Fonts.7fonts.ru_code128.ttf");

        /// <summary>
        /// Returns the specified font from an embedded resource.
        /// </summary>
        static byte[] LoadFontData(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (Stream stream = assembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                    throw new ArgumentException("No resource with name " + name);

                int count = (int)stream.Length;
                byte[] data = new byte[count];
                stream.Read(data, 0, count);
                return data;
            }
        }
    }
}
