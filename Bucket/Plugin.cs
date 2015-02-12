using System;
using System.Linq;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using SteamKit2;

namespace discord.plugins
{
    public class BucketPlugin : IPlugin
    {
        public BucketPlugin() { }

        public String Name { get { return "Bucket"; } }
        public String Desc { get { return "Factoid Engine"; } }
        public String Auth { get { return "Benjojo & Rohan"; } }

        private static Dictionary<ulong, Bucket> buckets;
        private static Dictionary<ulong, string> rohUsers;

        public void Load()
        {
            buckets = new Dictionary<ulong, Bucket>();
            rohUsers = new Dictionary<ulong, string>();

            discord.core.Events.OnChatMsgCallback += new core.ChatMsgEventHandler(Events_OnChatMsgCallbaack);
            Console.WriteLine("Bucket Loaded.");
        }

        public void Unload()
        {
            discord.core.Events.OnChatMsgCallback -= new core.ChatMsgEventHandler(Events_OnChatMsgCallbaack);
        }

        private static readonly List<ulong> Ignored = new List<ulong>()
        {
            76561198060797164 // ScootaBorg
        };

        void Events_OnChatMsgCallbaack(SteamFriends.ChatMsgCallback msg)
        {
            var userId = msg.ChatterID.ConvertToUInt64();
            var message = msg.Message;

            if (userId == 76561198071890301)
            {
                if (msg.Message.StartsWith("["))
                {
                    var nameEnd = message.IndexOf(']');
                    var name = message.Substring(1, nameEnd - 1);
                    var content = message.Substring(nameEnd + 2);
                    userId = (ulong)Math.Abs(name.GetHashCode());

                    if (!rohUsers.ContainsValue(name))
                        rohUsers[userId] = name;

                    message = content;
                } else {
                    return;
                }
            }

            if (IsReservedFunction(message))
                return;

            if (Ignored.Contains(userId))
                return;

            var bucket = GetBucket(msg.ChatRoomID);
            bucket.ProcessMessage(userId, message);
        }

        private Bucket GetBucket(SteamID chatId)
        {
            Bucket res;
            if (buckets.TryGetValue(chatId.ConvertToUInt64(), out res))
                return res;

            Console.WriteLine("[BUCKET] Creating Bucket instance for {0}", chatId.ConvertToUInt64());
            res = new Bucket("sb", s => Console.WriteLine("[BUCKET] {0}", s));
            res.Output = s => core.Discord.SendChatMessage(chatId, s);
            res.NameFromId = id => GetNickname(id);

            buckets[chatId] = res;
            return res;
        }

        private string GetNickname(ulong id)
        {
            if (rohUsers.ContainsKey(id))
                return rohUsers[id];
            return discord.core.Discord.GetUserName(id);
        }

        private static readonly string[] ReservedCommands = new[] { "last", "lookup", "info", "weather", "sql", "add definition", "weather", "wether", "clop", "math", "rmath", "define" };
        public static bool IsReservedFunction(string msg)
        {
            msg = msg.ToLower();
            return ReservedCommands.Any(str => msg.StartsWith("sb " + str));
        }
    }
}
