using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StinClasses
{
    public static class XmlHelper
    {
        static readonly string RegexParameter = @"(\A|\s*)\{(\w+)\}(\s*|\z)";
        static string ReadTemplateFile(string FileName)
        {
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates", FileName + ".xml");
            return File.ReadAllText(file, Encoding.UTF8);
        }
        static string RemoveReplaceReservedChars(this object text)
        {
            var sb = new StringBuilder(text.ToString());
            for (int i = 0; i < 32; i++)
                sb.Replace(Convert.ToString((char)i), "");
            sb.Replace("\"", "&quot;");
            sb.Replace("&", "&amp;");
            sb.Replace(">", "&gt;");
            sb.Replace("<", "&lt;");
            sb.Replace("<", "&lt;");
            sb.Replace("'", "&apos;");
            return sb.ToString();
        }
        public static byte[] CreateFromTemplate(this object data, string docName)
        {
            string xml = CreateFromTemplate(docName, data);
            return Encoding.UTF8.GetBytes(xml);
        }
        public static string CreateFromTemplate(string docName, object data)
        {
            string template = ReadTemplateFile(docName);
            var xmlData = new StringBuilder(template);
            var sb = new StringBuilder();
            var variants = Regex.Matches(template, RegexParameter)
                .Cast<Match>()
                .Select(m => m.Value.Trim());
            var XmlParams = data.GetType().GetProperties();
            foreach (string variant in variants)
            {
                var entry = XmlParams.Where(x => "{" + x.Name + "}" == variant).Select(x => x.GetValue(data, null)).FirstOrDefault();
                if (entry != null)
                {
                    if ((entry is IList entryList) && entry.GetType().IsGenericType)
                    {
                        if (entryList.Count > 0)
                        {
                            string rowTemplate = ReadTemplateFile(docName + "_" + variant.TrimStart('{').TrimEnd('}'));
                            var rowVariants = Regex.Matches(rowTemplate, RegexParameter)
                                .Cast<Match>()
                                .Select(m => m.Value.Trim());
                            var XmlRowParams = entryList[0].GetType().GetProperties();
                            sb.Clear();
                            foreach (var p in entryList)
                            {
                                var rowSb = new StringBuilder(rowTemplate);
                                foreach (string s in rowVariants)
                                {
                                    var value = XmlRowParams.Where(x => "{" + x.Name + "}" == s)
                                        .Select(x => x.GetValue(p, null))
                                        .FirstOrDefault();
                                    if (value != null)
                                        rowSb.Replace(s, value.RemoveReplaceReservedChars());
                                }
                                sb.AppendLine(rowSb.ToString());
                            }
                            if (sb.Length > 0)
                                xmlData.Replace(variant, sb.ToString());
                        }
                        else
                            xmlData.Replace(variant, "");
                    }
                    else
                        xmlData.Replace(variant, entry.RemoveReplaceReservedChars());
                }
            }
            return xmlData.ToString();
        }
    }
}
