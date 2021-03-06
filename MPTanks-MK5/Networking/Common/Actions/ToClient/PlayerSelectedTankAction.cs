﻿using Lidgren.Network;
using MPTanks.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPTanks.Networking.Common.Actions.ToClient
{
    public class PlayerSelectedTankAction : ActionBase
    {
        public ushort PlayerId { get; private set; }
        public string TankType { get; private set; }
        public PlayerSelectedTankAction()
        {
        }

        public PlayerSelectedTankAction(NetworkPlayer player, string tankType)
        {
            PlayerId = player.Id;
            TankType = tankType;
        }

        protected override void DeserializeInternal(NetIncomingMessage message)
        {
            PlayerId = (ushort)message.ReadUInt32(GameCore.PlayerIdNumberOfBits);
            TankType = message.ReadString();
        }
        public override void Serialize(NetOutgoingMessage message)
        {
            message.Write(PlayerId, GameCore.PlayerIdNumberOfBits);
            message.Write(TankType);
        }
    }
}
