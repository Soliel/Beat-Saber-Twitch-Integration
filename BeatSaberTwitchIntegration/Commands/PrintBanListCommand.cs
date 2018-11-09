using System.Collections.Generic;
using AsyncTwitch;

namespace TwitchIntegrationPlugin.Commands
{
    public class PrintBanListCommand : IrcCommand
    {
        public override string[] CommandAlias => new[] { "blist", "blacklist", "banlist" };

        public override void Run(TwitchMessage msg)
        {
            List<string> banList = StaticData.BanList.GetBanList();

            string msgString = "[Currently banned SongIDs]: ";
            foreach (string songId in banList)
            {
                if (msgString.Length + songId.Length > 496)
                {
                    TwitchConnection.Instance.SendChatMessage(msgString);
                    msgString = "";
                }

                msgString += songId + ", ";
            }

            if (msgString.Length > 0)
            {
                TwitchConnection.Instance.SendChatMessage(msgString);
            }
        }
    }
}
