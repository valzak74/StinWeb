using System;
using System.Collections.Generic;
using System.Linq;
//using System.Text;
//using System.Net;
//using System.Net.Mail;
using System.IO;
using System.Printing;
using System.Text;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Net.Http;
using System.Threading.Tasks;
//using NPOI.SS.UserModel;
//using NPOI.HSSF.UserModel;
//using NPOI.HSSF.Util;
//using NPOI.XSSF.UserModel;
//using NPOI.SS.Util;
//using System.Text.RegularExpressions;
//using System.Globalization;

//using MigraDoc;
//using PdfSharp;

//using MigraDoc.DocumentObjectModel;
//using MigraDoc.Rendering;
//using PdfSharp.Pdf;
//using NPOI.OpenXmlFormats.Dml.Spreadsheet;

namespace Tests
{
    public class ListNode
    {
      public int val;
      public ListNode next;
      public ListNode(int val = 0, ListNode next = null)
      {
            this.val = val;
            this.next = next;
      }
    }

    class Program
    {
        public static int Trap(int[] height)
        {
            int w = 0;
            int water = 0;
            var data = height.Select((x, i) => new { value = x, index = i });
            for (int i = 0; i < height.Length; i++)
            {
                int leftBorder = height[i];
                if (leftBorder == 0)
                    continue;
                var nextBorder = data
                    .Where(x => (x.value >= leftBorder) && (x.index > i))
                    .FirstOrDefault();
                if (nextBorder != null)
                {
                    while (i < nextBorder.index)
                    {
                        water += leftBorder - height[i];
                        i++;
                    }
                    i = nextBorder.index - 1;
                }
            }
            return water;
        }
        public static int FirstMissingPositive(int[] nums)
        {
            var positive = nums.Where(x => x > 0).OrderBy(x => x).Distinct();
            int total = positive.Count();
            if (total == 0)
                return 1;
            int minValue = positive.First();
            if (minValue > 1)
                return 1;
            int maxValue = positive.Last();
            if (maxValue - minValue == total - 1)
                return maxValue + 1;
            if (total > 10)
            {
                return SplitByHalf(positive);
            }
            else
                for (int i = 2; i < int.MaxValue; i++)
                {
                    if (!positive.Any(x => x == i))
                        return i;
                }
            return maxValue + 1;
        }
        private static int SplitByHalf(IEnumerable<int> data)
        {
            Console.WriteLine(data.Count().ToString());

            int half = data.Count() / 2;
            Console.WriteLine(half.ToString());
            var firstHalf = data.Take(half);
            int res = Proccess(firstHalf);
            if (res < 0)
            {
                var secondHalf = data.Skip(half);
                if (secondHalf.First() - firstHalf.Last() > 1)
                    return firstHalf.Last() + 1;
                res = Proccess(secondHalf);
            }
            return res;
        }
        private static int Proccess(IEnumerable<int> data)
        {
            int total = data.Count();
            Console.WriteLine(total.ToString());
            Console.WriteLine(data.First().ToString()); 
            Console.WriteLine(data.Last().ToString());
            if (data.Last() - data.First() != total - 1)
            {
                if (total > 10)
                    return SplitByHalf(data);
                else
                    for (int i = data.First(); i < int.MaxValue; i++)
                        if (!data.Any(x => x == i))
                            return i;
            }
            return -1;
        }

