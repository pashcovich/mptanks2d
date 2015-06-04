﻿using MPTanks.Engine.Core.Timing;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPTanks.Engine.Tanks;
using MPTanks.Engine;
using MPTanks.Engine.Gamemodes;

namespace MPTanks.Modding.Mods.Core.Tanks
{
    [Tank("BasicTankMP", "assets/components/basictank.json", 
        DisplayName = "Basic Tank", Description = "Basic Tank",
        RequiresMatchingOnOtherTeam = false)]
    public class BasicTank : Tank
    {
        protected override float RotationSpeed
        {
            get { return 0.05f; }
        }

        protected override float MovementSpeed
        {
            get { return 50; }
        }
        public BasicTank(GamePlayer player, Team team, GameCore game, bool authorized = false)
            : base(player, team, game, authorized)
        {
            Health = 150;
            Size = new Vector2(3, 5);
        }

        protected override void AddComponents()
        {
            Components.Add("base", new Engine.Rendering.RenderableComponent()
            {
                Size = new Vector2(3, 5),
                Mask = new Color(Color.White, 200)
            });
            Components.Add("tankFront", new Engine.Rendering.RenderableComponent()
            {
                Size = new Vector2(3f, 0.25f),
                Mask = new Color(Color.White, 200),
                Offset = new Vector2(0, 0),
                AssetName = Assets.BasicTank.GrillMask.SpriteName,
                SpriteSheetName = Assets.BasicTank.GrillMask.SheetName,
                Rotation = 0
            });
            Components.Add("turret", new Engine.Rendering.RenderableComponent()
            {
                Size = new Vector2(0.5f, 2.35f),
                Mask = new Color(255, 150, 150, 150),
                Offset = new Vector2(1.25f, -1f),
                RotationOrigin = new Vector2(0.25f, 3.5f),
                Rotation = 0
            });
            Components.Add("turretBase", new Engine.Rendering.RenderableComponent()
            {
                Size = new Vector2(2, 2.5f),
                Mask = new Color(Color.White, 200),
                Offset = new Vector2(0.5f, 1.5f),
                RotationOrigin = new Vector2(1f, 1f),
                AssetName = Assets.BasicTank.TurretBase.SpriteName,
                SpriteSheetName = Assets.BasicTank.TurretBase.SheetName,
                Rotation = 0
            });
            Components.Add("turretDoor", new Engine.Rendering.RenderableComponent()
            {
                Size = new Vector2(0.65f, 0.65f),
                Mask = new Color(Color.Gray, 100),
                Offset = new Vector2(0.75f, 1.75f),
                RotationOrigin = new Vector2(0.75f, 0.75f),
                Rotation = 0
            });
        }

        private bool canFirePrimary = true;
        public override void Update(GameTime time)
        {
            //handle turret rotation
            Components["turret"].Rotation = InputState.LookDirection - Rotation;
            Components["turretBase"].Rotation = InputState.LookDirection - Rotation;
            Components["turretDoor"].Rotation = InputState.LookDirection - Rotation;


            if (InputState.FirePressed && InputState.WeaponNumber == 0)
                FirePrimary();
            if (InputState.FirePressed && InputState.WeaponNumber == 1)
                FireSecondary();

            base.Update(time);
        }

        private void FirePrimary()
        {
            if (!canFirePrimary)
                return;
            if (Game.Authoritative) // If we are able to be create game objects AKA we're authoritative, make the projectile
            {
                var rotation = InputState.LookDirection;
                //Spawn a projectile
                var projectile = MPTanks.Engine.Projectiles.Projectile
                    .ReflectiveInitialize<Projectiles.BasicTank.MainGunProjectile>(
                    Projectiles.BasicTank.MainGunProjectile.ReflectionTypeName, this, Game,
                    false, TransformPoint(new Vector2(1.5f, -1.1f), rotation, true), rotation);

                //Fire it in the barrel direction
                const float velocity = 60f;
                var x = velocity * (float)Math.Sin(rotation);
                var y = velocity * -(float)Math.Cos(rotation);
                projectile.LinearVelocity = new Vector2(x, y);
                //Add to the game world
                Game.AddGameObject(projectile, this);
            }

            //Reload timer
            canFirePrimary = false;
            Game.TimerFactory.CreateTimer((timer) => canFirePrimary = true, 500);
        }
        private void FireSecondary()
        {
            if (!canFirePrimary)
                return;

            if (Game.Authoritative) //Once again, check that we've got the power
            {
                var rotation = InputState.LookDirection;
                //Spawn a projectile
                var projectile = new Projectiles.BasicTank.MainGunProjectile(
                    this, Game, false,
                    TransformPoint(new Vector2(1.5f, -1f), rotation, true), rotation);
                //Fire it in the barrel direction
                const float velocity = 7f;
                var x = velocity * (float)Math.Sin(rotation);
                var y = velocity * -(float)Math.Cos(rotation);
                projectile.LinearVelocity = new Vector2((float)x, (float)y);
                //Add to the game world
                Game.AddGameObject(projectile, this);
            }
            //Reload timer
            canFirePrimary = false;
            Game.TimerFactory.CreateTimer((timer) => canFirePrimary = true, 500);
        }

        protected override bool DestroyInternal(GameObject destructor = null)
        {
            var si = MPTanks.Engine.Assets.AssetHelper.GetRandomExplosionAnimation();
            var anim = new MPTanks.Engine.Rendering.Animations.Animation(
                     si.AnimationName, Position, new Vector2(10), si.SheetName);

            Game.AnimationEngine.AddAnimation(anim);

            Game.TimerFactory.CreateReccuringTimer((Action<Timer>)((timer) =>
            {
                if (timer.ElapsedMilliseconds > 500)
                    Game.TimerFactory.RemoveTimer(timer);

                anim.Position = Position;
            }), 1);

            if (Game.Authoritative)
                Game.TimerFactory.CreateTimer((timer) => IsDestructionCompleted = true, 500);

            return true;
        }
    }
}
