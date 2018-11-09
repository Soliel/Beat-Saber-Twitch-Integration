using System.Collections.Generic;
using AsyncTwitch;
using TwitchIntegrationPlugin.Serializables;

namespace TwitchIntegrationPlugin.Commands
{
    public class PrintQueueCommand : IrcCommand
    {
        public override string[] CommandAlias => new[] {"queue", "songlist", "q", "gimmesongies", "awooque", "showmehell"};
        public override void Run(TwitchMessage msg)
        {
            List<QueuedSong> songList = StaticData.SongQueue.GetSongList();

            string msgString = "[Current Songs in Queue]: ";
            foreach (QueuedSong song in songList)
            {
                if (msgString.Length + song.SongName.Length + 2 > 496)
                {
                    TwitchConnection.Instance.SendChatMessage(msgString);
                    msgString = "";
                }
                msgString += song.SongName + ", ";
            }

            if (msgString.Length > 0)
            {
                TwitchConnection.Instance.SendChatMessage(msgString);
            }
        }
    }
}
