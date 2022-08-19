using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

namespace StinWeb.Models.DataManager
{
    public static class МодульПечати
    {
        private static readonly string RegexParameter = @"(\A|\s*)\{(\w+)\}(\s*|\z)";
        public static string CreateOrUpdateHtmlPrintPage(this string html, string docName, object ДанныеДляПечати, bool needNewPage = true)
        {
            if (string.IsNullOrEmpty(html))
                return ReadTemplateFile("PrintPage.htm").InsertStyle(docName).InsertDiv(docName, ДанныеДляПечати);
            else
                return needNewPage ? html.InsertSplitter().InsertStyle(docName).InsertDiv(docName, ДанныеДляПечати) : html.InsertPageSeparator().InsertDiv(docName, ДанныеДляПечати);
        }
        public static string ReadTemplateFile(string FileName)
        {
            string file = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "templates", FileName);
            return File.ReadAllText(file, Encoding.GetEncoding(1251));
        }
        public static string InsertStyle(this string html, string docName)
        {
            return html.Insert(html.IndexOf("</head>"), ReadTemplateFile(docName + "_Styles.htm"));
        }
        public static string InsertSplitter(this string html)
        {
            return html.Insert(html.IndexOf("</body>"), ReadTemplateFile("PageSplitter.htm"));
        }
        public static string InsertPageSeparator(this string html)
        {
            return html.Insert(html.IndexOf("</body>"), ReadTemplateFile("PageSeparator.htm"));
        }
        public static string InsertDiv(this string html, string docName, object ДанныеДляПечати)
        {
            string div = ReadTemplateFile(docName + "_Div.htm");
            var variants = Regex.Matches(div, RegexParameter)
                .Cast<Match>()
                .Select(m => m.Value.Trim());
            Type ТипДанныеПечати = ДанныеДляПечати.GetType();
            var ПараметрыПечати = ТипДанныеПечати.GetProperties();
            foreach (string variant in variants)
            {
                var entry = ПараметрыПечати.Where(x => "{" + x.Name + "}" == variant).Select(x => x.GetValue(ДанныеДляПечати, null)).FirstOrDefault();
                if (entry != null)
                {
                    if (entry is IList && entry.GetType().IsGenericType)
                    {
                        if ((entry as IList).Count > 0)
                        {
                            string строкаTemplate = ReadTemplateFile(docName + "_" + variant.TrimStart('{').TrimEnd('}') + ".htm");
                            var variantsСтроки = Regex.Matches(строкаTemplate, RegexParameter)
                                .Cast<Match>()
                                .Select(m => m.Value.Trim());
                            Type ТипДанныеТаблЧасти = (entry as IList)[0].GetType();
                            var ПараметрыТаблЧасти = ТипДанныеТаблЧасти.GetProperties();
                            string таблЧасть = "";
                            foreach (var p in (entry as IList))
                            {
                                string строка = строкаTemplate;
                                foreach (string s in variantsСтроки)
                                {
                                    var значение = ПараметрыТаблЧасти.Where(x => "{" + x.Name + "}" == s)
                                        .Select(x => x.GetValue(p, null))
                                        .FirstOrDefault();
                                    if (значение != null)
                                        строка = строка.Replace(s, значение.ToString());
                                }
                                таблЧасть += строка;
                            }
                            if (!string.IsNullOrEmpty(таблЧасть))
                            {
                                div = div.Replace(variant, таблЧасть);
                            }
                        }
                        else
                            div = div.Replace(variant, "");
                    }
                    else
                        div = div.Replace(variant, entry.ToString());
                }
            }
            return html.Insert(html.IndexOf("</body>"), div);
        }
    }
}
