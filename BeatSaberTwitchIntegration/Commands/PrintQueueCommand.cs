using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AsyncTwitch;
using TwitchIntegrationPlugin.Serializables;

namespace TwitchIntegrationPlugin.Commands
{
    class PrintQueueCommand : IrcCommand
    {
        public override string[] CommandAlias => new[] {"queue", "songlist", "q", "gimmesongies", "awooque", "showmehell"};
        public override void Run(TwitchMessage msg)
        {
            List<QueuedSong> songList = StaticData.SongQueue.GetSongList();

            string msgString = "[Current Songs in Queue]: ";
            foreach (QueuedSong song in songList)
            {
                if (msgString.Length + song.SongName.Length + 2 > 498)
                {
                    TwitchConnection.Instance.SendChatMessage(msgString);
                    msgString = "";
                }
                msgString += song.SongName + ", ";


            }
        }
    }
}
