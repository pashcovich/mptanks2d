﻿using MPTanks.Modding.Unpacker;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPTanks.Modding
{
    [Serializable]
    public class ModMetadata
    {

        private static Dictionary<string, ModMetadata> _cache = new Dictionary<string, ModMetadata>();

        static ModMetadata()
        {
            //Don't load because we're on a short lived domain
            if (AppDomain.CurrentDomain.GetData("__metadata__creation__domain") != null) return;

            //Load
            if (File.Exists(Path.Combine(Settings.ConfigDir, "Mod Metadata Cache.json")))
                _cache = JsonConvert.DeserializeObject<Dictionary<string, ModMetadata>>(
                    File.ReadAllText(Path.Combine(Settings.ConfigDir, "Mod Metadata Cache.json")));
        }

        private static void Save()
        {
            File.WriteAllText(Path.Combine(Settings.ConfigDir, "Mod Metadata Cache.json"),
                JsonConvert.SerializeObject(_cache));
        }

        /// <summary>
        /// Warning! Very expensive. Creates an appdomain, unpacks a mod, and inspects its contents
        /// to get a list of all files in the mod.
        /// </summary>
        public static ModMetadata CreateMetadata(string modFile)
        {
            if (_cache.ContainsKey(modFile.ToLower()))
                return _cache[modFile.ToLower()];

            ModMetadata meta = null;

            var domain = AppDomain.CreateDomain($"__Temp__ModMetadata::CreateMetadata({modFile})");
            domain.Load(new System.Reflection.AssemblyName(typeof(ModMetadata).Assembly.FullName));

            domain.SetData("__create__modFile", modFile);
            //Flag the domain for special case usage - avoid loading the entire metadata cache which we will not use.
            domain.SetData("__metadata__creation__domain", new object());
            var crossDomainTarget = new CrossAppDomainDelegate(Create);

            domain.DoCallBack(crossDomainTarget);
            meta = (ModMetadata)domain.GetData("__create__return__metadata");

            AppDomain.Unload(domain);

            _cache.Add(modFile.ToLower(), meta);
            Save();

            return meta;
        }

        private static void Create()
        {
            var modFile = (string)AppDomain.CurrentDomain.GetData("__create__modFile");
            ModMetadata result = new ModMetadata();

            string errors = null;

            Module modData;

            FileInfo fi = new FileInfo(modFile);

            if (fi.Extension.ToLower().EndsWith("dll"))
                modData = ModLoader.Load(modFile, true, out errors);
            else
                modData = ModLoader.LoadMod(modFile, Settings.MetadataModUnpackDir,
               Settings.MetadataModUnpackDir, out errors, true);

            result.ModFile = modFile;

            result.GameObjects = modData.GameObjects.Select(a => new GameObjectDescriptor()
            {
                Name = a.DisplayName,
                Description = ((GameObjectAttribute)
                    (a.Type.GetCustomAttributes(typeof(GameObjectAttribute), true)[0])).Description,
                ComponentsFile = ((GameObjectAttribute)
                    (a.Type.GetCustomAttributes(typeof(GameObjectAttribute), true)[0])).ComponentFile,
                ReflectionName = a.ReflectionTypeName,
                Owner = result
            }).ToArray();

            result.Tanks = modData.Tanks.Select(a => new GameObjectDescriptor()
            {
                Name = a.DisplayName,
                Description = ((GameObjectAttribute)
                    (a.Type.GetCustomAttributes(typeof(GameObjectAttribute), true)[0])).Description,
                ComponentsFile = ((GameObjectAttribute)
                    (a.Type.GetCustomAttributes(typeof(GameObjectAttribute), true)[0])).ComponentFile,
                ReflectionName = a.ReflectionTypeName,
                Owner = result
            }).ToArray();

            result.Projectiles = modData.Projectiles.Select(a => new GameObjectDescriptor()
            {
                Name = a.DisplayName,
                Description = ((GameObjectAttribute)
                    (a.Type.GetCustomAttributes(typeof(GameObjectAttribute), true)[0])).Description,
                ComponentsFile = ((GameObjectAttribute)
                    (a.Type.GetCustomAttributes(typeof(GameObjectAttribute), true)[0])).ComponentFile,
                ReflectionName = a.ReflectionTypeName,
                Owner = result
            }).ToArray();

            result.MapObjects = modData.MapObjects.Select(a => new GameObjectDescriptor()
            {
                Name = a.DisplayName,
                Description = ((GameObjectAttribute)
                    (a.Type.GetCustomAttributes(typeof(GameObjectAttribute), true)[0])).Description,
                ComponentsFile = ((GameObjectAttribute)
                    (a.Type.GetCustomAttributes(typeof(GameObjectAttribute), true)[0])).ComponentFile,
                ReflectionName = a.ReflectionTypeName,
                Owner = result
            }).ToArray();

            result.Gamemodes = modData.Gamemodes.Select(a => new GamemodeDescriptor()
            {
                Name = a.DisplayName,
                Description = a.DisplayDescription,
                MinPlayerCount = a.MinPlayersCount,
                Owner = result,
                HotJoinAllowed = a.HotJoinAllowed,
                ReflectionName = a.ReflectionTypeName
            }).ToArray();

            AppDomain.CurrentDomain.SetData("__create__return__metadata", result);
        }

        public string ModFile { get; private set; }
        public GameObjectDescriptor[] GameObjects { get; private set; }
        public GameObjectDescriptor[] Projectiles { get; private set; }
        public GameObjectDescriptor[] Tanks { get; private set; }
        public GameObjectDescriptor[] MapObjects { get; private set; }
        public GamemodeDescriptor[] Gamemodes { get; private set; }

    }
    [Serializable]
    public class GameObjectDescriptor
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ReflectionName { get; set; }
        public string ComponentsFile { get; set; }
        public string ComponentsJSONData
        {
            get { return ModUnpacker.GetStringFile(Owner.ModFile, ComponentsFile); }
        }
        public ModMetadata Owner { get; set; }
    }
    [Serializable]
    public class GamemodeDescriptor
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ReflectionName { get; set; }
        public bool HotJoinAllowed { get; set; }
        public int MinPlayerCount { get; set; }
        public ModMetadata Owner { get; set; }
    }
}
