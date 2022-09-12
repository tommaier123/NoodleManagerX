using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;using System.Linq;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace NoodleManagerX.Utils
{
    // Taken from https://github.com/LavaGang/MelonLoader/blob/master/MelonLoader/InternalUtils/UnityInformationHandler.cs
    // Uses the Apache 2.0 License
    // Unnecessary checks and fallback data retrieval logic removed
    public static class UnityInformationHandler
    {
        public static string GameName { get; private set; } = "UNKNOWN";
        public static string GameDeveloper { get; private set; } = "UNKNOWN";
        public static string GameVersion { get; private set; } = "0";

        internal static void Setup(string gameDataDirectory)
        {
            AssetsManager assetsManager = new AssetsManager();
            ReadGameInfo(gameDataDirectory, assetsManager);
            assetsManager.UnloadAll();
        }

        private static void ReadGameInfo(string gameDataDirectory, AssetsManager assetsManager)
        {
            AssetsFileInstance instance = null;
            try
            {
                string bundlePath = Path.Combine(gameDataDirectory, "globalgamemanagers");
                if (!File.Exists(bundlePath))
                {
                    Console.Error.WriteLine("Couldn't find globalgamemanagers file for game version");
                    return;
                }
                    
                instance = assetsManager.LoadAssetsFile(bundlePath, true);
                if (instance == null)
                {
                    Console.Error.WriteLine("Couldn't load assets file");
                    return;
                }

                assetsManager.LoadIncludedClassPackage();
                if (!instance.file.typeTree.hasTypeTree)
                {
                    assetsManager.LoadClassDatabaseFromPackage(instance.file.typeTree.unityVersion);
                }

                List<AssetFileInfoEx> assetFiles = instance.table.GetAssetsOfType(129);
                if (assetFiles.Count > 0)
                {
                    AssetFileInfoEx playerSettings = assetFiles.First();

                    AssetTypeInstance assetTypeInstance = null;
                    try
                    {
                        assetTypeInstance = assetsManager.GetTypeInstance(instance, playerSettings);
                    }
                    catch (Exception ex)
                    {
                        assetsManager.LoadIncludedLargeClassPackage();
                        assetsManager.LoadClassDatabaseFromPackage(instance.file.typeTree.unityVersion);
                        assetTypeInstance = assetsManager.GetTypeInstance(instance, playerSettings);
                    }

                    if (assetTypeInstance != null)
                    {
                        AssetTypeValueField playerSettings_baseField = assetTypeInstance.GetBaseField();

                        AssetTypeValueField bundleVersion = playerSettings_baseField.Get("bundleVersion");
                        if (bundleVersion != null)
                        {
                            GameVersion = bundleVersion.GetValue().AsString();
                        }

                        AssetTypeValueField companyName = playerSettings_baseField.Get("companyName");
                        if (companyName != null)
                        {
                            GameDeveloper = companyName.GetValue().AsString();
                        }

                        AssetTypeValueField productName = playerSettings_baseField.Get("productName");
                        if (productName != null)
                        {
                            GameName = productName.GetValue().AsString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to Initialize Assets Manager! " + ex.Message);
            }

            if (instance != null)
            {
                instance.file.Close();
            }
        }

        public static ClassDatabasePackage LoadIncludedClassPackage(this AssetsManager assetsManager)
        {
            ClassDatabasePackage classPackage = null;
            using (MemoryStream mstream = new MemoryStream(Resources.Resources.classdata))
                classPackage = assetsManager.LoadClassPackage(mstream);
            return classPackage;
        }

        public static ClassDatabasePackage LoadIncludedLargeClassPackage(this AssetsManager assetsManager)
        {
            ClassDatabasePackage classPackage = null;
            using (MemoryStream mstream = new MemoryStream(Resources.Resources.classdata_large))
                classPackage = assetsManager.LoadClassPackage(mstream);
            return classPackage;
        }
    }
}
