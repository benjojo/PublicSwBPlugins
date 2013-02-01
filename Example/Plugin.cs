using System;

namespace discord.plugins
{
    public class TestPlugin : IPlugin
    {
        public TestPlugin() { }

        public String Name { get { return "Test Plugin"; } }
        public String Desc { get { return "A different test."; } }
        public String Auth { get { return "Matt"; } }

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
            Console.WriteLine(msg.Message);
        }
    }
}