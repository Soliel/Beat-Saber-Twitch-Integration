using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AsyncTwitch;
using TwitchIntegrationPlugin.Commands;

namespace TwitchIntegrationPlugin
{
    public class BeatBotNew
    {
        private const string Prefix = "!";
        private Dictionary<string, IrcCommand> _commandDict = new Dictionary<string, IrcCommand>();

        public BeatBotNew()
        {
            StaticData.Config = StaticData.Config.LoadFromJson();
            StaticData.SongQueue.LoadSongQueue();
            StaticData.BanList.LoadBanList();
            LoadCommandClasses();

            TwitchConnection.Instance.StartConnection();
            TwitchConnection.Instance.RegisterOnMessageReceived(OnMessageReceived);
        }

        private void OnMessageReceived(TwitchConnection connection, TwitchMessage msg)
        {
            if (!msg.Content.StartsWith(Prefix)) return;
            string commandString = msg.Content.Split(' ')[0];
            commandString = commandString.Remove(0, Prefix.Length);

            if (_commandDict.ContainsKey(commandString))
            {
                _commandDict[commandString].Run(msg);
            }
        }

        private void LoadCommandClasses()
        {
            Type[] assemblyTypes = Assembly.GetExecutingAssembly().GetTypes();
            IEnumerable<Type> commandList = assemblyTypes.Where(x => x.IsSubclassOf(typeof(IrcCommand)));

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
