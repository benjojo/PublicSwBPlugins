using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net.NetworkInformation;
namespace discord.plugins
{
    public class HashResolver : IPlugin
    {
        public HashResolver() { }

        public String Name { get { return "Hash Resolver"; } }
        public String Desc { get { return "Detects strings that look like MD5 hashes and then does a Rainbow table lookup."; } }
        public String Auth { get { return "Benjojo"; } }

        public void Load()
        {
            discord.core.Events.OnChatMsgCallback += new core.ChatMsgEventHandler(Events_OnChatMsgCallbaack);
        }

        public void Unload()
        {
            discord.core.Events.OnChatMsgCallback -= new core.ChatMsgEventHandler(Events_OnChatMsgCallbaack);
        }

        void Events_OnChatMsgCallbaack(SteamKit2.SteamFriends.ChatMsgCallback msg)
        {
            try
            {
                string[] words = msg.Message.Split(' ');
                foreach (string word in words)
                {
                    if (word.Length == 32)
                    {
                        if (word.ToCharArray().Any(c => !"0123456789abcdefABCDEF".Contains(c)))
                        {
                            string lookup = core.Discord.DoClassicQuery("SELECT `Word` FROM  `HashMap` WHERE  `Hash` =  '"+ core.Discord.MySQLEscape(word) +"' LIMIT 0 , 30");
                            if (lookup != ",")
                            {
                                core.Discord.SendChatMessage(msg.ChatRoomID,string.Format("Hash {0} is MD5(\"{1}\")",word,lookup.Substring(0,lookup.Length-1)));
                            }
                        }
                    }
                }
            }
            catch
            {
                // Woops.
            }
        }
    }
}