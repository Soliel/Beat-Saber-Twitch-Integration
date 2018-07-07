using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using MonoWebUtil;
using UnityEngine.Networking;
using UnityEngine;
using SimpleJSON;
using System.Collections;

namespace TwitchIntegrationPlugin
{
    class BeatBot
    {
        //TODO: Index downloaded songs to ease on searching.
        private Config config;
        private Thread botThread;
        private bool retry = false;
        private bool exit = false;


        public BeatBot()
        {
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

            botThread.Join();
        }

        public void Initiate()
        {
            Console.WriteLine("Twitch bot starting...");
            var retryCount = 0;

            do
            {
                try
                {
                    using (var irc = new TcpClient("irc.chat.twitch.tv", 6667))
                    using (var stream = irc.GetStream())
                    using (var reader = new StreamReader(stream))
                    using (var writer = new StreamWriter(stream))
                    {
                        Console.WriteLine("Connection to twitch server established. Beginning Login.");
                        writer.WriteLine("PASS " + config.Token);
                        writer.Flush();
                        writer.WriteLine("NICK " + config.Channel);
                        writer.Flush();
                        writer.WriteLine("JOIN #" + config.Channel);
                        writer.Flush();

                        Console.WriteLine("login Complete. Twitch bot online.");


                        while (!exit)
                        {
                            string inputLine;
                            while ((inputLine = reader.ReadLine()) != null)
                            {
                                string[] splitInput = inputLine.Split(' ');
                                //Console.WriteLine(inputLine);
                                //Console.WriteLine(splitInput.Length);
                                if (splitInput[0] == "PING")
                                {
                                    writer.WriteLine("PONG " + splitInput[1]);
                                    writer.Flush();
                                }

                                splitInput = inputLine.Split(':');
                                if (2 < splitInput.Length)
                                {
                                    if (splitInput[2].StartsWith("!bsr ")) {
                                        var queryString = splitInput[2].Remove(0, 5);
                                        Console.WriteLine("Command Recieved with query: " + queryString);
                                        OnSongRequest(reader, writer, queryString);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    Thread.Sleep(5000);
                    retry = ++retryCount <= 20;
                }
            } while (retry);
        }

        private Config ReadCredsFromConfig()
        {
            string channel;
            string token;

            try
            {
                using (StreamReader sr = new StreamReader("Plugins\\TwitchIntegration.txt"))
                {
                    channel = sr.ReadLine().ToLower();
                    token = sr.ReadLine();
                }

            }
            catch(Exception e)
            {
                Console.WriteLine("File could not be read. Exiting. : " + e);
                
                return null;
            }
            return new Config(channel, token);
        }

        public IEnumerable OnSongRequest(StreamReader reader, StreamWriter writer, String queryString)
        {
            Console.WriteLine("command entered.");

            UnityWebRequest www = UnityWebRequest.Get(String.Format("https://beatsaver.com/search.php?q={0}", queryString));
            www.timeout = 2;
            yield return www.SendWebRequest();

            Console.WriteLine("Webrequest sent");

            if (www.isNetworkError || www.isHttpError)
            {
                writer.WriteLine("PRIVMSG #" + config.Channel + " :Error Searching for song.");
                writer.Flush();
            }
            else
            {
                Console.WriteLine("Search Success! parsing...");
                try
                {
                    string parse = www.downloadHandler.text;

                    JSONNode node = JSON.Parse(parse);
                    string songName = HttpUtility.HtmlDecode(node["hits"]["hits"][0]["_source"]["songName"]);
                    string beatName = HttpUtility.HtmlDecode(node["hits"]["hits"][0]["_source"]["beatname"]);
                    string authorName = HttpUtility.HtmlDecode(node["hits"]["hits"][0]["_source"]["authorName"]);

                    StaticData.songQueue.Enqueue(new QueuedSong(songName,
                        beatName,
                        authorName,
                        node["hits"]["hits"][0]["_source"]["beatsPerMinute"],
                        node["hits"]["hits"][0]["_source"]["id"]));

                    writer.WriteLine("PRIVMSG #" + config.Channel + " :Song Found: " + node["hits"]["hits"][0]["_source"]["beatname"] + " . Adding to the queue.");
                    writer.Flush();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Parsing Song Data Failed: " + e);
                    writer.WriteLine("PRIVMSG #" + config.Channel + " :Error Searching for song.");
                    writer.Flush();
                }
            }
        }
    }
}
