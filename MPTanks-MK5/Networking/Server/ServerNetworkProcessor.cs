﻿using MPTanks.Networking.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using MPTanks.Networking.Common.Actions;
using MPTanks.Networking.Common.Actions.ToServer;
using MPTanks.Networking.Common.Actions.ToClient;
using Microsoft.Xna.Framework;
using MPTanks.Engine.Logging;

namespace MPTanks.Networking.Server
{
    public class ServerNetworkProcessor : NetworkProcessorBase
    {
        public Server Server { get; private set; }
        public override ILogger Logger => Server.Logger;

        public ServerNetworkProcessor(Server server)
        {
            Server = server;
        }

        public override void ProcessToServerAction(ActionBase action)
        {
            var player = Server.Connections.PlayerTable[action.MessageFrom.SenderConnection];
            if (action is InputChangedAction)
            {
                Server.MessageProcessor.SendMessage(
                    new PlayerInputChangedAction(player.Player,
                    ((InputChangedAction)action).InputState));

                if (player.Player.HasTank)
                {
                    player.Player.Tank.InputState = ((InputChangedAction)action).InputState;
                    //if (Vector2.Distance(((InputChangedAction)action).PlayerPosition +
                    //        player.Player.Tank.LinearVelocity * (player.Connection.AverageRoundtripTime / 2),
                    //    player.Player.Tank.Position) < 2f)
                    //    player.Player.Tank.Position = ((InputChangedAction)action).PlayerPosition +
                    //        player.Player.Tank.LinearVelocity * (player.Connection.AverageRoundtripTime / 2);
                }
            }

            if (action is PlayerTankTypeSelectedAction)
            {
                player.Player.SelectedTankReflectionName =
                    (((PlayerTankTypeSelectedAction)action).SelectedTypeReflectionName);
            }

            if (action is RequestFullGameStateAction)
            {
                Server.MessageProcessor.SendPrivateMessage(player, new FullGameStateSentAction(Server.Game));
            }

            if (action is SentChatMessageAction)
            {
                var act = action as SentChatMessageAction;
                Server.ChatHandler.ForwardMessage(act.Message, player,
                    act.Targets.Select(a => Server.GetPlayer(a)).ToArray());
            }

            if (action is PlayerReadyChangedAction)
            {
                var act = action as PlayerReadyChangedAction;
                player.Player.IsReady = act.IsReady;
            }
        }


        public override void ProcessToServerMessage(MessageBase message)
        {
        }

        public override void OnProcessingError(Exception error)
        {
            Server.Logger.Error("Message processing from client", error);
        }

        private Dictionary<ServerPlayer, List<MessageBase>> _privateQueue =
            new Dictionary<ServerPlayer, List<MessageBase>>();
        public IReadOnlyDictionary<ServerPlayer, List<MessageBase>> PrivateMessageQueues => _privateQueue;


        //Set this flag to send every message in its own network packet. Useful for tracking cascading issues
        //caused by packet concentation
        //#define SEND_MESSAGES_INDIVIDUALLY
        public void SendPrivateMessage(ServerPlayer player, MessageBase message)
        {
            if (!_privateQueue.ContainsKey(player))
                _privateQueue.Add(player, new List<MessageBase>());

            _privateQueue[player].Add(message);
#if SEND_MESSAGES_INDIVIDUALLY
            if (player.Connection == null) return;
            var snp = Server.NetworkServer.CreateMessage();
            WritePrivateMessages(player, snp);
            player.Connection.SendMessage(snp, NetDeliveryMethod.ReliableOrdered, Channels.GameplayData);
            Server.NetworkServer.FlushSendQueue();
#endif
        }

        public void WritePrivateMessages(ServerPlayer player, NetOutgoingMessage message)
        {
            if (!_privateQueue.ContainsKey(player))
                _privateQueue.Add(player, new List<MessageBase>());

            var queue = _privateQueue[player];
            message.Write((ushort)queue.Count);
            foreach (var msg in queue)
                message.Write(TypeIndexTable[msg.GetType()]);
            foreach (var msg in queue)
            {
                RaiseMessageSentOrDiscarded(msg);
                msg.Serialize(message);
            }

            queue.Clear();
        }

        public bool HasPrivateMessages(ServerPlayer player)
        {
            if (!_privateQueue.ContainsKey(player))
                _privateQueue.Add(player, new List<MessageBase>());

            return _privateQueue[player].Count > 0;
        }

#if SEND_MESSAGES_INDIVIDUALLY
        public override void SendMessage(MessageBase message)
        {
            foreach (var plr in Server.Players) SendPrivateMessage(plr, message);
            base.SendMessage(message);
        }
#endif

        public void ClearPrivateQueues()
        {
            _privateQueue.Clear();
        }
    }
}
