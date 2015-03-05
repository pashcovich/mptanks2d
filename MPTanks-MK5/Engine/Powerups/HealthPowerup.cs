﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Powerups
{
    public class HealthPowerup : Powerup
    {
        public HealthPowerup(GameCore game, Vector2 position)
            :base(game, position)
        {

        }
        public override bool SpawnRandomly
        {
            get { throw new NotImplementedException(); }
        }

        public override void CollidedWithTank(Tanks.Tank tank)
        {
            throw new NotImplementedException();
        }

        public override Microsoft.Xna.Framework.Vector2 Size
        {
            get { throw new NotImplementedException(); }
        }

        public override void Update(Microsoft.Xna.Framework.GameTime time)
        {
            throw new NotImplementedException();
        }
    }
}