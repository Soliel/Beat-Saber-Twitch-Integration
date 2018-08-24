using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Xml;
using NLog;


namespace TwitchIntegrationPlugin
{
    public class BeatBot
    {
        //private const string Beatsaver = "https://beatsaver.com/";
        private readonly Config _config;
        private readonly Thread _botThread;
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

        private StreamWriter _writer;

        public BeatBot()
        {
            _logger = LogManager.GetCurrentClassLogger();

            if (!Directory.Exists("Plugins\\Config"))
            {
                _logger.Debug("Creating Directory 'Plugins\\Config'");
                Directory.CreateDirectory("Plugins\\Config");
            }

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
        }

        public void Start()
        {
            _botThread.Start();
        }

        public void Exit()
        {
            _retry = false;
            _exit = true;
            _botThread.Abort();
            _botThread.Join();
        }

        public void Initiate()
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
                        SendMessage("CAP REQ :twitch.tv/membership");
                        SendMessage("CAP REQ :twitch.tv/commands");
                        SendMessage("CAP REQ :twitch.tv/tags");

                        _logger.Debug("Login complete Beat bot online.");
                        _logger.Debug(_config.Username);

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
                                    _logger.Info("Responded to twitch ping.");
                                    SendMessage("PONG " + splitInput[1]);
                                    Thread.Sleep(1750);
                                }

                                splitInput = inputLine.Split(':');

                                if (2 >= splitInput.Length) continue;

                                var command = splitInput[2];
                                OnCommandReceived(command, _currUser);
                                Thread.Sleep(1750);
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

