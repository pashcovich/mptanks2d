﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Maps.MapObjects
{
    public class Building : MapObject
    {
        public Building(GameCore game, bool authorized = false, Vector2 position = default(Vector2), float rotation = 0)
            : base(game, authorized, position, rotation)
        {
            Size = new Vector2(8);
        }

        protected override void AddComponents()
        {
            Components.Add("building", new Rendering.RenderableComponent()
            {
                Mask = new Color(Color.Blue, 255),
                Size = new Vector2(8, 8)
            });
            Components.Add("building_p2", new Rendering.RenderableComponent()
            {
                Mask = new Color(Color.DarkBlue, 255),
                Offset = new Vector2(1, 0),
                Size = new Vector2(6, 8)
            });
            Components.Add("building_p3", new Rendering.RenderableComponent()
            {
                Mask = new Color(Color.BlueViolet, 255),
                Offset = new Vector2(2, 0),
                Size = new Vector2(4, 8)
            });
            Components.Add("building_p4", new Rendering.RenderableComponent()
            {
                Mask = new Color(Color.Blue, 255),
                Offset = new Vector2(3, 0),
                Size = new Vector2(2, 8)
            });
            Components.Add("chimney", new Rendering.RenderableComponent()
            {
                Mask = new Color(Color.Green, 255),
                Offset = new Vector2(1, 2),
                Size = new Vector2(2, 1),
                Rotation = 0.24f
            });
        }

        public override void Update(GameTime time)
        {
        }
    }
}