        public static bool IsValidSudoku(char[][] board)
        {
            var col0 = new List<char>();
            var col1 = new List<char>();
            var col2 = new List<char>();
            var col3 = new List<char>();
            var col4 = new List<char>();
            var col5 = new List<char>();
            var col6 = new List<char>();
            var col7 = new List<char>();
            var col8 = new List<char>();
            var box0 = new List<char>();
            var box1 = new List<char>();
            var box2 = new List<char>();
            var box3 = new List<char>();
            var box4 = new List<char>();
            var box5 = new List<char>();
            var box6 = new List<char>();
            var box7 = new List<char>();
            var box8 = new List<char>();
            for (int row = 0; row < board.Length; row++)
            {
                var rowDigits = board[row].Where(x => x != '.').Select(x => x);
                if (rowDigits.Count() != rowDigits.Distinct().Count())
                    return false;
                col0.Add(board[row][0]);
                col1.Add(board[row][1]);
                col2.Add(board[row][2]);
                col3.Add(board[row][3]);
                col4.Add(board[row][4]);
                col5.Add(board[row][5]);
                col6.Add(board[row][6]);
                col7.Add(board[row][7]);
                col8.Add(board[row][8]);

                if (row <= 2)
                {
                    box0.Add(board[row][0]);
                    box0.Add(board[row][1]);
                    box0.Add(board[row][2]);
                    box1.Add(board[row][3]);
                    box1.Add(board[row][4]);
                    box1.Add(board[row][5]);
                    box2.Add(board[row][6]);
                    box2.Add(board[row][7]);
                    box2.Add(board[row][8]);
                }
                else if (row <= 5)
                {
                    box3.Add(board[row][0]);
                    box3.Add(board[row][1]);
                    box3.Add(board[row][2]);
                    box4.Add(board[row][3]);
                    box4.Add(board[row][4]);
                    box4.Add(board[row][5]);
                    box5.Add(board[row][6]);
                    box5.Add(board[row][7]);
                    box5.Add(board[row][8]);
                }
                else
                {
                    box6.Add(board[row][0]);
                    box6.Add(board[row][1]);
                    box6.Add(board[row][2]);
                    box7.Add(board[row][3]);
                    box7.Add(board[row][4]);
                    box7.Add(board[row][5]);
                    box8.Add(board[row][6]);
                    box8.Add(board[row][7]);
                    box8.Add(board[row][8]);
                }
            }
            var colDigits = col0.Where(x => x != '.').Select(x => x);
            if (colDigits.Count() != colDigits.Distinct().Count())
                return false;
            colDigits = col1.Where(x => x != '.').Select(x => x);
            if (colDigits.Count() != colDigits.Distinct().Count())
                return false;
            colDigits = col2.Where(x => x != '.').Select(x => x);
            if (colDigits.Count() != colDigits.Distinct().Count())
                return false;
            colDigits = col3.Where(x => x != '.').Select(x => x);
            if (colDigits.Count() != colDigits.Distinct().Count())
                return false;
            colDigits = col4.Where(x => x != '.').Select(x => x);
            if (colDigits.Count() != colDigits.Distinct().Count())
                return false;
            colDigits = col5.Where(x => x != '.').Select(x => x);
            if (colDigits.Count() != colDigits.Distinct().Count())
                return false;
            colDigits = col6.Where(x => x != '.').Select(x => x);
            if (colDigits.Count() != colDigits.Distinct().Count())
                return false;
            colDigits = col7.Where(x => x != '.').Select(x => x);
            if (colDigits.Count() != colDigits.Distinct().Count())
                return false;
            colDigits = col8.Where(x => x != '.').Select(x => x);
            if (colDigits.Count() != colDigits.Distinct().Count())
                return false;
            var boxDigits = box0.Where(x => x != '.').Select(x => x);
            if (boxDigits.Count() != boxDigits.Distinct().Count())
                return false;
            boxDigits = box1.Where(x => x != '.').Select(x => x);
            if (boxDigits.Count() != boxDigits.Distinct().Count())
                return false;
            boxDigits = box2.Where(x => x != '.').Select(x => x);
            if (boxDigits.Count() != boxDigits.Distinct().Count())
                return false;
            boxDigits = box3.Where(x => x != '.').Select(x => x);
            if (boxDigits.Count() != boxDigits.Distinct().Count())
                return false;
            boxDigits = box4.Where(x => x != '.').Select(x => x);
            if (boxDigits.Count() != boxDigits.Distinct().Count())
                return false;
            boxDigits = box5.Where(x => x != '.').Select(x => x);
            if (boxDigits.Count() != boxDigits.Distinct().Count())
                return false;
            boxDigits = box6.Where(x => x != '.').Select(x => x);
            if (boxDigits.Count() != boxDigits.Distinct().Count())
                return false;
            boxDigits = box7.Where(x => x != '.').Select(x => x);
            if (boxDigits.Count() != boxDigits.Distinct().Count())
                return false;
            boxDigits = box8.Where(x => x != '.').Select(x => x);
            if (boxDigits.Count() != boxDigits.Distinct().Count())
                return false;
            return true;
        }
        public static int RemoveDuplicates(int[] nums)
        {
            var tt = (new List<char> { '.','.','5','5','.'}).Where(x => x != '.').Select(x => x);
            Console.WriteLine(tt.Count());
            Console.WriteLine(tt.Distinct().Count());
            //List<int> result = new List<int>();
            int j = -1;
            for (int i = 0; i < nums.Length; i++)
            {
                //result.Add(nums[i]);
                j++;
                Console.WriteLine("i=" + i.ToString() + " ; j=" + j.ToString());
                nums[j] = nums[i];
                Console.WriteLine("!");
                while ((i < nums.Length - 1) && (nums[i] == nums[i + 1]))
                    i++;
            }
            Console.WriteLine(j.ToString());
            return j + 1;
        }
        public static IList<IList<int>> ThreeSum(int[] nums)
        {
            IList<IList<int>> result = new List<IList<int>>();
            var n = nums.ToList();
            n.Sort();
            for (int i = 0; i < n.Count - 2; i++)
            {
                if ((i > 0) && (n[i] == n[i - 1]))
                    continue;

                int minIndex = i + 1;
                int maxIndex = n.Count - 1;

                while (minIndex < maxIndex)
                {
                    if (n[i] + n[minIndex] + n[maxIndex] == 0)
                    {
                        result.Add(new List<int> { n[i], n[minIndex], n[maxIndex] });
                        int checkedValue = n[minIndex];
                        minIndex++;
                        while ((minIndex < maxIndex) && (n[minIndex] == n[minIndex + 1]))
                            minIndex++;
                        if (checkedValue == n[minIndex])
                            minIndex = maxIndex;
                    }
                    else if (n[i] + n[minIndex] + n[maxIndex] > 0)
                    {
                        int checkedValue = n[maxIndex];
                        maxIndex--;
                        while ((minIndex < maxIndex) && (n[maxIndex] == n[maxIndex - 1]))
                            maxIndex--;
                        if (checkedValue == n[maxIndex])
                            minIndex = maxIndex;
                    }
                    else
                    {
                        int checkedValue = n[minIndex];
                        minIndex++;
                        while ((minIndex < maxIndex) && (n[minIndex] == n[minIndex + 1]))
                            minIndex++;
                        if (checkedValue == n[minIndex])
                            minIndex = maxIndex;
                    }
                }
            }
            return result;
        }
        private static char Next(string s, int position)
        {
            return position < s.Length - 1 ? s[position + 1] : char.MinValue;
        }
        private static int SameChar(string s, int position, char p)
        {
            int start = position;
            while ((position < s.Length) && (s[position] == p))
                position++;
            return position - start;
        }
        public static bool IsMatch(string s, string p)
        {
            if ((p.IndexOf('.') < 0) && (p.IndexOf('*') < 0))
                return p == s;
            int sPosition = 0;
            for (int i = 0; i < p.Length; i++)
            {
                if (sPosition >= s.Length)
                    return false;
                if (p[i] == '.')
                {
                    if (Next(p, i) == '*')
                    {
                        char afterStar = Next(p, i + 1);
                        if (afterStar == char.MinValue)
                            return true;
                        else
                        {
                            if (char.IsLetter(afterStar))
                            {
                                sPosition = s.IndexOf(afterStar);
                                if (sPosition < 0)
                                    return false;
                                continue;
                            }
                            else
                            {
                                //.
                            }
                        }
                    }
                    sPosition++;
                    continue;
                }
                char pChar = p[i];
                char pNextChar = Next(p, i);
                if (pNextChar == '*')
                {
                    int e = i + 1;
                    while(e < p.Length)
                    {
                        char suffexChar = Next(p, e);
                        if (suffexChar == pChar)
                            e++;
                        else
                            break;
                    }
                    Console.WriteLine("e=" + e.ToString());
                    int minPosition = i;
                    int maxPosition = i + e - (i + 1);
                    int repeateChars = SameChar(s, sPosition, pChar);
                    Console.WriteLine("minPosition=" + minPosition.ToString());
                    Console.WriteLine("maxPosition=" + maxPosition.ToString());
                    Console.WriteLine("repeateChars=" + repeateChars.ToString());
                    if (repeateChars >= (maxPosition - minPosition))
                    {
                        sPosition = sPosition + repeateChars;
                        i = maxPosition+1;
                        Console.WriteLine("sPosition=" + sPosition.ToString());
                    }
                    else
                        return false;
                }
                else
                {
                    if (pChar != s[sPosition])
                        return false;
                    sPosition++;
                }
            }
            
            return sPosition == s.Length;
        }
        public static string LongestPalindrome(string s)
        {
            int maxLength = 0;
            string maxPalindrom = "";
            for (int i = 0; i < s.Length; i++)
            {
                int left = i;
                int right = i;
                while ((left > 0) && (s[left - 1] == s[i]))
                    left--;
                while ((right < s.Length - 1) && (s[right + 1] == s[i]))
                    right++;
                while ((left > 0) && (right < s.Length - 1) && (s[left - 1] == s[right + 1]))
                {
                    left--;
                    right++;
                    if ((right - left + 1) > maxLength)
                    {
                        maxLength = right - left + 1;
                        maxPalindrom = s.Substring(left, right - left + 1);
                    }
                }
            }
            return maxPalindrom;
        }

