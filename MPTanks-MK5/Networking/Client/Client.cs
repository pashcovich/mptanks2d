﻿using Microsoft.Xna.Framework;
using MPTanks.Engine;
using MPTanks.Networking.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPTanks.Networking.Client
{
    public class Client
    {
        public Lidgren.Network.NetClient NetworkClient { get; private set; }
        public bool Connected { get; private set; }
        public NetworkedGame GameInstance { get; private set; }
        public Guid PlayerId { get; set; }
        public ClientNetworkProcessor MessageProcessor { get; private set; }
        public Chat.ChatClient Chat { get; private set; }
        public GameCore Game => GameInstance.Game;
        public GamePlayer Player
        {
            get
            {
                return
                    GameInstance.Game.PlayersById.ContainsKey(PlayerId) ?
                    GameInstance.Game.PlayersById[PlayerId] : null;
            }
        }
        public bool NeedsToSelectTank { get; internal set; }
        public bool IsInCountdown { get; internal set; }
        public TimeSpan RemainingCountdownTime { get; internal set; }

        public bool GameRunning { get { return Connected && GameInstance != null; } }
        public Client(string connection, ushort port, string password = null, bool connectOnInit = true)
        {
            //connect to server
            if (connectOnInit)
                Connect();

            MessageProcessor = new ClientNetworkProcessor(this);
            GameInstance = new NetworkedGame(null);
            Chat = new Networking.Client.Chat.ChatClient(this);
        }

        private void TickCountdown(GameTime gameTime)
        {
            if (!IsInCountdown) return;

            RemainingCountdownTime -= gameTime.ElapsedGameTime;
            if (RemainingCountdownTime < TimeSpan.Zero)
                IsInCountdown = false;
        }

        public void Connect()
        {

        }

        private void DeferredConnect()
        {

        }
        /// <summary>
        /// Waits until it connects to the server and downloads the game state or returns false if the connection
        /// timed out;
        /// </summary>
        /// <returns></returns>
        public bool WaitForConnection()
        {
            return false;
        }

        public void Disconnect()
        {

        }
    }
}
