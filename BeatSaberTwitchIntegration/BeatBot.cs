using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using MonoWebUtil;
using UnityEngine.Networking;
using SimpleJSON;
using System.Xml;
using NLog;


namespace TwitchIntegrationPlugin
{
    class BeatBot
    {
        private const String BEATSAVER = "https://beatsaver.co
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
                }
            } while (retry);
        }

        public void OnCommandRecieved(StreamWriter writer, string queryString)
        {
            /*if(queryString.StartsWith("help"))
            {
                //TODO: Implement
            }
            else if(queryString.StartsWith("showqueue"))
            {
                //TODO: Implement
            }
            else if(queryString.StartsWith("Rem last"))
            {
                //TODO: Implement
            }
            else if(queryString.StartsWith("Rem")) {
                //TODO: Implement
            }
            else if(queryString.StartsWith("https"))
            {
                RequestSongById(writer, queryString);
            }
            else
            {*/
                RequestSongByText(writer, queryString);
            //}
        }

        public void RequestSongByText(StreamWriter writer, String queryString)
        {
            UnityWebRequest www = new UnityWebRequest(String.Format("{0}/api/songs/search/all/{1}", BEATSAVER, queryString));
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
                        string parse = HttpUtility.HtmlDecode(www.downloadHandler.text);

                        JSONNode node = JSON.Parse(parse);
                        string songName = node["hits"]["hits"][0]["_source"]["songName"];
                        string beatName = node["hits"]["hits"][0]["_source"]["beatname"];
                        string authorName = node["hits"]["hits"][0]["_source"]["authorName"];

                        StaticData.songQueue.Enqueue(new QueuedSong(songName,
                            beatName,
                            authorName,
                            node["hits"]["hits"][0]["_source"]["beatsPerMinute"],
                            node["hits"]["hits"][0]["_source"]["id"]));

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
            string id = queryString.Split('=')[1];

            UnityWebRequest www = new UnityWebRequest(String.Format("{0}/api/{1}", BEATSAVER, id));
            www.timeout = 2;
            www.SendWebRequest().completed += (e) =>
            {

            };
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
