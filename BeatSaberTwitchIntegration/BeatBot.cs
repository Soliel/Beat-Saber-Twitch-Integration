using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Xml;
using NLog;
using System.Linq;


namespace TwitchIntegrationPlugin
{
    class BeatBot
    {
        private
        const String BEATSAVER = "https://beatsaver.com/";
        private Config config;
        private Thread botThread;
        private bool retry = false;
        private bool exit = false;
        private Logger logger;
        EventWaitHandle wait = new AutoResetEvent(false);

        private string currUser = "QueueBot";

        private bool isModerator = false;
        private bool isSubscriber = false;
        private bool isBroadcaster = false;
        private bool isAcceptingRequests = true;

        private ArrayList banList = new ArrayList();
        private ArrayList queueList = new ArrayList();
        private readonly ArrayList randomizedList = new ArrayList();

        private StreamWriter _writer;

        public BeatBot()
        {
            logger = LogManager.GetCurrentClassLogger();

            if (!Directory.Exists("Plugins\\Config"))
            {
                logger.Debug("Creating Dir");
                Directory.CreateDirectory("Plugins\\Config");
            }
            if (!File.Exists("Plugins\\Config\\song_blacklist.txt"))
            {
                logger.Info("Creating File");
                using (FileStream fs = File.Create("Plugins\\Config\\song_blacklist.txt"))
                {
                    logger.Debug("Created Blacklist File");
                }
            }
            else
            {
                StreamReader file = new StreamReader("Plugins\\Config\\song_blacklist.txt");
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    logger.Info(line);
                    banList.Add(line);
                }

                file.Close();
            }

            config = ReadCredsFromConfig();
            botThread = new Thread(new ThreadStart(Initiate));
        }

        public void Start()
        {
            botThread.Start();
        }

        public void Exit()
        {
            retry = false;
            exit = true;
            botThread.Abort();
            botThread.Join();
        }