        public static ListNode AddTwoNumbers(ListNode l1, ListNode l2)
        {
            int singleSum = 0;
            ListNode result = null;
            while ((l1 != null) || (l2 != null) || (singleSum > 0))
            {
                singleSum += (l1 != null ? l1.val : 0) + (l2 != null ? l2.val : 0);
                if (result != null)
                    result = new ListNode(singleSum % 10, result);
                else
                    result = new ListNode(singleSum % 10);
                singleSum = singleSum /= 10;
                if (l1 != null)
                    l1 = l1.next;
                if (l2 != null)
                    l2 = l2.next;
            }
            return result;
        }
        public static int[] TwoSum(int[] nums, int target)
        {
            int er = 18;
            Console.WriteLine((er % 10).ToString());
            Console.WriteLine((er/= 10).ToString());
            Dictionary<int, int> checkedValues = new Dictionary<int, int>();
            var data = nums
                .Select((num, ind) => new { number = num, index = ind })
                .Where(x => x.number <= target);
            var result = new List<int>();
            foreach (var item in data)
            {
                int diff = target - item.number;
                Console.WriteLine(diff.ToString());
                if (diff == 0)
                {
                    //return new int[1] { item.index };
                    result.Add(item.index);
                }
                else if (checkedValues.Any(v => v.Value == diff))
                {
                    int oldIndex = checkedValues
                        .Where(v => v.Value == diff)
                        .Select(v => v.Key)
                        .FirstOrDefault();
                    return new int[2] { oldIndex, item.index };
                }
                else
                {
                    checkedValues.Add(item.index, item.number);
                }

            }
            return result.ToArray();
        }
         static void PrintHTML(string printer, string html)
        {
            using (var webBrowser = new System.Windows.Forms.WebBrowser())
            {
                webBrowser.DocumentText = "<html><body><p>I like StackOverflow</p><body></html>";
                webBrowser.DocumentCompleted += ((sender, e) => { ((System.Windows.Forms.WebBrowser)sender).Print(); ((System.Windows.Forms.WebBrowser)sender).Dispose(); });
            }
        }
        static async Task<string> GetResponse(string auth, string body)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    string url = @"https://localhost:44388/RegisterShipment";
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.Add("User-Agent", "HttpTestClient");
                    request.Headers.Add("Authorization", auth);
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                    var response = await client.SendAsync(request);
                    if (response.Content != null)
                        return await response.Content.ReadAsStringAsync();
                }
                return null;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        static async Task AsyncTester()
        {
            //var client = new HttpClient();
            //string url = @"https://localhost:44388/RegisterShipment";
            var reqData = new List<string>
                {
                    "Basic NTQ3NzE2VEwjMTAwMDAxOnJtLWFjY2VzczEzMw==",
                    "Basic NTQ3NzE2VEwjMTAwMDAxOnJtLWFjY2VzczEzMw==",
                    "Basic NTQ3NzE2VEwjMTAwMDAxOnJtLWFjY2VzczEzMw=="
                };
            string body = "{\"siteId\":9901,\"customerId\":100001,\"description\":\"FASHION\",\"shipmentRef\":\"HY450659207\",\"cost\":74.11,\"currency\":\"GBP\",\"customer\":{\"company\":\"NEW LOOK\",\"contact\":{},\"companyAddress\":{\"street\":\"PIT HEAD CLOSE\",\"district\":\"LYMEDALE BUSINESS PARK\",\"town\":\"NEWCASTLE UNDER L\",\"county\":\"STAFFORDSHIRE\",\"postcode\":\"ST5 9QG\",\"countryCode\":\"GB\"}},\"recipient\":{\"contact\":{\"name\":\"AMY THORNALLEY\",\"phone\":\"07593911130\",\"mobile\":\"07593911130\",\"email\":\"amyy_louise @live.com\"},\"companyAddress\":{\"country\":\"NORTH HUMBERSIDE\",\"street\":\"ST.ALBANS CHURCH\",\"district\":\"62 HALL ROAD\",\"town\":\"HULL\",\"postcode\":\"HU6 8SA\",\"countryCode\":\"GB\"}},\"shipment\":{\"packs\":1,\"carrier\":\"ROYAL MAIL\",\"serviceCode\":\"TPLN\",\"contractNo\":\"547716TL\",\"totalWeight\":1.133,\"addInsurance\":0,\"insValue\":0.0,\"despatchDate\":\"2022 - 05 - 10T00: 00:00 + 01:00\"},\"labelType\":3,\"invoiceType\":0}";
            var requests = reqData.Select
                (
                    x => Task.Factory.StartNew(() => GetResponse(x, body))
                    //{
                    //    GetResponse(x, body);
                    //    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    //    request.Headers.Add("User-Agent", "HttpTestClient");
                    //    request.Headers.Add("Authorization", x);
                    //    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                    //    client.SendAsync(request);
                    //})
                ).ToList();

            //Wait for all the requests to finish
            await Task.WhenAll(requests);
        }
        static ListNode CreateListNode(List<int> data)
        {
            ListNode result = null;
            data.Reverse();
            foreach (var item in data)
            {
                Console.WriteLine(item);
                result = new ListNode(item, result);
            }
            return result;
        }
        static void Main(string[] args)
        {
            var trap = Trap(new int[] {0, 1, 0, 2, 1, 0, 1, 3, 2, 1, 2, 1});
            var missed = FirstMissingPositive(new int[] { 12, 34, 41, 9, 14, 9, 26, 13, 13, 4, 19, 5, 19, 18, -1, 6, 5, 32, -9, 8, 35, -6, 41, -2, 11, 41, -6, 13, 17, -8, 41, 34, -2, 40, 2, 24, 21, 36, 1, 22, 1, 3 });
            var inSudoku = new List<char[]>();
            inSudoku.Add(new char[] { '.', '.', '4', '.', '.', '.', '6', '3', '.' });
            inSudoku.Add(new char[] { '.', '.', '.', '.', '.', '.', '.', '.', '.' });
            inSudoku.Add(new char[] { '5', '.', '.', '.', '.', '.', '.', '9', '.' });
            inSudoku.Add(new char[] { '.', '.', '.', '5', '6', '.', '.', '.', '.' });
            inSudoku.Add(new char[] { '4', '.', '3', '.', '.', '.', '.', '.', '1' });
            inSudoku.Add(new char[] { '.', '.', '.', '7', '.', '.', '.', '.', '.' });
            inSudoku.Add(new char[] { '.', '.', '.', '5', '.', '.', '.', '.', '.' });
            inSudoku.Add(new char[] { '.', '.', '.', '.', '.', '.', '.', '.', '.' });
            inSudoku.Add(new char[] { '.', '.', '.', '.', '.', '.', '.', '.', '.' });
            var validSudoku = IsValidSudoku(inSudoku.ToArray());
            if (validSudoku)
                Console.WriteLine("!!!");
            else
                Console.WriteLine("---");
            var valid = RemoveDuplicates((new List<int> { 0, 0, 1, 1, 1, 2, 2, 3, 3, 4 }).ToArray());
            var r = ThreeSum((new List<int> { -2, 0, 1, 1, 2 }).ToArray());
            var match = IsMatch("aaa", "ab*a*c*a");
            var palindrom = LongestPalindrome("babad");
            var t2 = AddTwoNumbers(CreateListNode(new List<int> { 2, 4, 3 }), CreateListNode(new List<int> { 5, 6, 4 }));
            var wer = TwoSum(new int[4] { -3, 7, 3, 11 }, 0);
            Console.WriteLine(string.Join(",", wer.Select(x => x.ToString())));
            string auth = "Basic NTQ3NzE2VEwjMTAwMDAxOnJtLWFjY2VzczEzMw==";
            string body = "{\"siteId\":9901,\"customerId\":100001,\"description\":\"FASHION\",\"shipmentRef\":\"HY450659207\",\"cost\":74.11,\"currency\":\"GBP\",\"customer\":{\"company\":\"NEW LOOK\",\"contact\":{},\"companyAddress\":{\"street\":\"PIT HEAD CLOSE\",\"district\":\"LYMEDALE BUSINESS PARK\",\"town\":\"NEWCASTLE UNDER L\",\"county\":\"STAFFORDSHIRE\",\"postcode\":\"ST5 9QG\",\"countryCode\":\"GB\"}},\"recipient\":{\"contact\":{\"name\":\"AMY THORNALLEY\",\"phone\":\"07593911130\",\"mobile\":\"07593911130\",\"email\":\"amyy_louise @live.com\"},\"companyAddress\":{\"country\":\"NORTH HUMBERSIDE\",\"street\":\"ST.ALBANS CHURCH\",\"district\":\"62 HALL ROAD\",\"town\":\"HULL\",\"postcode\":\"HU6 8SA\",\"countryCode\":\"GB\"}},\"shipment\":{\"packs\":1,\"carrier\":\"ROYAL MAIL\",\"serviceCode\":\"TPLN\",\"contractNo\":\"547716TL\",\"totalWeight\":1.133,\"addInsurance\":0,\"insValue\":0.0,\"despatchDate\":\"2022 - 05 - 10T00: 00:00 + 01:00\"},\"labelType\":3,\"invoiceType\":0}";
            Task t = AsyncTester(); //GetResponse(auth, body); //
            t.Wait();
            //var responses = requests.Select
            //    (
            //        task => task.Result
            //    );

            //foreach (var r in responses)
            //{
            //    // Extract the message body
            //    var si = await r.Content.ReadAsStringAsync();
            //    Console.WriteLine(si);
            //}            
            string asdf = "";
            //PrintHTML("","");
            //try
            //{
            //    var file = File.ReadAllBytes(@"f:\tmp\5.pdf");
            //    var printQueue = LocalPrintServer.GetDefaultPrintQueue();

            //    using (var job = printQueue.AddJob())
            //    using (var stream = job.JobStream)
            //    {
            //        stream.Write(file, 0, file.Length);
            //    }
            //}
            //catch (Exception)
            //{

            //    throw;
            //}
            string a = "1,09";
            var tt = Convert.ToDecimal(a);
            string s = "D00059710"; //"4B3030303337383934"; "D00036240"; //"443030303336323430"; //"D00030879"; //"443030303330383739"
            var bs = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(s));
            string back = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(bs));

            byte[] ba = System.Text.Encoding.UTF8.GetBytes(s);
            var hexString = BitConverter.ToString(ba);
            //var back2 = System.Text.Encoding.UTF8.GetString(BitConverter.GetBytes(hexString));

            DateTime scanDate;
            string scanlines = "DET|OCC|OCC964024000204289AO001001|DLD|01|Swadlincote Hub|2017|20210614|050554";
            if (scanlines.Contains("DET"))
            {
                string[] fields = scanlines.Split('|');
                if (fields.Length == 9)
                {
                    string consNo = fields[2].Trim();
                    if (!string.IsNullOrEmpty(consNo) && (consNo.Length > 3))
                    {
                        string commParcelNo = consNo;
                        //comm.Parameters["@PARCEL_NO"].Value = consNo;
                        consNo = consNo.Substring(0, consNo.Length - 3);
                        DateTime.TryParseExact(fields[7].Trim() + fields[8].Trim(), "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out scanDate);
                        string commConsNo = consNo;
                        //comm.Parameters["@CONS_NO"].Value = consNo;
                        string commCarrierCode = fields[3].Trim();
                        //comm.Parameters["@CARRIER_CODE"].Value = fields[3].Trim();
                        var commScanDate = scanDate;
                        //comm.Parameters["@SCAN_DATE"].Value = scanDate;
                        var commScanDepotName = fields[4].Trim() + " - " + fields[5].Trim();
                        //comm.Parameters["@SCAN_DEPOT_NAME"].Value = fields[4].Trim() + " - " + fields[5].Trim();
                        //comm.ExecuteNonQuery();
                        //if (!consList.Contains(consNo))
                        //    consList.Add(consNo);
                    }
                }
            }
            //List<string> dataList = new List<string> { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };
            //var rr = dataList
            //    .Select((x, i) => new { Index = i, Value = x })
            //    .GroupBy(x => x.Index / 4)
            //    .Select(x => x.Select(v => v.Value).ToList())
            //    .ToList();
            //var data = dataList.
            //        //Where(dt => dt.DateTime > dateLimit).
            //        //OrderBy(dt => dt.SomeField).
            //        //ToArray().
            //        Select((dt, i) => new { DataLog = dt, Index = i }).
            //        Where(x => x.Index % 5 == 0).
            //        Select(x => x.DataLog);

            //string x1 = "";
            //string scan;
            //bool eodSaveFiles = true;
            //StreamWriter sw = null;
            //using (Stream sm = new System.IO.MemoryStream())
            //{
            //    sw = new StreamWriter(sm);
            //    sw.WriteLine("data");
            //    sw.WriteLine("data 2");

            //    sm.Position = 0;
            //    using (StreamReader sr2 = new StreamReader(sm))
            //    {
            //        scan = sr2.ReadToEnd();
            //        if (eodSaveFiles)
            //        {
            //            sm.Position = 0;
            //            string logFile = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\Logs\\tt.txt";
            //            Directory.CreateDirectory(Path.GetDirectoryName(logFile));
            //            using (var fileStream = new FileStream(logFile, FileMode.Create, FileAccess.Write))
            //            {
            //                sm.CopyTo(fileStream);
            //            }
            //        }
            //    }
            //}
            //sw.Dispose();

            //var offset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow);
            //var sd = offset.ToString();

            //Console.WriteLine(sd);
            //Console.WriteLine("Press any key to quit !");
            //Console.ReadKey();

            //DateTime dateTime = DateTime.Now;
            //var datestr = dateTime.ToString("yyyyMMdd");
            //var h = dateTime.Hour;
            //var m = dateTime.Minute;
            //var s = dateTime.Second;
            //var ms = dateTime.Millisecond;
            //var time = (h * 3600 * 10000) + (m * 60 * 10000) + (s * 10000) + (ms * 10);
            //string strParse = "1 810,26 ₽";
            //string strParse2 = "1 810,26 ₽";
            //strParse = strParse.Replace('\u00A0', ' ');
            //var bytes = Encoding.UTF8.GetBytes(strParse);
            //var bytes2 = Encoding.UTF8.GetBytes(strParse2);
            //var numberFormatInfo = new NumberFormatInfo { NumberDecimalSeparator = ",", CurrencySymbol = "₽" };
            //decimal value;
            //bool b = Decimal.TryParse(strParse, NumberStyles.AllowCurrencySymbol | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.Number, new CultureInfo("ru-RU"), out value);
            //decimal d = decimal.Parse(strParse, numberFormatInfo);
        }
    }
}
