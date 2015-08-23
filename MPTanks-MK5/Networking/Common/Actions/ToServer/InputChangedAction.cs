﻿using Lidgren.Network;
using Microsoft.Xna.Framework;
using MPTanks.Engine.Tanks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPTanks.Networking.Common.Actions.ToServer
{
    /// <summary>
    /// Notifies that the input state for a tank was changed by the client.
    /// 5 bytes including message header
    /// </summary>
    public class InputChangedAction : ActionBase
    {
        public InputState InputState { get; private set; }
        public InputChangedAction(NetIncomingMessage message):base(message)
        {
            var iState = new InputState();
            iState.FirePressed = message.ReadBoolean();
            iState.LookDirection = message.ReadRangedSingle(-MathHelper.TwoPi, MathHelper.TwoPi, 13);
            iState.WeaponNumber = message.ReadByte(2);
            iState.MovementSpeed = ((float)message.ReadByte() / 128) - 1;
            iState.RotationSpeed = ((float)message.ReadByte() / 128) - 1;
            InputState = iState;
        }

        public InputChangedAction(InputState state)
        {
            InputState = state;
        }

        public override void Serialize(NetOutgoingMessage message)
        {
            message.Write(InputState.FirePressed);
            message.WriteRangedSingle(InputState.LookDirection, -MathHelper.TwoPi, MathHelper.TwoPi, 13);
            message.Write((byte)InputState.WeaponNumber, 2);
            message.Write((byte)((InputState.MovementSpeed + 1) * 128)); //compress to byte range
            message.Write((byte)((InputState.RotationSpeed + 1) * 128)); //compress to byte range
        }
    }
}
