using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AsyncTwitch;
using TwitchIntegrationPlugin.Serializables;

namespace TwitchIntegrationPlugin.Commands
{
    public class BanSongCommand : IrcCommand
    {
        public override string[] CommandAlias => new []{"ban", "wap"};
        private readonly Regex _songIdrx = new Regex(@"^[0-9\-]+$", RegexOptions.Compiled);

        public override void Run(TwitchMessage msg)
        {
            if(!msg.Author.IsMod && !msg.Author.IsBroadcaster) return;

            string queryString = msg.Content.Remove(0, msg.Content.IndexOf(' ') + 1);
            bool isTextSearch = !_songIdrx.IsMatch(queryString);

            QueuedSong request = ApiConnection.GetSongFromBeatSaver(isTextSearch, queryString, "banboi");
            if (request.SongHash == "" || request.Id == "")
            {
                TwitchConnection.Instance.SendChatMessage("Could not locate song on beatsaver.");
                return;
            }
            if (StaticData.BanList.IsBanned(request.Id))
            {
                TwitchConnection.Instance.SendChatMessage("Song is already banned.");
                return;
            }

            StaticData.BanList.AddToBanList(request.Id);
            StaticData.BanList.SaveBanList();
            TwitchConnection.Instance.SendChatMessage($"{request.SongName} was banned.");
        }
    }
}
