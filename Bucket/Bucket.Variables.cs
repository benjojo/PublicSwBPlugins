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
    }
}
