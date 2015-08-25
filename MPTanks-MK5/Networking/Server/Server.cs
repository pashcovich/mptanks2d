﻿using Microsoft.Xna.Framework;
using MPTanks.Engine;
using MPTanks.Engine.Logging;
using MPTanks.Networking.Common;
using MPTanks.Networking.Common.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPTanks.Networking.Server
{
    public partial class Server
    {
        public ILogger Logger { get; set; }
        public NetworkedGame GameInstance { get; private set; }
        public GameCore Game => GameInstance?.Game;
        public Lidgren.Network.NetServer NetworkServer { get; private set; }
        public LoginManager Login { get; private set; }
        public ConnectionManager Connections { get; set; }
        public ServerNetworkProcessor MessageProcessor { get; private set; }
        public InitializedConfiguration Configuration { get; private set; }
        public Chat.ChatServer ChatHandler { get; private set; }
        public Engine.Core.Timing.Timer.Factory Timers { get; private set; }
        public Extensions.ExtensionManager ExtensionManager { get; private set; }
        internal List<ServerPlayer> _players = new List<ServerPlayer>();
        public IReadOnlyList<ServerPlayer> Players => _players;
        public enum ServerStatus
        {
            NotInitialized,
            Starting,
            Open,
            Closed,
            Errored
        }
        public ServerStatus Status { get; private set; } = ServerStatus.NotInitialized;
        public Server(Configuration configuration, GameCore game, bool openOnInit = true, ILogger logger = null)
        {
            MessageProcessor = new ServerNetworkProcessor(this);
            GameInstance = new NetworkedGame(FullGameState.Create(game), logger, game.Settings);
                        HookEvents();
            SetGame(game);

            Logger = logger ?? new NullLogger();
            Login = new LoginManager(this);
            Connections = new ConnectionManager(this);
            Configuration = new InitializedConfiguration(configuration);
            Timers = new Engine.Core.Timing.Timer.Factory();
            ChatHandler = new Chat.ChatServer(this);
            if (openOnInit) Open();

        }
        public void Open()
        {
            Status = ServerStatus.Starting;
            NetworkServer = new Lidgren.Network.NetServer(new Lidgren.Network.NetPeerConfiguration("MPTANKS")
            {
                AutoFlushSendQueue = false,
                Port = Configuration.Port
            });
            NetworkServer.Start();
            Status = ServerStatus.Open;
        }

        public void Update(GameTime gameTime)
        {
            Game?.Update(gameTime);

            Timers.Update(gameTime);

            MessageProcessor.SendMessage(new Common.Actions.ToClient.FullGameStateSentAction(Game));
            //Send all the wideband messages (if someone is listening)
            if (Connections.ActiveConnections.Count > 0)
            {
                if (MessageProcessor.MessageQueue.Count > 0)
                {
                    var msg = NetworkServer.CreateMessage();
                    MessageProcessor.WriteMessages(msg);
                    NetworkServer.SendMessage(msg, Connections.ActiveConnections,
                        Lidgren.Network.NetDeliveryMethod.ReliableOrdered,
                        Channels.GameplayData);
                }

                //As well as narrowband ones
                foreach (var plr in Players)
                {
                    if (MessageProcessor.HasPrivateMessages(plr) && plr.Connection.Status == Lidgren.Network.NetConnectionStatus.Connected)
                    {
                        var msg = NetworkServer.CreateMessage();
                        MessageProcessor.WritePrivateMessages(plr, msg);
                        plr.Connection.SendMessage(msg,
                            Lidgren.Network.NetDeliveryMethod.ReliableOrdered,
                            Channels.GameplayData);
                    }
                }
            }
            else
            {
                //Just clear the queue since no one is listening
                MessageProcessor.ClearQueue();
            }

            FlushMessages();
        }
        public void Close(string reason = "Server closed")
        {
            NetworkServer.Shutdown(reason);
        }
        public void AddPlayer(ServerPlayer player)
        {
            Game.AddPlayer(player.Player);
            _players.Add(player);

            //Queue the game state for them
            MessageProcessor.SendPrivateMessage(player,
                new Common.Actions.ToClient.GameCreatedAction());
            MessageProcessor.SendPrivateMessage(player,
                new Common.Actions.ToClient.FullGameStateSentAction(Game));

            //Announce that they joined
            ChatHandler.SendMessage(Strings.Server.PlayerJoined(player.Player.Username));
            MessageProcessor.SendMessage(new Common.Actions.ToClient.PlayerJoinedAction(player.Player));

            player.LastSentState = PseudoFullGameWorldState.Create(Game);

            //Create a state sync loop
            Timers.CreateReccuringTimer(t =>
            {
                if (Players.Contains(player))
                {
                    var message = new Common.Actions.ToClient.PartialGameStateUpdateAction(Game, player.LastSentState);
                    player.LastSentState = message.StatePartial;
                    //do state sync
                    MessageProcessor.SendPrivateMessage(
                        player, message);
                }
                else
                {
                    //Disconnect
                    Timers.RemoveTimer(t);
                }

            }, Configuration.StateSyncRate);
        }

        public void RemovePlayer(ServerPlayer player)
        {
            _players.Remove(player);
            Game.RemovePlayer(player.Player.Id);

            MessageProcessor.SendMessage(new Common.Actions.ToClient.PlayerLeftAction(player.Player));
        }

        public ServerPlayer GetPlayer(Guid id) => Players.First(a => a.Player.Id == id);

        public void SetGame(GameCore game)
        {
            GameInstance.FullGameState = FullGameState.Create(game);
            Game.Authoritative = true;
            Game.EventEngine.UnsafeDisableEvents();
            Game.CanRun = false;
            Game.EventEngine.UnsafeEnableEvents();
        }
    }
}
