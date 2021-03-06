﻿using MPTanks.Modding.Unpacker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MPTanks.Modding
{
    public static class ModLoader
    {
        private static Dictionary<string, Module> loadedModFiles { get; set; } = new Dictionary<string, Module>();
        public static IReadOnlyDictionary<string, Module> LoadedMods { get { return loadedModFiles; } }

        public static Module LoadCompressedModFile(string modFile, string dllUnpackDir, string mapUnpackDir, string assetUnpackDir, out string errors, bool verifySafe = true, bool overwriteExisting = false)
        {
            errors = "";
            if (loadedModFiles.ContainsKey(modFile))
                return loadedModFiles[modFile];

            Module output = null;
            string err = "";
#if !DISABLE_ERROR_HANDLING_FOR_MODLOADER
            try
            {
#endif
            //Get the header to resolve dependencies
            var header = ModUnpacker.GetHeader(modFile);
            var deps = new List<string>();
            //Resolve the dependencies and get all of their dlls
            foreach (var dep in header.Dependencies)
                deps.AddRange(DependencyResolver.LoadDependency(dep.ModName, dep.Major, dep.Minor,
                    dllUnpackDir, mapUnpackDir, assetUnpackDir, header.Name, overwriteExisting));
            //Remove duplicates
            deps = deps.Distinct().ToList();
            //Then, unpack (our) assemblies to the correct directory
            var dllPaths = ModUnpacker.UnpackDlls(modFile, dllUnpackDir, overwriteExisting);

            //If it has source code, compile that
            if (header.CodeFiles.Length > 0)
            {
                string mErr = "";
                output = LoadFromSourceCode(
                         ModUnpacker.GetSourceCode(modFile),
                         verifySafe,
                         out mErr,
                         dllPaths,
                         deps.ToArray()
                         );

                err += mErr;
            }
            else
            {
                //Otherwise, just do a simple load
                string mErr = "";
                output = LoadPrecompiled(
                      dllPaths, verifySafe, out mErr);
                err += mErr;
            }

            //Check if the mod failed at loading
            errors = err;
            if (output == null) return null;

            //If it didn't, write the header
            output.Header = ModUnpacker.GetHeader(modFile);
            //And finally, unpack assets
            MapAssetFileNames(output.Header.ImageFiles, ModUnpacker.UnpackImages(modFile, assetUnpackDir, overwriteExisting), output);
            MapAssetFileNames(output.Header.SoundFiles, ModUnpacker.UnpackSounds(modFile, assetUnpackDir, overwriteExisting), output);
            MapAssetFileNames(output.Header.MapFiles, ModUnpacker.UnpackMaps(modFile, mapUnpackDir, overwriteExisting), output);
            MapAssetFileNames(output.Header.ComponentFiles, ModUnpacker.UnpackComponents(modFile, assetUnpackDir, overwriteExisting), output);
#if !DISABLE_ERROR_HANDLING_FOR_MODLOADER
        }
            catch (Exception ex)
            {
                err += ex.ToString() + "\n\n";
            }
#endif
            errors = err;
            loadedModFiles.Add(modFile, output);

            output.PackedFile = modFile;

            //Mark the mod as loaded
            ModDatabase.AddLoaded(output);
            return output;
        }

        private static void MapAssetFileNames(string[] src, string[] dst, Module module)
        {
            for (var i = 0; i < src.Length; i++)
                module.AssetMappings.Add(src[i], dst[i]);
        }
        
        public static Module LoadFromSourceCode(string source, bool verifySafe, out string errors, string[] precompiledAssemblies = null, string[] otherAssemblyReferences = null)
        {
            return LoadFromSourceCode(new[] { source }, verifySafe, out errors, precompiledAssemblies, otherAssemblyReferences);
        }
        public static Module LoadFromSourceCode(string[] sources, bool verifySafe, out string errors, string[] precompiledAssemblies = null, string[] otherAssemblyReferences = null)
        {
            //Null ref protection
            if (otherAssemblyReferences == null) otherAssemblyReferences = new string[0];
            if (precompiledAssemblies == null) precompiledAssemblies = new string[0];

            //Then compile everything
            var compileErrors = "";
            var asm = Compiliation.Compiler.CompileAssembly(sources, out compileErrors, otherAssemblyReferences);
            var mbuilderrors = "";

            //Catch failures
            if (asm == null)
            {

                errors = "Compiliation failed \n\n\n" + compileErrors;
                return null;
            }
            //Otherwise, build the list of both pre-built (dependency) assemblies + this one
            var assemblies = precompiledAssemblies.ToList();
            assemblies.Add(asm);
            //Build the module
            var module = LoadPrecompiled(assemblies.ToArray(), verifySafe, out mbuilderrors);
            //Mark the errors
            errors = compileErrors + "\n\n\n" + mbuilderrors;
            return module;

        }

        public static Module LoadPrecompiled(string asm, bool verifySafe, out string errors)
        {
            return LoadPrecompiled(new[] { asm }, verifySafe, out errors);
        }

        public static Module LoadPrecompiled(string[] assemblies, bool verifySafe, out string errors)
        {
            //Make sure that all of the assemblies have absolute paths (if not, we will crash when loading.
            assemblies = assemblies.Select((a) =>
            {
                if (Path.IsPathRooted(a))
                    return a;
                else
                    return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, a);
            }).ToArray();
            //Check that all assemblies conform to the white list by scanning the IL
            var safetyCheckErrors = "";
            if (verifySafe)
            {
                foreach (var asm in assemblies)
                {
                    if (!Compiliation.Verification.WhitelistVerify.IsAssemblySafe(asm, out safetyCheckErrors))
                    {
                        errors = safetyCheckErrors;
                        return null;
                    }
                }
            }

            //Then move on to building the module
            var builderErrors = "";
            var module = new Module();


            //Get the declaration
            ModuleDeclarationAttribute moduleDeclaration = null;
            foreach (var asm in assemblies)
            {
                var decl = FindModuleDeclaration(Assembly.LoadFile(asm));
                if (decl != null)
                {
                    //Check that it hasn't already been loaded
                    //If it is, we probably are looking at the wrong attribute (probably the
                    //declaration of a loaded dependency)
                    bool canBreak = true;
                    foreach (var mod in ModDatabase.LoadedModulesList)
                        if (mod.Name.Equals(moduleDeclaration.Name, StringComparison.InvariantCultureIgnoreCase) &&
                            mod.Version.Major == moduleDeclaration.Version.Major
                            && mod.Version.Minor == moduleDeclaration.Version.Minor)
                            canBreak = false;
                    if (canBreak)
                    {
                        moduleDeclaration = decl;
                        break;
                    }
                }
            }
            if (moduleDeclaration == null)
            {
                builderErrors = "Missing module declaration. ([ModuleDeclarationAttribute]). Cannot proceed.";
                errors = safetyCheckErrors + "\n\n\n" + builderErrors;
                return null;
            }
            //Load all of the assemblies
            module.Assemblies = assemblies.Select(a => Assembly.LoadFile(a)).ToArray();
            //Resolve dependencies
            module.Dependencies =
                module.Assemblies.SelectMany(
                    a => a.GetReferencedAssemblies().Select(
                        an => Assembly.Load(an))).ToArray();
            //And get the name
            module.Name = moduleDeclaration.Name;
            module.Description = moduleDeclaration.Description;
            module.Author = moduleDeclaration.Author;
            module.Version = moduleDeclaration.Version;

            //Tanks
            var tanks = new List<TankType>();
            foreach (var asm in module.Assemblies)
                foreach (var tank in ScanTankTypes(asm))
                {
#if !DISABLE_ERROR_HANDLING_FOR_MODLOADER
                    try
                    {
#endif
                    var typ = new TankType(tank, module);
                    Inject(typ, module);
                    tanks.Add(typ);
#if !DISABLE_ERROR_HANDLING_FOR_MODLOADER
                }
                    catch (Exception e)
                    {
                        builderErrors += "\n\n\nTank: " + tank.FullName + " has error: " + e.Message;
                    }
#endif
                }
            module.Tanks = tanks.ToArray();

            //Projectiles
            var projectiles = new List<ProjectileType>();
            foreach (var asm in module.Assemblies)
                foreach (var prj in ScanProjectileTypes(asm))
                {
#if !DISABLE_ERROR_HANDLING_FOR_MODLOADER
                    try
                    {
#endif
                    var typ = new ProjectileType(prj, module);
                    Inject(typ, module);
                    projectiles.Add(typ);
#if !DISABLE_ERROR_HANDLING_FOR_MODLOADER
                    }
                    catch (Exception e)
                    {
                        builderErrors += "\n\n\nProjectile: " + prj.FullName + " has error: " + e.Message;
                    }
#endif
                }
            module.Projectiles = projectiles.ToArray();

            //Map objects
            var mapObjects = new List<MapObjectType>();
            foreach (var asm in module.Assemblies)
                foreach (var mapObject in ScanMapObjectTypes(asm))
                {
#if !DISABLE_ERROR_HANDLING_FOR_MODLOADER
                    try
                    {
#endif
                    var typ = new MapObjectType(mapObject, module);
                    Inject(typ, module);
                    mapObjects.Add(typ);
#if !DISABLE_ERROR_HANDLING_FOR_MODLOADER
                    }
                    catch (Exception e)
                    {
                        builderErrors += "\n\n\nMap object: " + mapObject.FullName + " has error: " + e.Message;
                    }
#endif
                }
            module.MapObjects = mapObjects.ToArray();

            //Other Gameobjects
            var gameObjects = new List<GameObjectType>();
            foreach (var asm in module.Assemblies)
                foreach (var gameObject in ScanOtherGameObjectTypes(asm))
                {
#if !DISABLE_ERROR_HANDLING_FOR_MODLOADER
                    try
                    {
#endif
                    var typ = new GameObjectType(gameObject, module);
                    Inject(typ, module);
                    gameObjects.Add(typ);
#if !DISABLE_ERROR_HANDLING_FOR_MODLOADER
                    }
                    catch (Exception e)
                    {
                        builderErrors += "\n\n\nMap object: " + gameObject.FullName + " has error: " + e.Message;
                    }
#endif
                }
            module.GameObjects = gameObjects.ToArray();

            //Gamemodes
            var gamemodes = new List<GamemodeType>();
            foreach (var asm in module.Assemblies)
                foreach (var gamemode in ScanGamemodeTypes(asm))
                {
#if !DISABLE_ERROR_HANDLING_FOR_MODLOADER
                    try
                    {
#endif
                    var typ = new GamemodeType(gamemode, module);
                    Inject(typ, module);
                    gamemodes.Add(typ);
#if !DISABLE_ERROR_HANDLING_FOR_MODLOADER
                    }
                    catch (Exception e)
                    {
                        builderErrors += "\n\n\nGamemode: " + gamemode.FullName + " has error: " + e.Message;
                    }
#endif
                }
            module.Gamemodes = gamemodes.ToArray();

            //And call the constructors
            foreach (var asm in module.Assemblies)
                CallStaticCtors(asm);

            //And finally, inject the code

            errors = safetyCheckErrors + "\n\n" + builderErrors;
            module.PackedFile = assemblies[0];

            module.UsesWhitelist = verifySafe;

            return module;
        }

        public static void CallStaticCtors(Assembly asm)
        {
            foreach (Type t in asm.GetTypes())
            {
                try
                {
                    System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(t.TypeHandle);
                }
                catch (TypeInitializationException)
                {
                }
            }

            GC.Collect(2, GCCollectionMode.Forced, true);
        }


        private static ModuleDeclarationAttribute FindModuleDeclaration(Assembly asm)
        {
            foreach (var type in asm.GetTypes())
                if (type.GetCustomAttribute<ModuleDeclarationAttribute>() != null)
                    return type.GetCustomAttribute<ModuleDeclarationAttribute>();

            return null;
        }
        #region Type scanning
        private static Type[] ScanProjectileTypes(Assembly asm)
        {
            var projectileTypes = new List<Type>();
            foreach (var type in asm.GetTypes())
                if (ProjectileType.IsProjectileType(type))
                    projectileTypes.Add(type);

            return projectileTypes.ToArray();
        }
        private static Type[] ScanGamemodeTypes(Assembly asm)
        {
            var gamemodeTypes = new List<Type>();
            foreach (var type in asm.GetTypes())
                if (GamemodeType.IsGamemodeType(type))
                    gamemodeTypes.Add(type);

            return gamemodeTypes.ToArray();
        }
        private static Type[] ScanMapObjectTypes(Assembly asm)
        {
            var mapObjectTypes = new List<Type>();
            foreach (var type in asm.GetTypes())
                if (MapObjectType.IsMapObjectType(type))
                    mapObjectTypes.Add(type);

            return mapObjectTypes.ToArray();
        }
        private static Type[] ScanTankTypes(Assembly asm)
        {
            var tankTypes = new List<Type>();
            foreach (var type in asm.GetTypes())
                if (TankType.IsTankType(type))
                    tankTypes.Add(type);

            return tankTypes.ToArray();
        }
        private static Type[] ScanOtherGameObjectTypes(Assembly asm)
        {
            var types = new List<Type>();
            foreach (var type in asm.GetTypes())
                if (GameObjectType.IsGameObjectType(type) &&
                    !TankType.IsTankType(type) &&
                    !ProjectileType.IsProjectileType(type) &&
                    !MapObjectType.IsMapObjectType(type))
                    types.Add(type);

            return types.ToArray();
        }
        #endregion
        #region Type Injection
        private static void Inject(TankType type , Module module)
        {
            var typ = GetTypeHelper.GetType(ModSettings.TankTypeName);
            var method = typ.GetMethod("RegisterType", BindingFlags.Static | BindingFlags.NonPublic);
            method.MakeGenericMethod(type.Type).Invoke(null,new[] { module });
        }
        private static void Inject(ProjectileType type, Module module)
        {
            var typ = GetTypeHelper.GetType(ModSettings.ProjectileTypeName);
            var method = typ.GetMethod("RegisterType", BindingFlags.Static | BindingFlags.NonPublic);
            method.MakeGenericMethod(type.Type).Invoke(null, new[] { module });
        }
        private static void Inject(MapObjectType type, Module module)
        {
            var typ = GetTypeHelper.GetType(ModSettings.MapObjectTypeName);
            var method = typ.GetMethod("RegisterType", BindingFlags.Static | BindingFlags.NonPublic);
            method.MakeGenericMethod(type.Type).Invoke(null, new[] { module });
        }
        private static void Inject(GamemodeType type, Module module)
        {
            var typ = GetTypeHelper.GetType(ModSettings.GamemodeTypeName);
            var method = typ.GetMethod("RegisterType", BindingFlags.Static | BindingFlags.NonPublic);
            method.MakeGenericMethod(type.Type).Invoke(null, new[] { module });
        }
        private static void Inject(GameObjectType type, Module module)
        {
            var typ = GetTypeHelper.GetType(ModSettings.GameObjectTypeName);
            var method = typ.GetMethod("RegisterType", BindingFlags.Static | BindingFlags.NonPublic);
            method.MakeGenericMethod(type.Type).Invoke(null, new[] { module });
        }
        #endregion
    }
}
