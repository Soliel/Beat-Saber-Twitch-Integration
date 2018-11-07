using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TwitchIntegrationPlugin.Serializables
{
    [Serializable]
    public class BanList
    {
        [SerializeField]
        private List<string> _bannedSongs;
        private readonly Regex _songIdValidationRegex = new Regex(@"^[0-9\-]+$");

        public BanList()
        {
            _bannedSongs = new List<string>();
        }

        public void AddToBanList(string songID)
        {
            if (!_songIdValidationRegex.IsMatch(songID)) throw new FormatException("songID is not in the valid format.");
            if (songID.Contains("-"))
            {
                _bannedSongs.Add(songID.Split('-')[0]);
            }

            _bannedSongs.Add(songID);
        }

        public void RemoveFromBanList(string songID)
        {
            if (!_songIdValidationRegex.IsMatch(songID)) throw new FormatException("songID is not in the valid format");
            if (songID.Contains("-"))
            {
                songID = songID.Split('-')[0];
            }

            _bannedSongs.Remove(songID);
        }

        public bool IsBanned(string songID)
        {
            if (!_songIdValidationRegex.IsMatch(songID)) throw new FormatException("songID is not in the valid format.");
            if (songID.Contains("-"))
            {
                songID = songID.Split('-')[0];
            }

            return _bannedSongs.Contains(songID);
        }

        public void SaveBanList()
        {
            using (FileStream fs = new FileStream("UserData/TwitchIntegrationBans.json", FileMode.Create,
                FileAccess.Write))
            {
                byte[] Buffer = Encoding.ASCII.GetBytes(JsonUtility.ToJson(this, true));
                fs.Write(Buffer, 0, Buffer.Length);
            }
        }

        public void LoadBanList()
        {
            using (FileStream fs = new FileStream("UserData/TwitchIntegrationBans.json", FileMode.OpenOrCreate,
                FileAccess.ReadWrite))
            {
                //Lets reset our list before we load.
                _bannedSongs = new List<string>();

                //It didn't exist or there are no bans.
                if (fs.Length == 0) return;
                byte[] BannedSongBytes = new byte[fs.Length];

                //This imposes a limit of 2,147,483,647 characters. Enough to not worry hopefully.
                fs.Read(BannedSongBytes, 0, (int)fs.Length);

                string BannedSongString = Encoding.ASCII.GetString(BannedSongBytes);
                _bannedSongs = JsonUtility.FromJson<BanList>(BannedSongString).GetBanList();

            }
        }

        public List<string> GetBanList()
        {
            return _bannedSongs;
        }
    }
}
