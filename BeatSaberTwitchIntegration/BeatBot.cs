using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Xml;
using NLog;
using SimpleJSON;


namespace TwitchIntegrationPlugin
{
    public class BeatBot
    {
        //private const string Beatsaver = "https://beatsaver.com/";
        private readonly Config _config;
        private readonly Thread _botThread;
        private readonly Thread _queueThread;
        private bool _retry;
        private bool _exit;

        private readonly Logger _logger;
        //EventWaitHandle _wait = new AutoResetEvent(false);

        private string _currUser = "QueueBot";

        private bool _isModerator;
        private bool _isSubscriber;

        private bool _isBroadcaster;
        //private bool _isAcceptingRequests; No longer needed due to combination with twitch mode button.

        private readonly ArrayList _banList = new ArrayList();

        private readonly ArrayList _randomizedList = new ArrayList();

        private readonly ArrayList _messageQueue = new ArrayList();

        private StreamWriter _writer;

        private void CreateConfigDirectory()
        {
            if (Directory.Exists("Plugins\\Config")) return;

            _logger.Debug("Creating Directory 'Plugins\\Config'");
            Directory.CreateDirectory("Plugins\\Config");
        }

        public BeatBot()
        {
            _logger = LogManager.GetCurrentClassLogger();

            CreateConfigDirectory();
            LoadQueue();

            if (!File.Exists("Plugins\\Config\\song_blacklist.txt"))
            {
                _logger.Info("Creating Blacklist File");
                using (File.Create("Plugins\\Config\\song_blacklist.txt"))
                {
                    _logger.Debug("Created Blacklist File");
                }
            }
            else
            {
                var file = new StreamReader("Plugins\\Config\\song_blacklist.txt");
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    _logger.Info(line);
                    _banList.Add(line);
                }

                file.Close();
            }

            _config = ReadCredsFromConfig();
            _botThread = new Thread(Initiate);
            _queueThread = new Thread(SendMessageQueue); // send messages from queue
        }

        public void Start()
        {
            _botThread.Start();
            _queueThread.Start();
        }

        public void Exit()
        {
            _retry = false;
            _exit = true;
            _botThread.Abort();
            _botThread.Join();
            _queueThread.Abort();
            _queueThread.Join();

            SaveQueue();
        }

        /**
         * load queue from song_queue.txt
         */
        private void LoadQueue()
        {
            // create config directory if not exists
            CreateConfigDirectory();

            // ignore load if file doesn't exists
            if (!File.Exists("Plugins\\Config\\song_queue.txt")) return;

            // get json from file
            var file = new StreamReader("Plugins\\Config\\song_queue.txt");
            var json = "";
            string line;
            while ((line = file.ReadLine()) != null)
            {
                json += line;
            }

            var node = JSON.Parse(json);
            for (var i = 0; i < node.Count; i++)
            {
                var song = node[i];
                var qs = new QueuedSong(
                    song["songName"],
                    song["name"],
                    song["authorName"],
                    song["bpm"],
                    song["id"],
                    song["songSubName"],
                    song["downloadUrl"],
                    song["requestedBy"],
                    song["coverUrl"],
                    song["songHash"]
                );
                AddQueueSong(qs, false);
            }

            file.Close();
        }

        /**
         * save queue to song_queue.txt
         */
        private void SaveQueue()
        {
            // create config directory if not exists
            CreateConfigDirectory();

            // delete old song_queue.txt
            if (File.Exists("Plugins\\Config\\song_queue.txt"))
            {
                File.Delete("Plugins\\Config\\song_queue.txt");
            }

            // build json string -> todo json class?
            var json = "";
            foreach (var song in StaticData.QueueList)
            {
                if (json.Length > 0)
                {
                    json += ", ";
                }

                json += song.ToString();
            }

            // save into file
            File.WriteAllText("Plugins\\Config\\song_queue.txt", "[" + json + "]");
        }

        /**
         * check if queue has messages to send to irc server 
         */
        private void SendMessageQueue()
        {
            _logger.Debug("starting queue thread ;)");
            while (!_exit)
            {
                // skip while if messageQueue is empty :)
                if (_messageQueue.Count == 0) continue;
                // send message to irc server and wait 1750ms (because twitch rate limit)
                _writer.WriteLine(_messageQueue[0]);
                _writer.Flush();
                _messageQueue.RemoveAt(0);
                Thread.Sleep(1750);
            }
        }

