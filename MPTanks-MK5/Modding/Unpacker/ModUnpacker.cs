﻿using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPTanks.Modding.Unpacker
{
    public static class ModUnpacker
    {
        private static ZipEntry GetEntry(string filename, ZipFile zf)
        {
            foreach (ZipEntry ze in zf)
            {
                if (ze.Name.Equals(filename, StringComparison.InvariantCultureIgnoreCase))
                    return ze;
            }
            return null;
        }
        private static byte[] GetData(string filename, ZipFile zf)
        {
            var stream = zf.GetInputStream(GetEntry(filename, zf));
            var bytes = new List<byte>();
            int bt;
            while ((bt = stream.ReadByte()) != -1)
                bytes.Add((byte)bt);
            return bytes.ToArray();
        }

        private static string ReadText(string filename, ZipFile zf)
        {
            return Encoding.UTF8.GetString(GetData(filename, zf));
        }

        private static ZipFile OpenZip(string fileName)
        {
            var zf = new ZipFile(new FileStream(fileName, FileMode.Open, FileAccess.Read));
            zf.IsStreamOwner = true;
            return zf;
        }

        public static ModHeader GetHeader(string modFile)
        {
            var zf = OpenZip(modFile);
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject<ModHeader>(ReadText("mod.json", zf));
            zf.Close();
            return obj;
        }

        public static string[] UnpackDlls(string modFile, string outputDir)
        {
            //we unpack to modName_modMajor_modMinor_assetName.dll
            var header = GetHeader(modFile);
            var zf = OpenZip(modFile);
            var dlls = new List<string>();

            foreach (var dll in header.DLLFiles)
            {
                var path = Path.Combine(outputDir, $"{header.Name}_{header.Major}_{header.Minor}_{dll}");
                if (!File.Exists(path))
                    File.WriteAllBytes(path,
                    GetData(dll, zf));
                dlls.Add(path);
            }
            zf.Close();
            return dlls.ToArray();
        }
        public static string[] UnpackSounds(string modFile, string outputDir)
        {
            //we unpack to modFile_modMajor_modMinor_assetName.ogg/mp3/ac3/wav
            var header = GetHeader(modFile);
            var zf = OpenZip(modFile);

            var files = new List<string>();

            foreach (var sound in header.SoundFiles)
            {
                var path = Path.Combine(outputDir, $"{header.Name}_{header.Major}_{header.Minor}_{sound}");
                if (!File.Exists(path))
                    File.WriteAllBytes(path,
                        GetData(sound, zf));
                files.Add(path);
            }
            zf.Close();
            return files.ToArray();
        }
        public static string[] UnpackImages(string modFile, string outputDir)
        {
            //we unpack by modFile_modMajor_modMinor_assetName and *.json
            var header = GetHeader(modFile);
            var zf = OpenZip(modFile);

            var files = new List<string>();

            foreach (var img in header.ImageFiles)
            {
                var path = Path.Combine(outputDir, $"{header.Name}_{header.Major}_{header.Minor}_{img}");
                if (!File.Exists(path))
                    File.WriteAllBytes(path,
                    GetData(img, zf));
                files.Add(path);

                var jsonPath = Path.Combine(outputDir, $"{header.Name}_{header.Major}_{header.Minor}_{img}.json");
                if (!File.Exists(jsonPath))
                    File.WriteAllBytes(jsonPath,
                    GetData($"{img}.json", zf));
            }
            zf.Close();

            return files.ToArray();
        }

        public static string[] UnpackMaps(string modFile, string outputDir)
        {
            //we unpack by modFile_modMajor_modMinor_assetName.json
            var header = GetHeader(modFile);
            var zf = OpenZip(modFile);

            var files = new List<string>();

            foreach (var map in header.MapFiles)
            {
                var path = Path.Combine(outputDir, $"{header.Name}_{header.Major}_{header.Minor}_{map}");
                if (!File.Exists(path))
                    File.WriteAllBytes(path,
                    GetData(map, zf));
                files.Add(path);
            }
            zf.Close();

            return files.ToArray();
        }

        public static string[] UnpackComponents(string modFile, string outputDir)
        {
            //we unpack by modFile_modMajor_modMinor_assetName.json
            var header = GetHeader(modFile);
            var zf = OpenZip(modFile);

            var files = new List<string>();

            foreach (var component in header.ComponentFiles)
            {
                var path = Path.Combine(outputDir, $"{header.Name}_{header.Major}_{header.Minor}_{component}");
                if (!File.Exists(path))
                    File.WriteAllBytes(path,
                    GetData(component, zf));
                files.Add(path);
            }
            zf.Close();

            return files.ToArray();
        }

        public static string[] GetSourceCode(string modFile)
        {
            var codePages = new List<string>();
            var header = GetHeader(modFile);
            var zf = OpenZip(modFile);

            foreach (var cf in header.CodeFiles)
            {
                codePages.Add(ReadText(cf, zf));
            }
            zf.Close();
            return codePages.ToArray();
        }

        public static string GetStringFile(string modFile, string internalFileName)
        {
            var zf = OpenZip(modFile);
            var text = ReadText(internalFileName, zf);
            zf.Close();
            return text;
        }

        public static byte[] GetByteArrayFile(string modFile, string internalFileName)
        {
            var zf = OpenZip(modFile);
            var data = GetData(internalFileName, zf);
            zf.Close();
            return data;
        }
    }
}