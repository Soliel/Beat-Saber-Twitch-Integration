using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AsyncTwitch;
using NLog;
using UnityEngine;
using Logger = NLog.Logger;
using TwitchIntegrationPlugin.Commands;

namespace TwitchIntegrationPlugin
{
    public class BeatBotNew
    {
        private Logger _logger;
        private Dictionary<string, IrcCommand> _commandDict = new Dictionary<string, IrcCommand>();

        public BeatBotNew()
        {
            //_logger = LogManager.GetCurrentClassLogger();

            Console.WriteLine("Loading files.");
            StaticData.Config = StaticData.Config.LoadFromJson();
            StaticData.SongQueue.LoadSongQueue();
            StaticData.BanList.LoadBanList();
            Console.WriteLine("Getting Commands.");
            LoadCommandClasses();

            TwitchConnection.Instance.StartConnection();
            TwitchConnection.Instance.RegisterOnMessageReceived(OnMessageReceived);
        }

        private void OnMessageReceived(TwitchConnection connection, TwitchMessage msg)
        {
            if (!msg.Content.StartsWith("!")) return;
            string commandString = msg.Content.Split(' ')[0];
            commandString = commandString.Remove(0, 1);

            if (_commandDict.ContainsKey(commandString))
            {
                _commandDict[commandString].Run(msg);
            }

        }

        private void LoadCommandClasses()
        {
            Type[] assemblyTypes = Assembly.GetExecutingAssembly().GetTypes();
            Type[] commandList = ( from assemblyType in assemblyTypes
                where assemblyType.IsSubclassOf(typeof(IrcCommand))
                select assemblyType).ToArray();

            foreach (Type abstractCommand in commandList)
            {
                IrcCommand command = (IrcCommand)Activator.CreateInstance(abstractCommand);
                foreach (string alias in command.CommandAlias)
                {
                    _commandDict.Add(alias, command);
                }
            }
        }
    }
}
