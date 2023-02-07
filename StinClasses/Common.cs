using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Intrinsics.Arm;
using System.Drawing.Imaging;

namespace StinClasses
{
    public static class Common
    {
        public static readonly DateTime min1cDate = new DateTime(1753, 1, 1);
        public static readonly decimal zero = 0;

        public static readonly string ПустоеЗначение = "     0   ";
        public static readonly string ПустоеЗначениеИд13 = "   0     0   ";
        public static readonly string ПустоеЗначениеИд23 = "U                      ";
        public static readonly string ПустоеЗначениеTSP = "   ";

        public static readonly string НумераторПКО = "3376";
        public static readonly string НумераторТОРГ12 = "5811";
        public static readonly string НумераторРКО = "9522";
        public static readonly string НумераторСчФактуры = "13276";
        public static readonly string НумераторВЫДАЧАРЕМОНТ = "11331";
        public static readonly string НумераторИНВЕНТАРИЗАЦИЯ = "11387";

        public static readonly string ВалютаРубль = "     1   ";

        public static readonly string SkladEkran = "     DS  "; //Экран
        public static readonly string SkladGastello = "     7S  "; //Гастелло-Инструмент
        public static readonly string SkladCenter = "     1   "; //ЦентральныйСклад
        public static readonly string SkladOtstoy = "     6S  "; //Отстой
        public static readonly string SkladDomSad = "     9S  "; //Дом&Сад (Заводское)

        public static readonly string СтавкаНПбезНалога = "     1   ";

        public static readonly string ТипЦенРозничный = "     4   ";

        public static readonly string FirmaStPlus = "     1   ";
        public static readonly string FirmaIP = "     3S  ";
        public static readonly string FirmaSS = "     4S  ";

        public static readonly string UserRobot = "    20D  ";

        public static readonly string УслугаДоставкиId = "  237JD  ";

