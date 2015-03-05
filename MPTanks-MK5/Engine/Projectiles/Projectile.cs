﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Projectiles
{
    public abstract class Projectile : GameObject
    {
        public abstract int DamageAmount { get; }

        public Tanks.Tank Owner { get; private set; }

        public Projectile(Tanks.Tank owner, GameCore game, float density = 1,
            float bounciness = 0.1f, Vector2 position = default(Vector2), float rotation = 0)
            : base(game, density, bounciness, position, rotation)
        {
            Owner = owner;
            Body.IsBullet = true;
        }

        abstract public void CollidedWithTank(Tanks.Tank tank);
    }
}