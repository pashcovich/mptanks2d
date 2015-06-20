﻿using Lidgren.Network;
using MPTanks.Engine;
using MPTanks.Engine.Gamemodes;
using MPTanks.Engine.Logging;
using MPTanks.Engine.Settings;
using MPTanks.Engine.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPTanks.Networking.Common.Game
{
    public class FullGameState
    {
        public List<FullObjectState> ObjectStates { get; set; } = new List<FullObjectState>();
        /// <summary>
        /// The raw map data for the current map
        /// </summary>
        public string MapData { get; set; }
        public string GamemodeReflectionName { get; set; }
        public float CurrentGameTimeMilliseconds { get; set; }
        public byte[] GamemodeState { get; set; }
        public List<FullStatePlayer> Players { get; set; } = new List<FullStatePlayer>();

        public GameCore CreateGameFromState(ILogger logger = null, EngineSettings settings = null, float latency = 0)
        {
            var game = new GameCore(logger ?? new NullLogger(), GamemodeReflectionName, MapData, true, settings);
            game.Gamemode.FullState = GamemodeState;

            //Add all of the game objects
            foreach (var fullState in ObjectStates)
                GameObject.CreateAndAddFromSerializationInformation(game, fullState.Data, true);

            //Do this with reflection because we want to keep the api private (set gaame time)
            typeof(GameCore).GetProperty(nameof(GameCore.TimeMilliseconds),
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.SetProperty).SetValue(game, CurrentGameTimeMilliseconds);

            foreach (var player in Players)
            {
                var nwPlayer = new NetworkPlayer();
                nwPlayer.AllowedTankTypes = player.AllowedTankTypes;
                nwPlayer.ClanName = player.ClanName;
                nwPlayer.DisplayName = player.Username;
                nwPlayer.DisplayNameDrawColor = player.UsernameDisplayColor;
                nwPlayer.Game = game;
                nwPlayer.HasCustomTankStyle = player.TankHasCustomStyle;
                nwPlayer.HasSelectedTankYet = player.HasSelectedTank;
                nwPlayer.Id = player.Id;
                nwPlayer.IsAdmin = player.IsAdmin;
                nwPlayer.IsPremium = player.IsPremium;
                nwPlayer.IsSpectator = player.IsSpectator;
                nwPlayer.SelectedTankReflectionName = player.TankReflectionName;
                nwPlayer.SpawnPoint = player.SpawnPoint;
                nwPlayer.Tank = (Engine.Tanks.Tank)(player.HasTank ? game.GameObjectsById[player.TankObjectId] : null);
                nwPlayer.Team = (player.TeamId != -3 ? FindTeam(game.Gamemode.Teams, player.TeamId) : null);

                game.AddPlayer(nwPlayer);
            }

            if (latency > 0)
                game.UnsafeTickGameWorld(latency);

            return game;
        }

        private Team FindTeam(Team[] teams, int id)
        {
            foreach (var t in teams)
                if (t.TeamId == id) return t;

            return Team.Null;
        }

        public static FullGameState Create(GameCore game)
        {
            var state = new FullGameState();

            state.SetPlayers(game.Players.Select(x => (NetworkPlayer)x).AsEnumerable());
            state.SetObjects(game);

            state.MapData = game.Map.ToString();
            state.CurrentGameTimeMilliseconds = game.TimeMilliseconds;
            state.GamemodeReflectionName = game.Gamemode.ReflectionName;
            state.GamemodeState = game.Gamemode.FullState;

            return state;
        }

        private void SetPlayers(IEnumerable<NetworkPlayer> players)
        {
            foreach (var plr in players)
            {
                var serialized = new FullStatePlayer();
                serialized.AllowedTankTypes = plr.AllowedTankTypes;
                serialized.ClanName = plr.ClanName;
                serialized.HasSelectedTank = plr.HasSelectedTankYet;
                serialized.HasTank = plr.Tank != null;
                serialized.Id = plr.Id;
                serialized.IsAdmin = plr.IsAdmin;
                serialized.IsPremium = plr.IsPremium;
                serialized.IsSpectator = plr.IsSpectator;
                serialized.SpawnPoint = plr.SpawnPoint;
                serialized.TankHasCustomStyle = plr.HasCustomTankStyle;
                serialized.TankObjectId = (plr.Tank != null) ? plr.Tank.ObjectId : (ushort)0;
                serialized.TankReflectionName = (plr.Tank != null) ? plr.Tank.ReflectionName : "";
                serialized.TeamId = (plr.Team != null) ? plr.Team.TeamId : (short)-3;
                serialized.Username = plr.DisplayName;
                serialized.UsernameDisplayColor = plr.DisplayNameDrawColor;

                Players.Add(serialized);
            }
        }

        private void SetObjects(GameCore game)
        {
            foreach (var obj in game.GameObjects)
                ObjectStates.Add(new FullObjectState(obj.FullState));
        }


        public static FullGameState Read(NetIncomingMessage message)
        {
            var state = new FullGameState();
            state.MapData = message.ReadString();
            state.GamemodeReflectionName = message.ReadString();
            state.CurrentGameTimeMilliseconds = message.ReadFloat();
            state.GamemodeState = message.ReadBytes(message.ReadInt32());

            var objCount = message.ReadInt32();
            for (var i = 0; i < objCount; i++)
            {
                state.ObjectStates.Add(new FullObjectState(message.ReadBytes(message.ReadInt32())));
            }

            var playersCount = message.ReadInt32();

            for (var i = 0; i < playersCount; i++)
            {
                var fsPlayer = new FullStatePlayer();
                fsPlayer.ClanName = message.ReadString();
                fsPlayer.Id = new Guid(message.ReadBytes(16));

                fsPlayer.HasSelectedTank = message.ReadBoolean();
                fsPlayer.HasTank = message.ReadBoolean();
                fsPlayer.IsAdmin = message.ReadBoolean();
                fsPlayer.IsPremium = message.ReadBoolean();
                fsPlayer.IsSpectator = message.ReadBoolean();
                fsPlayer.TankHasCustomStyle = message.ReadBoolean();
                message.ReadPadBits();

                fsPlayer.SpawnPoint = new Microsoft.Xna.Framework.Vector2(
                    message.ReadFloat(), message.ReadFloat());

                fsPlayer.TankObjectId = message.ReadUInt16();
                fsPlayer.TankReflectionName = message.ReadString();

                fsPlayer.TeamId = message.ReadInt16();
                fsPlayer.Username = message.ReadString();
                fsPlayer.UsernameDisplayColor = new Microsoft.Xna.Framework.Color
                { PackedValue = message.ReadUInt32() };
            }

            return state;
        }

        public void Write(NetOutgoingMessage message)
        {
            message.Write(MapData);
            message.Write(GamemodeReflectionName);
            message.Write(CurrentGameTimeMilliseconds);
            message.Write(GamemodeState.Length);
            message.Write(GamemodeState);

            message.Write(ObjectStates.Count);
            foreach (var obj in ObjectStates)
            {
                message.Write(obj.Data.Length);
                message.Write(obj.Data);
            }

            message.Write(Players.Count);
            foreach (var player in Players)
            {
                message.Write(player.ClanName);
                message.Write(player.Id.ToByteArray());

                message.Write(player.HasSelectedTank);
                message.Write(player.HasTank);
                message.Write(player.IsAdmin);
                message.Write(player.IsPremium);
                message.Write(player.IsSpectator);
                message.Write(player.TankHasCustomStyle);
                message.WritePadBits();

                message.Write(player.SpawnPoint.X);
                message.Write(player.SpawnPoint.Y);

                message.Write(player.TankObjectId);
                message.Write(player.TankReflectionName);
                message.Write(player.TeamId);
                message.Write(player.Username);
                message.Write(player.UsernameDisplayColor.PackedValue);
            }
        }
    }
}
