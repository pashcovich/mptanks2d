﻿using MPTanks.Engine.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPTanks.Clients.GameClient
{
    [Serializable]
    public class GameSettings : SettingsBase
    {
        public static GameSettings Instance { get; private set; }

        static GameSettings()
        {
            Instance = new GameSettings();
            Instance.LoadFromFile(Path.Combine(ConfigDir, "Game Settings.json"));
        }

        public override void OnSettingChanged(Setting setting)
        {
            Save(Path.Combine(ConfigDir, "Game Settings.json"));
        }

        public Setting<string> GameLogLocation { get; private set; }

        /// <summary>
        /// The maximum number of on screen particles to allow
        /// </summary>
        public Setting<int> MaxParticlesToRender { get; private set; }

        /// <summary>
        /// Whether to force a gen 0 GC every frame. This gets rid of
        /// most temporary objects, helping to trace actual memory leaks.
        /// Unfortunately, it does so to the possible detriment of 
        /// framerate.
        /// </summary>
        public Setting<bool> ForceFullGCEveryFrame { get; private set; }
        public Setting<bool> ForceGen0GCEveryFrame { get; private set; }

        /// <summary>
        /// The number of instances of a sound that can be playing at the same time.
        /// </summary>
        public Setting<int> MaxInstancesOfOneSoundAllowed { get; private set; }

        public Setting<string[]> ImageAllowedFileExtensions { get; private set; }

        //Where to look for assets
        public Setting<string[]> ImageSearchPaths { get; private set; }

        public Setting<string[]> SoundAllowedFileExtensions { get; private set; }

        //Where to look for assets
        public Setting<string[]> SoundSearchPaths { get; private set; }

        //Stores mods in a runtime directory. That way, when we download mods from servers, we 
        //just leave them in the temp directory where they are removed next time the program opens
        public Setting<string> ModUnpackPath { get; private set; }
        public Setting<string> ModImagePath { get; private set; }
        public Setting<string> ModSoundPath { get; private set; }
        public Setting<string> ModDownloadPath { get; private set; }

        /// <summary>
        /// A list of mod files (relative or absolute) that should be loaded without 
        /// Code security verification. AKA trusted mods.
        /// </summary>
        public Setting<string[]> CoreMods { get; private set; }

        public static readonly string[] DefaultTrustedMods = new[] {
                    "MPTanks.Modding.Mods.Core.dll"
                };

        /// <summary>
        /// The scale the rendering runs at relative to the blocks.
        /// This way, we can pass integers around safely.
        /// </summary>
        public Setting<float> RenderScale { get; private set; }

        /// <summary>
        /// The amount of "blocks" to compensate for in rendering because of the 
        /// skin on physics objects
        /// </summary>
        public Setting<float> PhysicsCompensationForRendering { get; private set; }

        #region Screen Resolution
        public Setting<bool> Fullscreen { get; private set; }
        #endregion

        public GameSettings()
        {
            GameLogLocation = new Setting<string>(this, "Log storage location",
               "Where to store runtime logs for the game. This uses NLog storage conventions." +
               " So, ${basedir} is the program's installation directory.",
               Path.Combine(ConfigDir, "gamelogs", "game.log"));

            MaxParticlesToRender = new Setting<int>(this, "Max particles allowed on screen",
            "The maximum number of particles that can be displayed on screen at any particular time. Higher values" +
            " can increase visual fidelity (some particles may not be rendered at lower settings) while lower ones" +
            " substantially increase performance. See the related Particle Limit settings.",
            5000);

            ForceFullGCEveryFrame = new Setting<bool>(this, "Force Full GC every frame",
            "Whether to force a full GC every frame. Useful for detecting memory leaks, terrible for performance.", false);

            ForceGen0GCEveryFrame = new Setting<bool>(this, "Force Gen 0 GC every frame",
            "Whether to force a fast GC every frame. This is rarely a significant performance problem so" +
            " it's useful for debugging purposes. Recommended to be off but it's ok to have it on.", false);

            MaxInstancesOfOneSoundAllowed = new Setting<int>(this, "Max instances of 1 sound",
            "The maximum number of instaces of a single sound that can be playing simultaneously." +
            " If more sounds than that try to play simultaneously, the oldest one will be cut off. Increase" +
            " this if you are hearing audible cutoffs, at the cost of memory usage and performace.", 4);

            ImageAllowedFileExtensions = new Setting<string[]>(this, "Image asset file extensions",
                "The extensions to search for when trying to load an image, in the correct search order.",
                new[] { ".dds", ".png", ".jpg", ".jpeg", ".bmp", ".gif" });

            SoundAllowedFileExtensions = new Setting<string[]>(this, "Sound asset file extensions",
                "The extensions to search for when trying to load a sound, in the correct search order.",
                new[] { ".ogg", ".wav", ".mp3" });

            ModUnpackPath = new Setting<string>(this, "Mod temp directory",
                "The place to store mods that are used at runtime. In other words, this is the directory" +
                " that *.mod files are unpacked into.",
                Path.Combine(ConfigDir, "tempmodunpack"));

            ModSoundPath = new Setting<string>(this, "Mod temp directory for sounds",
                "The place to store mod sound assets that are used at runtime. In other words, this is the directory" +
                " that *.mod files are unpacked into.",
                Path.Combine(ConfigDir, "tempmodunpack", "sounds"));

            ModImagePath = new Setting<string>(this, "Mod temp directory for images",
                "The place to store mods images that are used at runtime. In other words, this is the directory" +
                " that *.mod files are unpacked into.",
                Path.Combine(ConfigDir, "tempmodunpack", "assets"));

            ModDownloadPath = new Setting<string>(this, "Mod download directory", 
                "The directory to store mods downloaded from servers in.",
                Path.Combine(ConfigDir, "mods"));


            CoreMods = new Setting<string[]>(this, "Core Mods",
                "The core mods that will be autoinjected into every game without verification." + 
                "They must be DLL files.", DefaultTrustedMods);

            ImageSearchPaths = new Setting<string[]>(this, "Image search paths", "The paths in which to look for image assets.",
                new[] {
                    Directory.GetCurrentDirectory(), //current directory
                    Path.Combine(Directory.GetCurrentDirectory(), "assets"),
                    Path.Combine(Directory.GetCurrentDirectory(), "assets", "animations"),
                    Path.Combine(Directory.GetCurrentDirectory(), "assets", "mapobjects"),
                    Path.Combine(Directory.GetCurrentDirectory(), "assets", "other"),
                    Path.Combine(Directory.GetCurrentDirectory(), "assets", "tanks"),
                    Path.Combine(ModUnpackPath, "assets"),
                    Path.Combine(ConfigDir, "assets")
                });

            SoundSearchPaths = new Setting<string[]>(this, "Sound search paths", "The paths in which to look for sounds.",
                new[] {
                    Directory.GetCurrentDirectory(), //current directory
                    Path.Combine(Directory.GetCurrentDirectory(), "assets", "sounds"),
                    Path.Combine(Directory.GetCurrentDirectory(), "assets", "sounds", "animations"),
                    Path.Combine(Directory.GetCurrentDirectory(), "assets", "sounds","mapobjects"),
                    Path.Combine(Directory.GetCurrentDirectory(), "assets", "sounds", "other"),
                    Path.Combine(Directory.GetCurrentDirectory(), "assets", "sounds", "tanks"),
                    Path.Combine(ModUnpackPath, "sounds"),
                    Path.Combine(ConfigDir, "sounds")
                });

            RenderScale = new Setting<float>(this, "Render Scale",
            "The scale of rendering relative to game space so integer conversions work", 100f);

            PhysicsCompensationForRendering = new Setting<float>(this, "Physics Skin Compensation",
                "The amount in blocks to compensate for Farseer Physics's skin on bodies.", 0.085f);

            Fullscreen = new Setting<bool>(this, "Fullscreen mode",
                "Whether to render the game in fullscreen mode", false);
        }
    }
}