        private void OnCommandReceived(string command, string requestedBy)
        {
            var parsedCommand = command.Split();
            if (_isModerator || _isBroadcaster)
            {
                var commandName = parsedCommand[0].ToLower();
                if (commandName == "!next" ||
                    commandName == "!clearall" ||
                    commandName == "!remove" ||
                    commandName == "!block" ||
//                    commandName == "!close" ||
//                    commandName == "!open" ||
                    commandName == "!randomize" ||
                    commandName == "!songinfo" ||
                    commandName == "!unblock")
                {
                    switch (commandName)
                    {
                        case "!next":
                            MoveToNextSongInQueue();
                            break;
                        case "!clearall":
                            RemoveAllSongsFromQueue();
                            break;
                        case "!remove" when parsedCommand.Length > 1:
                            var qs = ApiConnection.GetSongFromBeatSaver(false, parsedCommand[1], requestedBy);
                            if (qs != null)
                            {
                                if (StaticData.QueueList.Count >= 1)
                                {
                                    for (var i = 0; i < StaticData.QueueList.Count; i++)
                                    {
                                        if (!((QueuedSong) StaticData.QueueList[i]).SongName.Contains(qs.SongName))
                                            continue;
                                        StaticData.QueueList.RemoveAt(i);
                                        SendMessage("Removed \"" + qs.SongName + "\" from the queue");
                                    }
                                }
                                else
                                    SendMessage("BeatSaber queue was empty.");
                            }
                            else
                                SendMessage("Couldn't Parse Beatsaver Data.");

                            break;
                        case "!remove":
                            SendMessage("Missing Song ID!");
                            break;
                        case "!block":
                        case "!unblock":
                            if (parsedCommand.Length < 1)
                            {
                                SendMessage("Missing song ID!");
                                return;
                            }

                            BlacklistSong(
                                ApiConnection.GetSongFromBeatSaver(false, parsedCommand[1], ""),
                                commandName == "!block"
                            );
                            break;
                        case "!randomize":
                            var randomizer = new Random();

                            for (var i = 0; (i < _config.RandomizeLimit) && (StaticData.QueueList.Count > 0); i++)
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
                            break;
                        case "!songinfo" when StaticData.QueueList.Count >= 1:
                            SendMessage(
                                "Song Currently Playing: " +
                                "[ID: " + ((QueuedSong) StaticData.QueueList[0]).Id + "], " +
                                "[Song: " + ((QueuedSong) StaticData.QueueList[0]).SongName + "], " +
                                "[Download: " + ((QueuedSong) StaticData.QueueList[0]).DownloadUrl + "]"
                            );
                            break;
                        case "!songinfo":
                            SendMessage("No songs in the queue.");
                            break;
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
            }

            if (_config.ModOnly)
            {
                if (_isModerator || _isBroadcaster)
                {
                    BasicCommands(command, requestedBy);
                }
            }
            else if (_config.SubOnly)
            {
                if (_isModerator || _isBroadcaster || _isSubscriber)
                {
                    BasicCommands(command, requestedBy);
                }
            }
            else if (!_config.ModOnly && !_config.SubOnly)
            {
                BasicCommands(command, requestedBy);
            }
        }

        private void BasicCommands(string command, string requestedBy)
        {
            if (command.StartsWith("!bsr"))
            {
                try
                {
                    var split = command.Remove(0, 5);
                    if (char.IsDigit(split, 0))
                    {
                        AddRequestedSongToQueue(false, split, requestedBy);
                    }
                    else
                    {
                        AddRequestedSongToQueue(true, command.Remove(0, 5), requestedBy);
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                }
            }
            else if (command.StartsWith("!add"))
            {
                try
                {
                    AddRequestedSongToQueue(false, command.Remove(0, 5), requestedBy);
                }
                catch (Exception e)
                {
                    _logger.Error(e);
                }
            }
            else if (command.StartsWith("!queue"))
            {
                DisplaySongsInQueue(StaticData.QueueList, false);
            }
            else if (command.StartsWith("!blist"))
            {
                DisplaySongsInQueue(_banList, true);
            }
            else if (command.StartsWith("!qhelp"))
            {
                SendMessage("These are the valid commands for the Beat Saber Queue system.");
                SendMessage("!add <songId>, !queue, !blist, [Mod only] !next, !clearall, !block <songId>, !unblock <songId>, !open, !close !randomize");
            }
        }

        public void AddRequestedSongToQueue(bool isTextSearch, string queryString, string requestedBy)
        {
            if (!StaticData.TwitchMode)
            {
                if (!_isModerator && !_isBroadcaster)
                {
                    SendMessage("The queue is currently closed.");
                    return;
                }
            }

            var qs = ApiConnection.GetSongFromBeatSaver(isTextSearch, queryString, requestedBy);
            if (qs == null)
            {
                SendMessage("Invalid request");
                return;
            }

            var songExistsInQueue = false;
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
            }
            else
            {
                if (StaticData.QueueList.Count != 0)
                {
                    var internalCounter = 0;

                    foreach (QueuedSong q in StaticData.QueueList)
                    {
                        if (!q.SongName.Contains(qs.SongName)) continue;
                        songExistsInQueue = true;
                        break;
                    }

                    foreach (QueuedSong q in StaticData.QueueList)
                    {
                        if (q.RequestedBy.Equals(requestedBy))
                        {
                            internalCounter++;
                        }
                    }

                    if (!_isBroadcaster)
                    {
                        if (!_isModerator)
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
                    }

                    if (!limitReached)
                    {
                        if (!songExistsInQueue)
                        {
                            StaticData.QueueList.Add(qs);
                            //StaticData.songQueue.Enqueue((QueuedSong)StaticData.queueList[StaticData.queueList.Count - 1]);
                            SendMessage(requestedBy + " added \"" + qs.SongName + "\", uploaded by " + qs.AuthName + " to queue!");
                        }
                        else
                            SendMessage("\"" + qs.SongName + "\" already exists in the queue");
                    }
                    else
                    {
                        SendMessage(requestedBy + ", you've reached the request limit.");
                    }
                }
                else
                {
                    StaticData.QueueList.Add(qs);
                    //StaticData.songQueue.Enqueue((QueuedSong)StaticData.queueList[StaticData.queueList.Count - 1]);
                    SendMessage(requestedBy + " added \"" + qs.SongName + "\", uploaded by " + qs.AuthName + " to queue!");
                    StaticData.SongAddedToQueueEvent.Invoke(qs);
                }
            }
        }

        public void MoveToNextSongInQueue()
        {
            if (StaticData.QueueList.Count < 1)
            {
                SendMessage("BeatSaber queue was empty.");
                return;
            }

            StaticData.QueueList.RemoveAt(0);
            if (StaticData.QueueList.Count == 0)
            {
                SendMessage("Queue is now empty");
                return;
            }

            var remSong = ((QueuedSong) StaticData.QueueList[0]).SongName;
            SendMessage("Removed \"" + remSong + "\" from the queue, next song is \"" +
                        ((QueuedSong) StaticData.QueueList[0]).SongName + "\" requested by " +
                        ((QueuedSong) StaticData.QueueList[0]).RequestedBy);
        }

        public void RemoveAllSongsFromQueue()
        {
            if (StaticData.QueueList.Count != 0)
            {
                StaticData.QueueList.Clear();
                SendMessage("Removed all songs from the BeatSaber queue");
            }
            else
                SendMessage("BeatSaber queue was empty");
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

            if (queue.Count != 0)
            {
                for (var i = 0; i < StaticData.QueueList.Count; i++)
                {
                    if (i < queue.Count - 1)
                    {
                        curr += ((QueuedSong) queue[i]).SongName + ", ";
                    }
                    else
                        curr += ((QueuedSong) queue[i]).SongName;
                }

                SendMessage(curr);
            }
            else
                SendMessage(isEmptyMsg);
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

        /*public void SendQueueToChat(StreamWriter writer)
        {
            var tempList = StaticData.songQueue.ToArray();

            writer.WriteLine("PRIVMSG #" + config.Channel + " :Showing first 5 songs in queue.");
            writer.WriteLine("PRIVMSG #" + config.Channel + " :" + tempList.Length + " songs currently in queue");
            writer.WriteLine("PRIVMSG #" + config.Channel + " :1. " + tempList[0]._beatName);
            writer.WriteLine("PRIVMSG #" + config.Channel + " :2. " + tempList[1]._beatName);
            writer.WriteLine("PRIVMSG #" + config.Channel + " :3. " + tempList[2]._beatName);
            writer.WriteLine("PRIVMSG #" + config.Channel + " :4. " + tempList[3]._beatName);
            writer.WriteLine("PRIVMSG #" + config.Channel + " :5. " + tempList[4]._beatName);
            writer.Flush();
        }*/

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
                    writer.WriteElementString("SendMessageOnConnect", "true");
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
                var modonly = ConvertToBoolean(configNode.SelectSingleNode("ModeratorOnly").InnerText.ToLower());
                var subonly = ConvertToBoolean(configNode.SelectSingleNode("SubscriberOnly").InnerText.ToLower());
                var viewerlimit = ConvertToInteger(configNode.SelectSingleNode("ViewerRequestLimit").InnerText.ToLower());
                var sublimit = ConvertToInteger(configNode.SelectSingleNode("SubscriberLimitOverride").InnerText.ToLower());
                var continuequeue = ConvertToBoolean(configNode.SelectSingleNode("ContinueQueue").InnerText.ToLower());
                var randomize = ConvertToBoolean(configNode.SelectSingleNode("Randomize").InnerText.ToLower());
                var randomizelimit = ConvertToInteger(configNode.SelectSingleNode("RandomizeLimit").InnerText.ToLower());


                return new Config(channel, token, username, modonly, subonly, viewerlimit, sublimit, continuequeue, randomize, randomizelimit);
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

            //logger.Debug(message);
            //logger.Debug("Is Moderator: " + isModerator);
            //logger.Debug("Is Subscriber: " + isSubscriber);
            //logger.Debug("Is Broadcaster: " + isBroadcaster);
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
            if (message.Contains("PASS") || message.Contains("NICK") || message.Contains("JOIN #") || message.Contains("CAP REQ") || message.Contains("PONG"))
            {
                _writer.WriteLine(message);
            }
            else
                _writer.WriteLine("PRIVMSG #" + _config.Channel + " :" + message);

            _writer.Flush();
        }
    }
}