        public static readonly Dictionary<string, string> СтавкиНДС = new Dictionary<string, string>()
        {
            {"    I9   ","Без НДС" },
            {"    I8   ","10%" },
            {"    I7   ","20%" },
            {"   6F2   ","18%" },
            {"   9YV   ","0%" },
        };
        public static readonly Dictionary<string, string> КодОперации = new Dictionary<string, string>()
        {
            {"    C2   ","Поступление ТМЦ (купля-продажа)" },
            {"   7XX   ","Поступление ТМЦ (купля-продажа)" },//без списания остатков
            {"   7G6   ","Отчет по ОтветХранению" },
            {"   4XX   ","Передача в розницу" },
            {"   2GU   ","Реализация (розница)" },
            {"   3AA   ","Реализация (розница, ЕНВД)" },
            {"   16S   ","Реализация (купля-продажа)" },
            {"   7XW   ","Реализация (купля-продажа)" },//без списания остатков
            {"   8KE   ","Согласие на платный ремонт" },
            {"   8KF   ","Отказ от платного ремонта" },
            {"   7OD   ","Выдача ЗЧ мастеру" },
            {"   7OE   ","Возврат ЗЧ от мастера" },
            {"   1MN   ","Оплата от покупателя" },
            {"   99B   ","Отмена набора" },
            {"   15Q   ","Внутреннее перемещение" },
            {"   6PJ   ","Внутреннее перемещение с доставкой" },
        };
        public static string GetКодОперацииId(string name)
        {
            return КодОперации.Where(x => x.Value == name).Select(x => x.Key).SingleOrDefault();
        }
        public static string GetКодОперацииName(string id)
        {
            return КодОперации.Where(x => x.Key == id).Select(x => x.Value).SingleOrDefault();
        }
        public static readonly Dictionary<int, string> ВидыДокументов = new Dictionary<int, string>()
        {
            {1582,"Поступление ТМЦ" },
            {13369,"Корректировка Сроков Оплаты" },

            {9074,"ПродажаКасса" },
            {3114,"ОтчетККМ" },
            {3046,"ЧекККМ" },
            {13849,"ОплатаЧерезЮКасса" },

            {9899,"Прием в ремонт" },
            {13737,"ИзменениеСтатуса" },
            {10080,"Перемещение Изделий" },
            {10995,"На Диагностику" },
            {11037,"Результат диагностики" },
            {11101,"Согласие" },
            {9927,"Запрос запчастей" },
            {9947,"Проведенные работы" },
            {10457,"Завершение ремонта" },
            {10054,"Выдача из ремонта" },

            {1628,"Перемещение ТМЦ" },
            {10062,"Перемещение в Мастерскую" },

            {12747,"Предварительная заявка" },
            {11948,"Набор" },
        };
        public static readonly Dictionary<string, string> ВидыОперации = new Dictionary<string, string>()
        {
            {"   3O1   ","Неподтвержденный счет" },
            {"   3O2   ","Счет на оплату" },
            {"   3O3   ","Заявка (на согласование)" },
            {"   AD6   ","Заявка (согласованная)" },
            {"   APG   ","Заявка (одобренная)" },
            {"   AI8   ","Заявка дилера" },
        };
        public static readonly Dictionary<string, string> СтатусПартии = new Dictionary<string, string>()
        {
            {"","Оформление..." },
            {"   7OH   ","Принят в ремонт" },
            {"   7OI   ","Готов к выдаче" },
            {"   8IM   ","На диагностике" },
            {"   8KG   ","Экспертиза завершена" },
            {"   8KH   ","Отказ от платного ремонта" },
            {"   8IN   ","Ожидание запасных частей" },
            {"   8IO   ","В ремонте" },
            {"   ALM   ","Сортировка" },
            {"   ALS   ","Претензия на рассмотрении" },
            {"   ALT   ","Претензия отклонена" },
            {"   ALU   ","Замена по претензии" },
            {"   ALV   ","Восстановление по претензии" },
            {"   AML   ","Возврат денег по претензии" },
            {"   AMM   ","Доукомплектация по претензии" },
            {"   AMS   ","Диагностика претензии" },
            {"   2TB   ","Товар (купленный)" },
            {"   2TC   ","Товар (принятый)" },
            {"   4XZ   ","Товар (в рознице)" },
            {"   60N   ","Услуга" },
        };
        public static string GetСтатусПартииId(string name)
        {
            return СтатусПартии.Where(x => x.Value == name).Select(x => x.Key).SingleOrDefault();
        }
        public static string GetСтатусПартииName(string id)
        {
            return СтатусПартии.Where(x => x.Key == id).Select(x => x.Value).SingleOrDefault();
        }
        public static readonly Dictionary<string, string> СпособыОтгрузки = new Dictionary<string, string>()
        {
            {"   80N   ","Самовывоз" },
            {"   80O   ","Доставка" },
            {"   8MN   ","Межгород доставка" },
            {"   A49   ","Дальняя доставка" },
            {"   A9W   ","Доставка 150 руб." },
            {"   A9X   ","Доставка 500 руб." },
        };
        public static readonly Dictionary<string, string> СпособыРезервирования = new Dictionary<string, string>()
        {
            {"   64P   ","Резервировать только по заказам" },//Заказы
            {"   64Q   ","Резервировать только из текущего остатка" },//Остаток
            {"   64R   ","Резервировать по заказам и из текущего остатка" },//ЗаказыОстаток
            {"   64S   ","Резервировать из текущего остатка и по заказам" },//ОстатокЗаказы
        };
        public static readonly Dictionary<string, string> РежимыККТ = new Dictionary<string, string>()
        {
            {"   2CO   ","ФР" },
            {"   2CQ   ","OnLine" },
            {"   2CP   ","OffLine" }
        };
        public static long Decode36(this string input)
        {
            string CharList = "0123456789abcdefghijklmnopqrstuvwxyz";
            var reversed = input.ToLower().Reverse();
            long result = 0;
            int pos = 0;
            foreach (char c in reversed)
            {
                result += CharList.IndexOf(c) * (long)Math.Pow(36, pos);
                pos++;
            }
            return result;
        }
        public static string Encode36(this long input)
        {
            if (input < 0)
                return "";
            else
            {
                string CharList = "0123456789abcdefghijklmnopqrstuvwxyz";
                char[] clistarr = CharList.ToUpper().ToCharArray();
                var result = new Stack<char>();
                while (input != 0)
                {
                    result.Push(clistarr[input % 36]);
                    input /= 36;
                }
                return new string(result.ToArray());
            }
        }
        public static string EncodeHexString(this string value)
        {
            return Convert.ToHexString(Encoding.UTF8.GetBytes(value));
        }
        public static string DecodeHexString(this string value)
        {
            return Encoding.UTF8.GetString(Convert.FromHexString(value));
        }
        public static string TryDecodeHexString(this string value)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromHexString(value));
            }
            catch
            {
                //Console.WriteLine(value);
                return "";
            }
        }
        public static string EncodeDecString(this string value)
        {
            return string.Join('.', value.Select(c => (short)c));
        }
        public static string TryDecodeDecString(this string value)
        {
            try
            {
                var decs = value.Split('.', StringSplitOptions.RemoveEmptyEntries).Select(x => (char)(short.Parse(x)));
                return string.Join("", decs);
            }
            catch
            {
                return "";
            }
        }
        public static string Encode(this string value, EncodeVersion version)
        {
            return version switch
            {
                EncodeVersion.None => value,
                EncodeVersion.Hex => value.EncodeHexString(),
                EncodeVersion.Dec => value.EncodeDecString(),
                _ => value
            };
        }
        public static string Decode(this string value, EncodeVersion version)
        {
            return version switch
            {
                EncodeVersion.None => value,
                EncodeVersion.Hex => value.TryDecodeHexString(),
                EncodeVersion.Dec => value.TryDecodeDecString(),
                _ => value
            };
        }
        public static DateTime ToDateTime(this string DateTimeIddoc)
        {
            DateTime result = new DateTime();
            if (DateTimeIddoc.Length >= 14 && DateTime.TryParseExact(DateTimeIddoc.Substring(0, 8), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                var seconds = Decode36(DateTimeIddoc.Substring(8, 6).Trim()) / 10000;
                result = result.AddSeconds(seconds);
            }
            return result;
        }
        public static string FormatTo1CId(this string aValue)
        {
            if (aValue.Length >= 9)
                return aValue;
            return aValue.PadLeft(7).PadRight(9);
        }
        public static bool ValueChanged(this string aString, string incomingString)
        {
            if (string.IsNullOrWhiteSpace(incomingString))
                return false;
            else
                return aString != incomingString;
        }
        public static string ConditionallyAppend(this string Value, string AddString, string separator = ", ")
        {
            if (string.IsNullOrEmpty(AddString))
                return Value;
            if (!string.IsNullOrWhiteSpace(Value) && Value.Length > 0)
                return Value + separator + AddString;
            return AddString;
        }
        public static void ConditionallyAppend(this StringBuilder sb, string add, string separator = ", ")
        {
            if (sb == null) sb = new StringBuilder();
            if (string.IsNullOrEmpty(add)) return;
            if (sb.Length > 0) sb.Append(separator);
            sb.Append(add);
        }
        public static string StringLimit(this string value, int limit)
        {
            if (value == null)
                return "";
            else if (value.Length > limit)
                return value.Substring(0, limit);
            else
                return value;
        }
        public static double WithLimits(this double value, double min, double max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
        public static async Task<byte[]> CreateZip(this byte[] bytes, string fileName)
        {
            byte[] result;

            using (var packageStream = new System.IO.MemoryStream())
            {
                using (var archive = new System.IO.Compression.ZipArchive(packageStream, System.IO.Compression.ZipArchiveMode.Create, true))
                {
                    var zipFile = archive.CreateEntry(fileName, System.IO.Compression.CompressionLevel.Fastest);
                    using (var zipEntryStream = zipFile.Open())
                    {
                        await zipEntryStream.WriteAsync(bytes, 0, bytes.Length);
                    }
                }
                result = packageStream.ToArray();
            }
            return result;
        }
        public static string Склонять(this string data, int num)
        {
            data = data.ToLower();
            if (num > 100)
            {
                num = num % 100;
            }
            if (num >= 0 && num <= 20)
            {
                if (num == 0)
                {
                    switch (data)
                    {
                        case "year": return "лет";
                        case "month": return "месяцев";
                        case "week": return "недель";
                        case "day": return "дней";
                        case "hour": return "часов";
                        default: return "";
                    }
                }
                else if (num == 1)
                {
                    switch (data)
                    {
                        case "year": return "год";
                        case "month": return "месяц";
                        case "week": return "неделя";
                        case "day": return "день";
                        case "hour": return "час";
                        default: return "";
                    }
                }
                else if (num >= 2 && num <= 4)
                {
                    switch (data)
                    {
                        case "year": return "года";
                        case "month": return "месяца";
                        case "week": return "недели";
                        case "day": return "дня";
                        case "hour": return "часа";
                        default: return "";
                    }
                }
                else if (num >= 5 && num <= 20)
                {
                    switch (data)
                    {
                        case "year": return "лет";
                        case "month": return "месяцев";
                        case "week": return "недель";
                        case "day": return "дней";
                        case "hour": return "часов";
                        default: return "";
                    }
                }
            }
            else if (num > 20)
            {
                string str;
                str = num.ToString();
                string n = str[str.Length - 1].ToString();
                int m = Convert.ToInt32(n);
                if (m == 0)
                {
                    switch (data)
                    {
                        case "year": return "лет";
                        case "month": return "месяцев";
                        case "week": return "недель";
                        case "day": return "дней";
                        case "hour": return "часов";
                        default: return "";
                    }
                }
                else if (m == 1)
                {
                    switch (data)
                    {
                        case "year": return "год";
                        case "month": return "месяц";
                        case "week": return "неделя";
                        case "day": return "день";
                        case "hour": return "час";
                        default: return "";
                    }
                }
                else if (m >= 2 && m <= 4)
                {
                    switch (data)
                    {
                        case "year": return "года";
                        case "month": return "месяца";
                        case "week": return "недели";
                        case "day": return "дня";
                        case "hour": return "часа";
                        default: return "";
                    }
                }
                else
                {
                    switch (data)
                    {
                        case "year": return "лет";
                        case "month": return "месяцев";
                        case "week": return "недель";
                        case "day": return "дней";
                        case "hour": return "часов";
                        default: return "";
                    }
                }
            }
            return "";
        }
        public static byte[] ResizeImage(this byte[] img, float width, float height)
        {
            float milimetresPerInch = 25.4f;
            using var ms = new System.IO.MemoryStream(img);
            Image original = Image.FromStream(ms);
            //Get the image current width  
            float sourceWidth = original.Width / original.HorizontalResolution * milimetresPerInch;
            //Get the image current height  
            float sourceHeight = original.Height / original.VerticalResolution * milimetresPerInch;
            if ((width == sourceWidth) && (height == sourceHeight))
                return img;
            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;
            //Calulate  width with new desired size  
            nPercentW = width / sourceWidth;
            //Calculate height with new desired size  
            nPercentH = height / sourceHeight;
            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;
            //New Width  
            float destWidth = sourceWidth * nPercent;
            int destinationWidth = (int)(destWidth / milimetresPerInch * original.HorizontalResolution);
            //New Height  
            float destHeight = sourceHeight * nPercent;
            int destinationHeight = (int)(destHeight / milimetresPerInch * original.VerticalResolution);
            Bitmap b = new Bitmap(destinationWidth, destinationHeight);
            Graphics g = Graphics.FromImage((Image)b);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            // Draw image with new width and height  
            g.DrawImage(original, 0, 0, destinationWidth, destinationHeight);
            g.Dispose();
            using var destMs = new System.IO.MemoryStream();
            ((Image)b).Save(destMs, original.RawFormat);
            return destMs.ToArray();
        }
    }
}