        public void Initiate()
        {
            Console.WriteLine("Twitch bot starting...");
            var retryCount = 0;

            if (config == null) return;

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
                        logger.Debug("Connection to twitch server established. Beginning Login.");
                        SendMessage("PASS " + config.Token);
                        SendMessage("NICK " + config.Username);
                        SendMessage("JOIN #" + config.Channel);

                        // Adding Capabilities Requests so that we can parse Viewer information
                        SendMessage("CAP REQ :twitch.tv/membership");
                        SendMessage("CAP REQ :twitch.tv/commands");
                        SendMessage("CAP REQ :twitch.tv/tags");

                        logger.Debug("Login complete Beat bot online.");
                        logger.Debug(config.Username);

                        while (!exit)
                        {
                            string inputLine;
                            while ((inputLine = reader.ReadLine()) != null || exit)
                            {
                                logger.Debug(inputLine);
                                ProcessIRCMessage(inputLine);
                                string[] splitInput = inputLine.Split(' ');
                                if (splitInput[0] == "PING")
                                {
                                    logger.Info("Responded to twitch ping.");
                                    SendMessage("PONG " + splitInput[1]);
                                }

                                splitInput = inputLine.Split(':');
                                if (2 < splitInput.Length)
                                {
                                    String command = splitInput[2];
                                    OnCommandRecieved(command, currUser);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.Debug(e.ToString());
                    Thread.Sleep(5000);
                    retry = ++retryCount <= 20;
                    if(exit)
                    {
                        retry = false;
                    }
                }
            } while (retry);
        }

        public void OnCommandRecieved(String command, String requestedBy)
        {
            string[] parsedCommand = command.Split(new char[0]);
            if (isModerator || isBroadcaster)
            {
                if ((command.StartsWith("!next") || command.StartsWith("!clearall") || command.StartsWith("!remove") || command.StartsWith("!block") ||
                command.StartsWith("!close") || command.StartsWith("!open") || command.StartsWith("!randomize")) || command.StartsWith("!songinfo"))
                {
                    if (command.StartsWith("!next"))
                    {
                        MoveToNextSongInQueue();
                    }
                    else if (command.StartsWith("!clearall"))
                    {
                        RemoveAllSongsFromQueue();
                    }
                    else if (command.StartsWith("!remove"))
                    {
                        if (parsedCommand.Length > 1)
                        {
                            QueuedSong qs = APIConnection.GetSongFromBeatSaver(false, parsedCommand[1], requestedBy);
                            if (qs != null)
                            {
                                if (queueList.Count >= 1)
                                {
                                    for (int i = 0; i < queueList.Count; i++)
                                    {
                                        if (((QueuedSong)queueList[i])._songName.Contains(qs._songName))
                                        {
                                            queueList.RemoveAt(i);
                                            SendMessage("Removed \"" + qs._songName + "\" from the queue");
                                        }
                                    }
                                }
                                else
                                    SendMessage("BeatSaber queue was empty.");
                            }
                            else
                                SendMessage("Couldn't Parse Beatsaver Data.");
                        }
                        else
                            SendMessage("Missing Song ID!");
                    }
                    else if (command.StartsWith("!block"))
                    {
                        if (parsedCommand.Length > 1)
                        {
                            BlacklistSong(APIConnection.GetSongFromBeatSaver(false, parsedCommand[1], ""));
                        }
                        else
                            SendMessage("Missing song ID");

                    }
                    else if (command.StartsWith("!close"))
                    {
                        if (isAcceptingRequests)
                        {
                            isAcceptingRequests = false;
                            SendMessage("The queue has been closed!");
                        }
                        else
                        {
                            SendMessage("The queue was already closed.");
                        }
                    }
                    else if (command.StartsWith("!open"))
                    {
                        if (!isAcceptingRequests)
                        {
                            isAcceptingRequests = true;
                            SendMessage("The queue has been opened!");
                        }
                        else
                        {
                            SendMessage("The queue is already open.");
                        }
                    }
                    else if (command.StartsWith("!randomize"))
                    {
                        Random randomizer = new Random();

                        for (int i = 0; (i < config.RandomizeLimit) && (queueList.Count > 0); i++)
                        {
                            int randomIndex = randomizer.Next(queueList.Count);
                            QueuedSong randomSong = (QueuedSong)queueList[randomIndex];
                            //System.out.println(" " + randomSong.getFirst());
                            randomizedList.Add(randomSong);
                            queueList.RemoveAt(randomIndex);
                        }

                        queueList.Clear();
                        queueList.AddRange(randomizedList);
                        randomizedList.Clear();
                        SendMessage(config.RandomizeLimit + " songs were randomly chosen from queue!");
                        DisplaySongsInQueue(queueList, false);
                    }
                    else if (command.StartsWith("!songinfo"))
                    {
                        if (queueList.Count >= 1)
                        {
                            SendMessage("Song Currently Playing: " +
                            "[ID: " + ((QueuedSong)queueList[0])._id + "], " +
                            "[Song: " + ((QueuedSong)queueList[0])._songName + "], " +
                            "[Download: " + ((QueuedSong)queueList[0])._downloadUrl + "]");
                        }
                        else
                            SendMessage("No songs in the queue.");
                    }
                }
            }
            else if (config.ModOnly)
            {
                if (isModerator || isBroadcaster)
                {
                    BasicCommands(command, requestedBy);
                }
            }
            else if (config.SubOnly)
            {
                if (isModerator || isBroadcaster || isSubscriber)
                {
                    BasicCommands(command, requestedBy);
                }
            }
            else if (!config.ModOnly && !config.SubOnly)
            {
                BasicCommands(command, requestedBy);
            }
        }

        private void BasicCommands(String command, String requestedBy)
        {
            if (command.StartsWith("!bsr"))
            {
                AddRequestedSongToQueue(true, command.Remove(0, 5), requestedBy);
            }
            else if (command.StartsWith("!add"))
            {
                AddRequestedSongToQueue(false, command.Remove(0, 5), requestedBy);
            }
            else if (command.StartsWith("!queue"))
            {
                DisplaySongsInQueue(queueList, false);
            }
            else if (command.StartsWith("!blist"))
            {
                DisplaySongsInQueue(banList, true);
            }
            else if (command.StartsWith("!qhelp"))
            {
                SendMessage("These are the valid commands for the Beat Saber Queue system.");
                SendMessage("!add <songId>, !queue, !blist, [Mod only] !next, !clearall, !block <songId>, !open, !close !randomize");
            }
        }

        public void AddRequestedSongToQueue(Boolean isTextSearch, String queryString, String requestedBy)
        {
            if (!isAcceptingRequests)
            {
                if (!isModerator && !isBroadcaster)
                {
                    SendMessage("The queue is currently closed.");
                    return;
                }
            }

            QueuedSong qs = APIConnection.GetSongFromBeatSaver(isTextSearch, queryString, requestedBy);
            bool songExistsInQueue = false;
            bool limitReached = false;

            bool isAlreadyBanned = false;

            // Check to see if song is banned.
            foreach (String banValue in banList)
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
                if (queueList.Count != 0)
                {
                    int internalCounter = 0;

                    foreach (QueuedSong q in queueList)
                    {
                        if (q._songName.Contains(qs._songName))
                        {
                            songExistsInQueue = true;
                            break;
                        }
                    }

                    foreach (QueuedSong q in queueList)
                    {
                        if (q._requestedBy.Equals(requestedBy))
                        {
                            internalCounter++;
                        }
                    }

                    if (!isBroadcaster)
                    {
                        if (!isModerator)
                        {
                            if (config.SubLimit == config.ViewerLimit)
                            {
                                limitReached = internalCounter == config.ViewerLimit;
                            }
                            else if (!isSubscriber && (internalCounter == config.ViewerLimit))
                            {
                                limitReached = true;
                            }
                            else if (isSubscriber && (internalCounter == config.SubLimit))
                            {
                                limitReached = true;
                            }
                        }
                    }
                    if (!limitReached)
                    {
                        if (!songExistsInQueue)
                        {
                            queueList.Add(qs);
                            StaticData.songQueue.Enqueue((QueuedSong)queueList[queueList.Count - 1]);
                            SendMessage(requestedBy + " added \"" + qs._songName + "\", uploaded by " + qs._authName + " to queue!");
                        }
                        else
                            SendMessage("\"" + qs._songName + "\" already exists in the queue");
                    }
                    else
                    {
                        SendMessage(requestedBy + ", you've reached the request limit.");
                    }
                }
                else
                {
                    queueList.Add(qs);
                    StaticData.songQueue.Enqueue((QueuedSong)queueList[queueList.Count - 1]);
                    SendMessage(requestedBy + " added \"" + qs._songName + "\", uploaded by " + qs._authName + " to queue!");
                }
            }
        }

