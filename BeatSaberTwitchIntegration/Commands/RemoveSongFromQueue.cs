using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AsyncTwitch;
using JetBrains.Annotations;
using TwitchIntegrationPlugin.Serializables;

namespace TwitchIntegrationPlugin.Commands
{
    [UsedImplicitly]
    public class RemoveSongFromQueue :  IrcCommand
    {
        public override string[] CommandAlias => new[] {"remove", "rem", "rm"};

        private readonly Regex _songIDRX = new Regex(@"\d+-\d+", RegexOptions.Compiled);


        public override void Run(TwitchMessage msg)
        {
            if(!msg.Author.IsMod && !msg.Author.IsBroadcaster) return;
            List<QueuedSong> songList = StaticData.SongQueue.GetSongList();

            string queryString = msg.Content.Remove(0, msg.Content.IndexOf(' ') + 1);
            bool isTextSearch = !_songIDRX.IsMatch(queryString);

            QueuedSong remSong = songList.FirstOrDefault(x => isTextSearch ? x.SongName == queryString : x.Id == queryString);
            StaticData.SongQueue.RemoveSongFromQueue(remSong);

            TwitchConnection.Instance.SendChatMessage($"Song: {remSong.SongName}, removed from queue");
        }
    }
}
