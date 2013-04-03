using System;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

namespace discord.plugins
{
    public class Bucket : IPlugin
    {
        public Bucket() { }

        public String Name { get { return "Bucket"; } }
        public String Desc { get { return "Factoid Engine"; } }
        public String Auth { get { return "Benjojo & Rohan"; } }


        public void Load()
        {
            discord.core.Events.OnChatMsgCallback += new core.ChatMsgEventHandler(Events_OnChatMsgCallbaack);
            Console.WriteLine("Bukkit Loaded.");
        }

        public void Unload()
        {
            discord.core.Events.OnChatMsgCallback -= new core.ChatMsgEventHandler(Events_OnChatMsgCallbaack);

        }

        void Events_OnChatMsgCallbaack(SteamKit2.SteamFriends.ChatMsgCallback msg)
        {

        }
        
    }
}