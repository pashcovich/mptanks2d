﻿using Microsoft.Xna.Framework.Graphics;
using MPTanks.Rendering.Renderer.Assets.Sprites;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPTanks.Rendering.Renderer.Assets
{
    class AssetLoader
    {
        private GameWorldRenderer _renderer;
        private GraphicsDevice _graphics;
        private AssetResolver _resolver;
        public AssetLoader(GameWorldRenderer renderer, GraphicsDevice gd, AssetResolver resolver)
        {
            _renderer = renderer;
            _graphics = gd;
            _resolver = resolver;
        }

        public void DeferredLoadSpriteSheet(string sheetName, Action<SpriteSheet> callbackComplete, Action errorCallback, Sprite missingTextureSprite = null)
        {
            Task.Run(() =>
            {
                var sheet = LoadSpriteSheet(sheetName, missingTextureSprite);

                if (sheet == null)
                    errorCallback();
                else
                    callbackComplete(sheet);
            });
        }

        public SpriteSheet LoadSpriteSheet(string sheetName, Sprite missingTextureSprite = null)
        {
            try
            {
                //Find the file and fail if we can't
                var resolvedFilename = _resolver.ResolveAssetFile(sheetName);
                if (resolvedFilename == null)
                {
                    _renderer.Logger.Error("SpriteSheet does not exist: " + sheetName);
                    _renderer.Logger.Error("Paths searched: " + String.Join(", ", _resolver.SearchPaths));
                    return null;
                }
                //Check that the matching JSON file exists
                if (!System.IO.File.Exists(resolvedFilename + ".json"))
                {
                    _renderer.Logger.Error("SpriteSheet missing matching JSON file");
                    return null;
                }

                //Then load the texture
                var texture = LoadTexture(resolvedFilename);
                //And metadata
                var metadata = Newtonsoft.Json.JsonConvert.DeserializeObject<JSONSpriteSheet>(
                        System.IO.File.ReadAllText(resolvedFilename + ".json"));

                var sprites = new Dictionary<string, Sprite>();
                var animations = new Dictionary<string, Animation>();
                //Parse the animations
                if (metadata.Animations != null)
                    foreach (var animation in metadata.Animations)
                        animations.Add(animation.Name, new Animation(animation.Name, animation.Frames, animation.FrameRate));
                //And the sprites
                if (metadata.Sprites != null)
                    foreach (var sprite in metadata.Sprites)
                        sprites.Add(sprite.Name, new Sprite(sprite.X, sprite.Y, sprite.Width, sprite.Height, sprite.Name));
                //And build the output sprite sheet
                return new SpriteSheet(animations, sprites, texture, metadata.Name, missingTextureSprite);
            }
            catch (Exception ex)
            {
                _renderer.Logger.Error("SpriteSheet load error: " + sheetName);
                _renderer.Logger.Error(ex.ToString());
            }
            return null;
        }

        private Texture2D LoadTexture(string fn)
        {
            var fs = new FileStream(fn, FileMode.Open, FileAccess.Read);
            var texture = Texture2D.FromStream(_graphics, fs);
            fs.Close();
            fs.Dispose();
            return texture;
        }

        private class JSONSpriteSheet
        {
            public string Name = null;

            public JSONSprite[] Sprites = null;
            public class JSONSprite
            {
                public string Name = null;
                public int X = 0;
                public int Y = 0;
                public int Width = 0;
                public int Height = 0;

            }

            public JSONAnimation[] Animations = null;
            public class JSONAnimation
            {
                public string Name = null;
                public string Friendly = null;
                public float FrameRate = 0;
                public string[] Frames = null;
            }
        }
    }
}