﻿using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Generated;
using EmptyKeys.UserInterface.Input;
using EmptyKeys.UserInterface.Media;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MPTanks.Clients.GameClient.Menus.InGame;
using MPTanks.Rendering.UI;
using System;

namespace MPTanks.Clients.GameClient.Menus
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class ClientCore : Game
    {
        GraphicsDeviceManager graphics;

        private int nativeScreenWidth;
        private int nativeScreenHeight;


        private bool sizeDirty = true;
        UserInterface ui;

        public ClientCore()
            : base()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "mgcontent";
            graphics.PreparingDeviceSettings += graphics_PreparingDeviceSettings;
            graphics.DeviceCreated += graphics_DeviceCreated;
            Window.ClientSizeChanged += Window_ClientSizeChanged;
            Window.AllowUserResizing = true;

        }

        void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            sizeDirty = true;
        }

        void graphics_DeviceCreated(object sender, EventArgs e)
        {
        }

        private void graphics_PreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            nativeScreenWidth = graphics.PreferredBackBufferWidth;
            nativeScreenHeight = graphics.PreferredBackBufferHeight;

            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            graphics.PreferMultiSampling = true;
            graphics.PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        private bool _bl;
        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            this.IsMouseVisible = true;

            ui = new UserInterface(Content, GraphicsDevice);
            ui.SetPage("mainmenu");
            ui.ActiveBinder.ExitAction = (Action)Exit;
            ui.ActiveBinder.HostAction = (Action)(() =>
            {
                var game = new LiveGame(new Networking.Common.Connection.ConnectionInfo(), new string[] { });
            });
        }

        int ct;
        private void v()
        {
            if (ct++ < 10)
            {
                ui.ShowMessageBox("Message" + ct, "ssssssssssssssssssssssssssssssssssssssssssssssss",
                   (UserInterface.MessageBoxType)(ct % 3), (UserInterface.MessageBoxButtons)(ct % 6), (r) =>
                   {
                       v();
                   });
            }
        }

        private void ExitEvent(object parameter)
        {
            Exit();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        private float opacity = 1;
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (sizeDirty)
            {
                graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
                graphics.ApplyChanges();
                sizeDirty = false;
            }

            if (_bl)
            {
                if (opacity <= 0)
                    opacity = 0;
                else
                    opacity -= 0.05f;

            }

            ui.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(15, 15, 15, 255));

            ui.Draw(gameTime);

            base.Draw(gameTime);
        }
    }
}