        public void MoveToNextSongInQueue()
        {
            if (queueList.Count >= 1)
            {
                String remSong = ((QueuedSong)queueList[0])._songName;
                queueList.RemoveAt(0);

                if (queueList.Count != 0)
                {
                    SendMessage("Removed \"" + remSong + "\" from the queue, next song is \"" + ((QueuedSong)queueList[0])._songName + "\" requested by " + ((QueuedSong)queueList[0])._requestedBy);
                }
                else
                    SendMessage("Queue is now empty");
            }
            else
                SendMessage("BeatSaber queue was empty.");
        }

        public void RemoveAllSongsFromQueue()
        {
            if (queueList.Count != 0)
            {
                queueList.Clear();
                SendMessage("Removed all songs from the BeatSaber queue");
            }
            else
                SendMessage("BeatSaber queue was empty");
        }

        public void DisplaySongsInQueue(ArrayList queue, Boolean isBanlist)
        {
            string curr = "Current song list: ";
            string isEmptyMsg = "No songs in the queue";
            if (isBanlist)
            {
                curr = "Current banned list: ";
                isEmptyMsg = "No songs currently banned.";
            }

            if (queue.Count != 0)
            {
                for (int i = 0; i < queueList.Count; i++)
                {
                    if (i < queue.Count - 1)
                    {
                        curr += ((QueuedSong)queue[i])._songName + ", ";
                    }
                    else
                        curr += ((QueuedSong)queue[i])._songName;
                }

                SendMessage(curr);
            }
            else
                SendMessage(isEmptyMsg);
        }

