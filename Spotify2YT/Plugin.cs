using System;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Web;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

using System.Net.NetworkInformation;
namespace discord.plugins
{
    public class Spot2YT : IPlugin
    {
        public Spot2YT() { }

        public String Name { get { return "SP2YT"; } }
        public String Desc { get { return "Spotify to youtube resolving"; } }
        public String Auth { get { return "Narrkie"; } }

        public void Load()
        {
            discord.core.Events.OnChatMsgCallback += new core.ChatMsgEventHandler(Events_OnChatMsgCallbaack);
            Console.WriteLine("YT To Spotify Has Loaded");
            ServicePointManager.ServerCertificateValidationCallback = CheakCert;
        }

        public bool CheakCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true; // LOLOLOL IRAN COME GET ME
        }

        public void Unload()
        {
            discord.core.Events.OnChatMsgCallback -= new core.ChatMsgEventHandler(Events_OnChatMsgCallbaack);

        }

        void Events_OnChatMsgCallbaack(SteamKit2.SteamFriends.ChatMsgCallback msg)
        {
            if (msg.Message.Contains("http://open.spotify.com/track/"))
            {
                try
                {
                    Video v = Spotify2Youtube(msg.Message);
                    if (v.Title != "penisdickmassagesthisisauniquestring")
                    {
                        discord.core.Discord.SendChatMessage(msg.ChatRoomID, "Spotify -> Youtube: http://youtu.be/" + v.YouTubeID);
                        //put here code to send to room "http://youtu.be/" + v.YouTubeID
                    }
                    else
                    {
                        discord.core.Discord.SendChatMessage(msg.ChatRoomID, "Failed to resolve Spotify 2 YT");
                        //return ":( http://i.imgur.com/QlPUL.gif Because ";
                    }
                }
                catch
                {
                    discord.core.Discord.SendChatMessage(msg.ChatRoomID, "Failed to resolve Spotify 2 YT");
                }
            }
        }
        public class Video
        {
            public string Artist;
            public string Title;
            public string SpotifyID;
            public string YouTubeID;
            public Video(string name, string artist, string spotifyid, string youtubeid)
            {
                Artist = artist;
                Title = name;
                SpotifyID = spotifyid;
                YouTubeID = youtubeid;
            }
        }

        public static Video Spotify2Youtube(string spotifylink)
        {
            string name = "penisdickmassagesthisisauniquestring";
            string artist = "exploded";
            string youtubeID = "7_mol6B9z00";
            string spotifyID = spotifylink;
            string APIKEY = "";
            //Spotify API part by Matt from AgopBot.
            Regex r = new Regex(@"(?<Protocol>\w+):\/\/(?<Domain>[\w@][\w.:@]+)\/?[\w\.?=%&=\-@/$,]*");
            Match m = r.Match(spotifylink);
            string api = "http://ws.spotify.com/lookup/1/.json?uri=spotify:track:" + m.Value.Substring(30);
            if (m.Success)
            {
                if (m.Groups["Domain"].Value == "open.spotify.com")
                {
                    WebRequest request = WebRequest.Create(api);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    string responseFromServer = reader.ReadToEnd();

                    JObject token = JObject.Parse(responseFromServer);
                    name = token.SelectToken("track").SelectToken("name").ToObject<string>();
                    artist = token.SelectToken("track").SelectToken("artists").First.SelectToken("name").ToObject<string>();

                    string _name = System.Web.HttpUtility.UrlEncode(name);
                    string _artist = System.Web.HttpUtility.UrlEncode(artist);
                    reader.Close();
                    dataStream.Close();
                    response.Close();
                     
                    string querystring = @"https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=5&order=relevance&q=" + _name + "%20%2B%20" + _artist + "&key=" + APIKEY;
                    request = WebRequest.Create(querystring);
                    response = (HttpWebResponse)request.GetResponse();

                    dataStream = response.GetResponseStream();
                    reader = new StreamReader(dataStream);
                    responseFromServer = reader.ReadToEnd();

                    token = JObject.Parse(responseFromServer);

                    string url = token.SelectToken("items").First.SelectToken("id").SelectToken("videoId").ToObject<string>();
                    youtubeID = url;
                    reader.Close();
                    dataStream.Close();
                    response.Close();
                }
            }
            return new Video(name, artist, spotifylink, youtubeID);
        }
    }
}