﻿using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPTanks.Networking.Common.Actions.ToClient
{
    public class PlayerTankSelectionAcknowledgedAction : ActionBase
    {
        public bool WasAccepted { get; private set; }
        public PlayerTankSelectionAcknowledgedAction(NetIncomingMessage message ) : base(message)
        {
            WasAccepted = message.ReadBoolean();
        }
        public PlayerTankSelectionAcknowledgedAction(bool wasAccepted)
        {
            WasAccepted = wasAccepted;
        }
        public override void Serialize(NetOutgoingMessage message)
        {
            message.Write(WasAccepted);
        }
    }
}
