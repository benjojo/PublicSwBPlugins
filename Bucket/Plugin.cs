using System;
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

        private static Dictionary<SteamID, Bucket> buckets;

        public void Load()
        {
            buckets = new Dictionary<SteamID, Bucket>();
            discord.core.Events.OnChatMsgCallback += new core.ChatMsgEventHandler(Events_OnChatMsgCallbaack);
            Console.WriteLine("Bucket Loaded.");
        }

        public void Unload()
        {
            discord.core.Events.OnChatMsgCallback -= new core.ChatMsgEventHandler(Events_OnChatMsgCallbaack);
        }

        void Events_OnChatMsgCallbaack(SteamFriends.ChatMsgCallback msg)
        {
            var bucket = GetBucket(msg.ChatRoomID);
            bucket.ProcessMessage(msg.ChatterID.ConvertToUInt64(), msg.Message);
        }

        private Bucket GetBucket(SteamID chatId)
        {
            Bucket res;
            if (buckets.TryGetValue(chatId, out res))
                return res;

            res = new Bucket("sb", s => Console.WriteLine("[BUCKET] {0}", s));
            res.Output = s =>
            {
                core.Discord.SendChatMessage(chatId, s);
            };
            res.NameFromId = id => discord.core.Discord.GetUserName(id);

            buckets[chatId] = res;
            return res;
        }
    }
}