        private void Initiate()
        {
            _logger.Debug("Twitch bot starting...");
            var retryCount = 0;

            if (_config == null) return;

            do
            {
                try
                {
                    using (var irc = new TcpClient("irc.chat.twitch.tv", 6667))
                    using (var stream = irc.GetStream())
                    using (var reader = new StreamReader(stream))
                    using (var writer = new StreamWriter(stream))
                    {
                        // Set a global Writer
                        _writer = writer;

                        // Login Information for the irc client
                        _logger.Debug("Connection to twitch server established. Beginning Login.");
                        SendMessage("PASS " + _config.Token);
                        SendMessage("NICK " + _config.Username);
                        SendMessage("JOIN #" + _config.Channel);

                        // Adding Capabilities Requests so that we can parse Viewer information
                        SendMessage("CAP REQ :twitch.tv/membership twitch.tv/commands twitch.tv/tags");

                        _logger.Debug("Login complete Beat bot online. -> " + _config.Username);

                        while (!_exit)
                        {
                            string inputLine;
                            while ((inputLine = reader.ReadLine()) != null || _exit)
                            {
                                _logger.Debug(inputLine);
                                ProcessIrcMessage(inputLine);

                                if (inputLine == null) continue;

                                var splitInput = inputLine.Split(' ');
                                if (splitInput[0] == "PING")
                                {
                                    SendMessage("PONG " + splitInput[1]);
                                    continue;
                                }

                                // ignore server stuff there not privmsg or whisper (remove whisper?)
                                if (splitInput[2] != "PRIVMSG" && splitInput[2] != "WHISPER") continue;

                                splitInput = inputLine.Split(':');
                                OnCommandReceived(splitInput[2], _currUser);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Debug("----------");
                    _logger.Debug(e.ToString());
                    _logger.Debug("----------");
                    _retry = ++retryCount <= 20;
                    if (_exit)
                    {
                        _retry = false;
                    }
                    else
                    {
                        Thread.Sleep(5000);
                    }
                }
            } while (_retry);
        }

        private void OnCommandReceived(string command, string username)
        {
            var parameters = command.Split();
            var commandName = parameters[0].Trim().ToLower();

            if (_isModerator || _isBroadcaster)
            {
                switch (commandName)
                {
                    case "!next":
                        MoveToNextSongInQueue();
                        return;
                    case "!clearall":
                        RemoveAllSongsFromQueue();
                        return;
                    case "!remove":
                        if (parameters.Length < 2)
                        {
                            SendMessage("Missing Song ID!");
                            return;
                        }

                        if (StaticData.QueueList.Count < 1)
                        {
                            SendMessage("BeatSaber queue was empty.");
                            return;
                        }

                        var qs = ApiConnection.GetSongFromBeatSaver(false, parameters[1], username);
                        if (qs == null)
                        {
                            SendMessage("Couldn't Parse BeatSaver Data.");
                            return;
                        }

                        for (var i = 0; i < StaticData.QueueList.Count; i++)
                        {
                            if (!((QueuedSong) StaticData.QueueList[i]).SongName.Contains(qs.SongName))
                                continue;
                            StaticData.QueueList.RemoveAt(i);
                            SendMessage("Removed \"" + qs.SongName + "\" from the queue");
                            return;
                        }

                        SendMessage("\"" + qs.SongName + "\" is not in the queue");

                        return;
                    case "!block":
                    case "!unblock":
                        if (parameters.Length < 2)
                        {
                            SendMessage("Missing song ID!");
                            return;
                        }

                        BlacklistSong(
                            ApiConnection.GetSongFromBeatSaver(false, parameters[1], ""),
                            commandName == "!block"
                        );
                        return;
                    case "!randomize":
                        var randomizer = new Random();

                        for (var i = 0; StaticData.QueueList.Count > 0 && i < _config.RandomizeLimit; i++)
                        {
                            var randomIndex = randomizer.Next(StaticData.QueueList.Count);
                            var randomSong = (QueuedSong) StaticData.QueueList[randomIndex];
                            //System.out.println(" " + randomSong.getFirst());
                            _randomizedList.Add(randomSong);
                            StaticData.QueueList.RemoveAt(randomIndex);
                        }

                        StaticData.QueueList.Clear();
                        StaticData.QueueList.AddRange(_randomizedList);
                        _randomizedList.Clear();
                        SendMessage(_config.RandomizeLimit + " songs were randomly chosen from queue!");
                        DisplaySongsInQueue(StaticData.QueueList, false);
                        return;
                    case "!songinfo":
                        if (StaticData.QueueList.Count < 1)
                        {
                            SendMessage("No songs in the queue.");
                            return;
                        }

                        SendMessage(
                            "Song Currently Playing: " +
                            "[ID: " + ((QueuedSong) StaticData.QueueList[0]).Id + "], " +
                            "[Song: " + ((QueuedSong) StaticData.QueueList[0]).SongName + "], " +
                            "[Download: " + ((QueuedSong) StaticData.QueueList[0]).DownloadUrl + "]"
                        );
                        return;
                    //This block is unnessecary now that queue has been moved to rely on twitch button.
                    /*else if (command.StartsWith("!close"))
                {
                    if (_isAcceptingRequests)
                    {
                        _isAcceptingRequests = false;
                        SendMessage("The queue has been closed!");
                    }
                    else
                    {
                        SendMessage("The queue was already closed.");
                    }
                }
                else if (command.StartsWith("!open"))
                {
                    if (!_isAcceptingRequests)
                    {
                        _isAcceptingRequests = true;
                        SendMessage("The queue has been opened!");
                    }
                    else
                    {
                        SendMessage("The queue is already open.");
                    }
                }*/
                }
            }

            if (
                // mod only and is mod or broadcaster
                _config.ModOnly && (_isModerator || _isBroadcaster) ||
                // sub only and is mod, broadcaster or sub
                _config.SubOnly && (_isModerator || _isBroadcaster || _isSubscriber) ||
                // no mod only and no sub only -> everyone can use command
                !_config.ModOnly && !_config.SubOnly
            )
            {
                BasicCommands(commandName, parameters, username);
            }
        }

        private void BasicCommands(string commandName, IReadOnlyList<string> parameters, string username)
        {
            switch (commandName)
            {
                case "!bsr":
                    if (parameters.Count < 2)
                    {
                        SendMessage("Missing song ID or song name!");
                        return;
                    }

                    try
                    {
                        var split = string.Join(" ", parameters.Skip(1));
                        AddRequestedSongToQueue(!char.IsDigit(split, 0), split, username);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e);
                    }

                    break;
                case "!add":
                    if (parameters.Count < 2)
                    {
                        SendMessage("Missing song ID or song name!");
                        return;
                    }

                    try
                    {
                        AddRequestedSongToQueue(false, parameters[1], username);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e);
                    }

                    break;
                case "!queue":
                    DisplaySongsInQueue(StaticData.QueueList, false);
                    break;
                case "!blist":
                    DisplaySongsInQueue(_banList, true);
                    break;
                case "!qhelp":
                    SendMessage("These are the valid commands for the Beat Saber Queue system.");
                    SendMessage("!add <songId>, !queue, !blist, [Mod only] !next, !clearall, !block <songId>, !unblock <songId>, !open, !close !randomize");
                    break;
            }
        }

        public void AddRequestedSongToQueue(bool isTextSearch, string queryString, string requestedBy)
        {
            if (!StaticData.TwitchMode && !_isModerator && !_isBroadcaster)
            {
                SendMessage("The queue is currently closed.");
                return;
            }

            var qs = ApiConnection.GetSongFromBeatSaver(isTextSearch, queryString, requestedBy);
            if (qs == null)
            {
                SendMessage("Invalid request");
                return;
            }

            var limitReached = false;
            var isAlreadyBanned = false;

            // Check to see if song is banned.
            foreach (string banValue in _banList)
            {
                if (queryString.Contains("-"))
                {
                    if (banValue.Contains(queryString.Split('-')[0]))
                    {
                        isAlreadyBanned = true;
                    }
                }
                else if (banValue.Contains(queryString))
                {
                    isAlreadyBanned = true;
                }
            }

            // Check for song against Blacklist, else proceed to get Song information
            if (isAlreadyBanned)
            {
                SendMessage("Song is currently Blacklisted");
                return;
            }

            if (StaticData.QueueList.Count == 0)
            {
                AddQueueSong(qs, true);
                return;
            }

            var internalCounter = 0;
            foreach (QueuedSong q in StaticData.QueueList)
            {
                if (!q.SongName.Contains(qs.SongName)) continue;

                SendMessage("\"" + qs.SongName + "\" already exists in the queue");
                return;
            }

            foreach (QueuedSong q in StaticData.QueueList)
            {
                if (q.RequestedBy.Equals(requestedBy))
                {
                    internalCounter++;
                }
            }

            // Todo optimize if...
            if (!_isBroadcaster && !_isModerator)
            {
                if (_config.SubLimit == _config.ViewerLimit)
                {
                    limitReached = internalCounter == _config.ViewerLimit;
                }
                else if (!_isSubscriber && (internalCounter == _config.ViewerLimit))
                {
                    limitReached = true;
                }
                else if (_isSubscriber && (internalCounter == _config.SubLimit))
                {
                    limitReached = true;
                }
            }

            if (limitReached)
            {
                SendMessage(requestedBy + ", you've reached the request limit.");
                return;
            }

            AddQueueSong(qs, true);
        }

        /**
         * add a song to the queue
         */
        public void AddQueueSong(QueuedSong qs, bool sendMessage)
        {
            StaticData.QueueList.Add(qs);
            if (StaticData.QueueList.Count == 1) StaticData.SongAddedToQueueEvent?.Invoke(qs);

            if (sendMessage)
            {
                SendMessage(qs.RequestedBy + " added \"" + qs.SongName + "\", uploaded by " + qs.AuthName + " to queue!");
            }
        }

        public void MoveToNextSongInQueue()
        {
            if (StaticData.QueueList.Count < 1)
            {
                SendMessage("BeatSaber queue is empty.");
                return;
            }

            StaticData.QueueList.RemoveAt(0);
            if (StaticData.QueueList.Count == 0)
            {
                SendMessage("Queue is now empty.");
                return;
            }

            var remSong = ((QueuedSong) StaticData.QueueList[0]).SongName;
            SendMessage(
                "Removed \"" + remSong + "\" from the queue, next song is \"" +
                ((QueuedSong) StaticData.QueueList[0]).SongName + "\" requested by " +
                ((QueuedSong) StaticData.QueueList[0]).RequestedBy
            );
        }

        public void RemoveAllSongsFromQueue()
        {
            if (StaticData.QueueList.Count == 0)
            {
                SendMessage("BeatSaber queue was empty");
                return;
            }

            StaticData.QueueList.Clear();
            SendMessage("Removed all songs from the BeatSaber queue");
        }

        public void DisplaySongsInQueue(ArrayList queue, bool isBanlist)
        {
            var curr = "Current song list: ";
            var isEmptyMsg = "No songs in the queue";
            if (isBanlist)
            {
                curr = "Current banned list: ";
                isEmptyMsg = "No songs currently banned.";
            }

            if (queue.Count == 0)
            {
                SendMessage(isEmptyMsg);
                return;
            }

            for (var i = 0; i < StaticData.QueueList.Count; i++)
            {
                curr += ((QueuedSong) queue[i]).SongName;
                if (i < queue.Count - 1)
                {
                    curr += ", ";
                }
            }

            SendMessage(curr);
        }

        private void BlacklistSong(QueuedSong song, bool isAdd)
        {
            if (song.Id == null) return;
            if (isAdd && _banList.Contains(song.Id) || !isAdd && !_banList.Contains(song.Id))
            {
                SendMessage(isAdd ? "Song already on banlist." : "Song is not on banlist.");
                return;
            }

            if (isAdd)
            {
                _banList.Add(song.Id);
            }
            else
            {
                _banList.Remove(song.Id);
            }

            if (!File.Exists("Plugins\\Config\\song_blacklist.txt")) return;

            // add song to blacklist.txt
            if (isAdd)
            {
                // add song id to file
                File.AppendAllText("Plugins\\Config\\song_blacklist.txt", song.Id + Environment.NewLine);
                // send feedback
                SendMessage("Added \"" + song.SongName + "\", uploaded by " + song.AuthName + " to banlist!");

                return;
            }

            // remove song from blacklist.txt (list remaining songs)
            var bannedSongs = "";
            foreach (string banValue in _banList)
            {
                bannedSongs += banValue + Environment.NewLine;
            }

            File.WriteAllText("Plugins\\Config\\song_blacklist.txt", bannedSongs);

            // send feedback
            SendMessage("Removed \"" + song.SongName + "\", uploaded by " + song.AuthName + " from banlist!");
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private Config ReadCredsFromConfig()
        {
            if (!Directory.Exists("Plugins\\Config"))
            {
                Directory.CreateDirectory("Plugins\\Config");
            }

            if (!File.Exists("Plugins\\Config\\TwitchIntegration.xml"))
            {
                using (var writer = XmlWriter.Create("Plugins\\Config\\TwitchIntegration.xml"))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Config");
                    writer.WriteElementString("Username", "Twitch Username");
                    writer.WriteElementString("Oauth", "Oauth Token");
                    writer.WriteElementString("Channel", "Channel Name");
                    writer.WriteElementString("ModeratorOnly", "false");
                    writer.WriteElementString("SubscriberOnly", "false");
                    writer.WriteElementString("ViewerRequestLimit", "5");
                    writer.WriteElementString("SubscriberLimitOverride", "5");
                    writer.WriteElementString("ContinueQueue", "false");
                    writer.WriteElementString("Randomize", "false");
                    writer.WriteElementString("RandomizeLimit", "5");
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }

                return null;
            }

            try
            {
                var doc = new XmlDocument();
                doc.Load("Plugins\\Config\\TwitchIntegration.xml");
                var configNode = doc.SelectSingleNode("Config");

                var username = configNode.SelectSingleNode("Username").InnerText.ToLower();
                var token = configNode.SelectSingleNode("Oauth").InnerText;
                var channel = configNode.SelectSingleNode("Channel").InnerText.ToLower();
                var modOnly = ConvertToBoolean(configNode.SelectSingleNode("ModeratorOnly").InnerText.ToLower());
                var subOnly = ConvertToBoolean(configNode.SelectSingleNode("SubscriberOnly").InnerText.ToLower());
                var viewerLimit = ConvertToInteger(configNode.SelectSingleNode("ViewerRequestLimit").InnerText.ToLower());
                var subLimit = ConvertToInteger(configNode.SelectSingleNode("SubscriberLimitOverride").InnerText.ToLower());
                var continueQueue = ConvertToBoolean(configNode.SelectSingleNode("ContinueQueue").InnerText.ToLower());
                var randomize = ConvertToBoolean(configNode.SelectSingleNode("Randomize").InnerText.ToLower());
                var randomizeLimit = ConvertToInteger(configNode.SelectSingleNode("RandomizeLimit").InnerText.ToLower());

                return new Config(channel, token, username, modOnly, subOnly, viewerLimit, subLimit, continueQueue, randomize, randomizeLimit);
            }
            catch (Exception e)
            {
                _logger.Error("Error loading xml file: " + e);
                return null;
            }
        }

        private void ProcessIrcMessage(string message)
        {
            if (!message.Contains("PRIVMSG")) return;
            var splitInput = message.Split(';');

            _isBroadcaster = splitInput[0].Contains("broadcaster");
            _isModerator = splitInput[5].Contains("mod=1");
            _isSubscriber = splitInput[7].Contains("subscriber=1");
            _currUser = splitInput[2].Split('=')[1];
        }

        // Wanted defaults set when conversions failed, so I wrapped in a try/catch block and assigned defaults.
        private bool ConvertToBoolean(string value)
        {
            try
            {
                return Convert.ToBoolean(value);
            }
            catch (FormatException)
            {
                _logger.Error("Value should be a Boolean, but doesn't convert. Default of \"False\" being used.");
                return false;
            }
        }

        private int ConvertToInteger(string value)
        {
            try
            {
                return int.Parse(value);
            }
            catch (FormatException)
            {
                _logger.Error("Value should be a Integer, but doesn't convert. Default of \"5\" being used.");
                return 5;
            }
        }

        private void SendMessage(string message)
        {
            // ignore pong, it should be not send with a sleep
            // ignore others only on start (faster bot start)
            if (message.StartsWith("PASS") || message.StartsWith("NICK") || message.StartsWith("JOIN #") || message.StartsWith("CAP REQ") || message.StartsWith("PONG"))
            {
                _writer.WriteLine(message);
                _writer.Flush();
            }
            else
            {
                _messageQueue.Add("PRIVMSG #" + _config.Channel + " :" + message);
            }
        }
    }
}