        private void BlacklistSong(QueuedSong song)
        {
            if (song._id != null)
            {
                if (banList.Contains(song._id))
                {
                    SendMessage("Song already on banlist");
                }
                else
                {
                    banList.Add(song._id);
                    if (File.Exists("Plugins\\Config\\song_blacklist.txt"))
                    {
                        File.AppendAllText("Plugins\\Config\\song_blacklist.txt", song._id + Environment.NewLine);
                        SendMessage("Added \"" + song._songName + "\", uploaded by " + song._authName + " to banlist!");
                    }
                }
            }
        }

        public void SendQueueToChat(StreamWriter writer)
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
        }

        private Config ReadCredsFromConfig()
        {
            string channel;
            string token;
            string username;
            bool modonly;
            bool subonly;
            int viewerlimit;
            int sublimit;
            bool continuequeue;
            bool randomize;
            int randomizelimit;

            if (!Directory.Exists("Plugins\\Config"))
            {
                Directory.CreateDirectory("Plugins\\Config");
            }

            if (!File.Exists("Plugins\\Config\\TwitchIntegration.xml"))
            {
                using (XmlWriter writer = XmlWriter.Create("Plugins\\Config\\TwitchIntegration.xml"))
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
                XmlDocument doc = new XmlDocument();
                doc.Load("Plugins\\Config\\TwitchIntegration.xml");
                XmlNode configNode = doc.SelectSingleNode("Config");

                username = configNode.SelectSingleNode("Username").InnerText.ToLower();
                token = configNode.SelectSingleNode("Oauth").InnerText;
                channel = configNode.SelectSingleNode("Channel").InnerText.ToLower();
                modonly = ConvertToBoolean(configNode.SelectSingleNode("ModeratorOnly").InnerText.ToLower());
                subonly = ConvertToBoolean(configNode.SelectSingleNode("SubscriberOnly").InnerText.ToLower());
                viewerlimit = ConvertToInteger(configNode.SelectSingleNode("ViewerRequestLimit").InnerText.ToLower());
                sublimit = ConvertToInteger(configNode.SelectSingleNode("SubscriberLimitOverride").InnerText.ToLower());
                continuequeue = ConvertToBoolean(configNode.SelectSingleNode("ContinueQueue").InnerText.ToLower());
                randomize = ConvertToBoolean(configNode.SelectSingleNode("Randomize").InnerText.ToLower());
                randomizelimit = ConvertToInteger(configNode.SelectSingleNode("RandomizeLimit").InnerText.ToLower());


                return new Config(channel, token, username, modonly, subonly, viewerlimit, sublimit, continuequeue, randomize, randomizelimit);
            }
            catch (Exception e)
            {
                logger.Error("Error loading xml file: " + e);
                return null;
            }
        }

        private void ProcessIRCMessage(string message)
        {
            if (message.Contains("PRIVMSG"))
            {
                string[] splitInput = message.Split(';');

                isBroadcaster = splitInput[0].Contains("broadcaster");
                isModerator = splitInput[5].Contains("mod=1");
                isSubscriber = splitInput[7].Contains("subscriber=1");
                currUser = splitInput[2].Split('=')[1];

                //logger.Debug(message);
                //logger.Debug("Is Moderator: " + isModerator);
                //logger.Debug("Is Subscriber: " + isSubscriber);
                //logger.Debug("Is Broadcaster: " + isBroadcaster);
            }

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
                logger.Error("Value should be a Boolean, but doesn't convert. Default of \"False\" being used.");
                return false;
            }
        }

        private int ConvertToInteger(string value)
        {
            try
            {
                return Int32.Parse(value);
            }
            catch (FormatException)
            {
                logger.Error("Value should be a Integer, but doesn't convert. Default of \"5\" being used.");
                return 5;
            }
        }

        private void SendMessage(String message)
        {
            if (message.Contains("PASS") || message.Contains("NICK") || message.Contains("JOIN #") || message.Contains("CAP REQ") || message.Contains("PONG"))
            {
                _writer.WriteLine(message);
            }
            else
                _writer.WriteLine("PRIVMSG #" + config.Channel + " :" + message);

            _writer.Flush();
        }
    }
}