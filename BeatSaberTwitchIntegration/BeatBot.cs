using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine.Networking;
using SimpleJSON;
using System.Xml;
using NLog;
using System.Linq;

namespace TwitchIntegrationPlugin
{
    class BeatBot
    {
        private const String BEATSAVER = "https://beatsaver.com";
        private Config config;
        private Thread botThread;
        private bool retry = false;
        private bool exit = false;
        private Logger logger;
        EventWaitHandle wait = new AutoResetEvent(false);
       

        public BeatBot()
        {
            logger = LogManager.GetCurrentClassLogger();

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
                        logger.Debug("Connection to twitch server established. Beginning Login.");
                        writer.WriteLine("PASS " + config.Token);
                        writer.Flush();
                        writer.WriteLine("NICK " + config.Username);
                        writer.Flush();
                        writer.WriteLine("JOIN #" + config.Channel);
                        writer.Flush();

                        logger.Debug("Login complete Beat bot online.");
                        logger.Debug(config.Username);

                        while (!exit)
                        {
                            string inputLine;
                            while ((inputLine = reader.ReadLine()) != null || exit )
                            {

                                string[] splitInput = inputLine.Split(' ');
                                if (splitInput[0] == "PING")
                                {
                                    logger.Info("Responded to twitch ping.");
                                    writer.WriteLine("PONG " + splitInput[1]);
                                    writer.Flush();
                                }

                                splitInput = inputLine.Split(':');
                                if (2 < splitInput.Length)
                                {
                                    if (splitInput[2].StartsWith("!bsr ")) {
                                        var queryString = splitInput[2].Remove(0, 5);
                                        logger.Info("Command Recieved with query: " + queryString);
                                        if(queryString.StartsWith("https"))
                                        {
                                            queryString = splitInput[3];
                                        }
                                        OnCommandRecieved(writer, queryString);
                                    }
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

        public void OnCommandRecieved(StreamWriter writer, string queryString)
        {
            /*if(queryString.StartsWith("help"))
            {
                //TODO: Implement
            }*/
            if(queryString.StartsWith("showqueue"))
            {
                SendQueueToChat(writer);
            }
            /*else if(queryString.StartsWith("Rem last"))
            {
                //TODO: Implement remove last
            }
            else if(queryString.StartsWith("Rem")) {
                //TODO: Implement remove
            }*/
            else if (queryString.StartsWith("//") || char.IsDigit(queryString, 0))
            {
                if(!char.IsDigit(queryString, 0))
                {
                    queryString = queryString.Replace("//beatsaver.com/browse/detail/", string.Empty);
                }
                RequestSongById(writer, queryString);
            }
            else
            {
                RequestSongByText(writer, queryString);
            }
        }

        public void RequestSongByText(StreamWriter writer, String queryString)
        {
            UnityWebRequest www = UnityWebRequest.Get(String.Format("{0}/api/songs/search/all/{1}", BEATSAVER, queryString));
            www.timeout = 2;
            www.SendWebRequest().completed += (e) =>
            {

                logger.Debug("Webrequest sent.");

                if (www.isNetworkError || www.isHttpError)
                {
                    writer.WriteLine("PRIVMSG #" + config.Channel + " :Error Searching for song.");
                    writer.Flush();
                }
                else
                {
                    logger.Debug("Song request recieved. Parsing.");
                    try
                    {
                        JSONNode node = JSON.Parse(www.downloadHandler.text);
                        string songName = node["songs"][0]["songName"];
                        string beatName = node["songs"][0]["name"];
                        string authorName = node["songs"][0]["authorName"];

                        StaticData.songQueue.Enqueue(new QueuedSong(songName,
                            beatName,
                            authorName,
                            node["songs"][0]["bpm"],
                            node["songs"][0]["key"],
                            node["songs"][0]["downloadUrl"],
                            node["songs"][0]["coverUrl"]));

                        writer.WriteLine("PRIVMSG #" + config.Channel + " :Song Found: " + beatName + " adding to the queue.");
                        writer.Flush();
                    }
                    catch
                    {
                        logger.Error("Parsing Song Data Failed.");
                        writer.WriteLine("PRIVMSG #" + config.Channel + " :Error Searching for song.");
                        writer.Flush();
                    }
                }
            };
        }

        private void RequestSongById(StreamWriter writer, string queryString)
        {
            UnityWebRequest www = UnityWebRequest.Get(String.Format("{0}/api/songs/detail/{1}", BEATSAVER, queryString));
            www.timeout = 2;
            www.SendWebRequest().completed += (e) =>
            {
                logger.Debug("Webrequest sent.");

                if (www.isNetworkError || www.isHttpError)
                {
                    writer.WriteLine("PRIVMSG #" + config.Channel + " :Error Searching for song.");
                    writer.Flush();
                }
                else
                {
                    logger.Debug("Song request recieved. Parsing.");
                    try
                    {
                        JSONNode node = JSON.Parse(www.downloadHandler.text);
                        string songName = node["song"]["songName"];
                        string beatName = node["song"]["name"];
                        string authorName = node["song"]["authorName"];

                        StaticData.songQueue.Enqueue(new QueuedSong(songName,
                            beatName,
                            authorName,
                            node["song"]["bpm"],
                            node["song"]["key"],
                            node["song"]["downloadUrl"],
                            node["song"]["coverUrl"]));

                        writer.WriteLine("PRIVMSG #" + config.Channel + " :Song Found: " + beatName + " adding to the queue.");
                        writer.Flush();
                    }
                    catch
                    {
                        logger.Error("Parsing Song Data Failed.");
                        writer.WriteLine("PRIVMSG #" + config.Channel + " :Error Searching for song.");
                        writer.Flush();
                    }
                }
            };
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

                return new Config(channel, token, username);
            }
            catch (Exception e)
            {
                logger.Error("Error loading xml file: " + e);
                return null;
            }
        }
    }
}
