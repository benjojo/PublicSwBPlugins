using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Newtonsoft.Json.Linq;

namespace discord.plugins
{
    public class BetterMath : IPlugin
    {

        public BetterMath() { }

        public String Name { get { return "Better Maths"; } }
        public String Desc { get { return "Math funcs assisted by google."; } }
        public String Auth { get { return "Communist Pancake"; } }

        public void Load()
        {
            discord.core.Events.OnChatMsgCallback += new core.ChatMsgEventHandler(Events_OnChatMsgCallback);
        }

        public void Unload()
        {
            discord.core.Events.OnChatMsgCallback -= new core.ChatMsgEventHandler(Events_OnChatMsgCallback);
        }

        void Events_OnChatMsgCallback(SteamKit2.SteamFriends.ChatMsgCallback msg)
        {
            try
            {
                if (msg.Message.StartsWith("sb bmath") && msg.Message.Split(' ').Length > 3)
                {
                    string[] args = msg.Message.Split(' ');
                    string[] mathArgs = new string[args.Length - 2];
                    Array.Copy(args, 2, mathArgs, 0, args.Length - 2);
                    string math = string.Join("", mathArgs);
                    math = math.Replace("as", "=").Replace("in", "="); //turns 1USD as EUR into 1USD=EUR, or 1GB in KB into 1GB=KB
                    JObject json = JObject.Parse(new WebClient().DownloadString("http://www.google.com/ig/calculator?hl=en&q=" + System.Uri.EscapeDataString(math)));
                    if (!string.IsNullOrWhiteSpace((string)json["error"]) && (string)json["error"] != "0")
                        discord.core.Discord.SendChatMessage(msg.ChatRoomID, "Cannot compute!");
                    discord.core.Discord.SendChatMessage(msg.ChatRoomID, json["lhs"] + " = " + json["rhs"]);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.Source);
                Console.WriteLine(e.StackTrace);
                // Woops.
            }
        }
    }
}
