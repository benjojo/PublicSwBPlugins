using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace discord.plugins
{
    public partial class Bucket
    {
        public string Var_Who(string parameters)
        {
            return NameFromId(who);
        }

        public string Var_Someone(string parameters)
        {
            var id = RecentPosters.Skip(random.Next(RecentPosters.Count)).First();
            return NameFromId(id);
        }

        public string Var_Digit(string parameters)
        {
            return random.Next(10).ToString();
        }

        public string Var_Nonzero(string parameters)
        {
            return random.Next(1, 10).ToString();
        }

        // Low latency version of asciidicks.com
        public string Var_Dick(string parameters)
        {
            return string.Format("8{0}D", new string('=', random.Next(1, 8)));
        }

        public string Var_Tumblr(string parameters)
        {
            if (parameters == null)
                return null; // needs parameters

            const string tumblrTaggedUri = "http://api.tumblr.com/v2/tagged?api_key=fuiKNFp9vQFvjLNvx4sUwti4Yb5yGutBN4Xh10LXZhhRKjWlV4&limit=50&before={1}&tag={0}";
            const int timeRange = 2 * 7 * 24 * 60 * 60; // two weeks

            var time = Util.GetCurrentUnixTimestamp() - random.Next(timeRange);
            var uri = string.Format(tumblrTaggedUri, HttpUtility.UrlEncode(parameters), time);
            var response = DownloadPage(uri);

            try
            {
                var images = new List<string>();
                dynamic obj = JsonConvert.DeserializeObject(response);
                foreach (dynamic entry in obj.response)
                {
                    try
                    {
                        foreach (dynamic photo in entry.photos)
                        {
                            images.Add(photo.original_size.url.ToString());
                        }
                    }
                    catch { } // no photos/fuck u
                }
                return images[random.Next(images.Count)];
            }
            catch
            {
                return ":(";
            }
        }

        #region Top Secret Austech Similator 2013
        private static readonly char[] RoflLetters = new[] { 'R', 'O', 'F', 'L' };
        private static readonly Dictionary<char, string[]> RoflTypos = new Dictionary<char, string[]>()
        {
            { 'R', new[] { "TR", "E" } },
            { 'O', new[] { "OP", "P" } },
            { 'F', new[] { "D" } },
            { 'L', new[] { "KL" } }
        };
        public string Var_Rofl(string parameters)
        {
            var funnyness = random.Next(4);

            switch (funnyness)
            {
                case 0:
                    return "rofl";

                case 1:
                    {
                        var subType0 = random.Next(2);
                        switch (subType0)
                        {
                            case 0:
                                return "ROFL";
                            case 1:
                                return "ROFl";
                        }
                        break;
                    }

                case 2:
                    return string.Format("R{0}F{1}", new string('O', random.Next(5, 18)), random.Next(2) == 0 ? "L" : "l");

                case 3:
                    {
                        var current = random.Next(2);
                        var res = "";
                        var len = random.Next(8, 24);
                        for (var i = 0; i < len; i++)
                        {
                            var c = RoflLetters[current];
                            current += random.Next(1, 3);
                            current %= RoflLetters.Length;
                            var press = "" + c;
                            if (random.NextDouble() > 0.95)
                            {
                                var typos = RoflTypos[c];
                                press = typos[random.Next(typos.Length)];
                            }
                            res += press;
                        }
                        return res;
                    }
            }

            return "TOO FUNNY";
        }
        #endregion
    }
}
