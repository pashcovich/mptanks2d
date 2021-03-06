﻿using MPTanks.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPTanks.Networking.Server
{
    public partial class Server
    {
        public void HookEvents()
        {
            GameInstance.GameChanged += (s, e) =>
             {
                 if (e.OldGame != null)
                 {
                     e.OldGame.EventEngine.OnGameEnded -= Game_Ended;
                     e.OldGame.EventEngine.OnGameStarted -= Game_Started;
                     e.OldGame.EventEngine.OnGameTimescaleChanged -= Game_TimescaleChanged;

                     e.OldGame.EventEngine.OnGameObjectDestroyed -= GameObject_Destroyed;
                     e.OldGame.EventEngine.OnGameObjectDestructionEnded -= GameObject_DestructionEnded;
                     e.OldGame.EventEngine.OnGameObjectStateChanged -= GameObject_StateChanged;
                     e.OldGame.EventEngine.OnGameObjectBasicPropertyChanged -= GameObject_BasicPropertyChanged;
                     e.OldGame.EventEngine.OnGameObjectCreated -= GameObject_Created;

                     e.OldGame.EventEngine.OnGamemodeStateChanged -= Gamemode_StateChanged;

                     UnhookPlayers(e.OldGame);
                 }
                 e.Game.EventEngine.OnGameEnded += Game_Ended;
                 e.Game.EventEngine.OnGameStarted += Game_Started;
                 e.Game.EventEngine.OnGameTimescaleChanged += Game_TimescaleChanged;

                 e.Game.EventEngine.OnGameObjectDestroyed += GameObject_Destroyed;
                 e.Game.EventEngine.OnGameObjectDestructionEnded += GameObject_DestructionEnded;
                 e.Game.EventEngine.OnGameObjectStateChanged += GameObject_StateChanged;
                 e.Game.EventEngine.OnGameObjectBasicPropertyChanged += GameObject_BasicPropertyChanged;
                 e.Game.EventEngine.OnGameObjectCreated += GameObject_Created;

                 e.Game.EventEngine.OnGamemodeStateChanged += Gamemode_StateChanged;

                 HookPlayers(e.Game);

                 //And send out the game state
                 MessageProcessor.SendMessage(MessagePool.Retrieve<Common.Actions.ToClient.GameCreatedAction>());
                 MessageProcessor.SendMessage(new Common.Actions.ToClient.FullGameStateSentAction(Game));
             };
        }

        private void Gamemode_StateChanged(object sender, byte[] e)
        {
            var msg = MessagePool.Retrieve<Common.Actions.ToClient.GamemodeStateChangedAction>();
            msg.NewState = e;
            MessageProcessor.SendMessage(msg);
        }

        private void GameObject_Created(object sender, GameObject e)
        {
            MessageProcessor.SendMessage(new Common.Actions.ToClient.GameObjectCreatedAction(e));
        }

        private void GameObject_BasicPropertyChanged(object sender, GameObject.BasicPropertyChangeArgs e)
        {
            var msg = MessagePool.Retrieve<Common.Actions.ToClient.ObjectBasicPropertyChangedAction>();
            msg.Set(e.Type, e);
            MessageProcessor.SendMessage(msg);
        }

        private void GameObject_StateChanged(object sender, Engine.Core.Events.Types.GameObjects.StateChangedEventArgs e)
        {
            var msg = MessagePool.Retrieve<Common.Actions.ToClient.ObjectStateChangedAction>();
            msg.PrivateData = e.State;
            msg.ObjectId = e.Object.ObjectId;
            MessageProcessor.SendMessage(msg);
        }

        private void Game_TimescaleChanged(object sender, Engine.GameCore.TimescaleValue e)
        {
            MessageProcessor.SendMessage(new Common.Actions.ToClient.TimescaleChangedAction(Game));
            MessageProcessor.SendMessage(new Common.Actions.ToClient.PartialGameStateUpdateAction(Game));
        }

        private void Game_Started(object sender, EventArgs e)
        {
            MessageProcessor.SendMessage(new Common.Actions.ToClient.GameStartedAction());
            //And send the full state so they can update it
            MessageProcessor.SendMessage(new Common.Actions.ToClient.FullGameStateSentAction(Game));
        }

        private void GameObject_Destroyed(object sender, Engine.Core.Events.Types.GameObjects.DestroyedEventArgs e)
        {
            var msg = MessagePool.Retrieve<Common.Actions.ToClient.GameObjectDestroyedAction>();
            msg.ObjectId = e.Destroyed.ObjectId;
            MessageProcessor.SendMessage(msg);
        }

        private void GameObject_DestructionEnded(object sender, GameObject e)
        {
            var msg = MessagePool.Retrieve<Common.Actions.ToClient.GameObjectDestructionEndedAction>();
            msg.ObjectId = e.ObjectId;
            MessageProcessor.SendMessage(new Common.Actions.ToClient.GameObjectDestructionEndedAction(e));
        }

        private void Game_Ended(object sender, Engine.Core.Events.Types.GameCore.EndedEventArgs e)
        {
            MessageProcessor.SendMessage(new Common.Actions.ToClient.GameEndedAction(Game.Gamemode.WinningTeam));
        }
    }
}
