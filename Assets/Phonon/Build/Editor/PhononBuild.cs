//
// Copyright (C) Valve Corporation. All rights reserved.
//

using System;
using UnityEditor;

namespace Phonon
{

    //
    // Build
    // Command-line build system for Phonon.
    //

    public static class Build
    {
        //
        // Phonon Common Assets
        //

        static string[] PhononCoreScripts =
        {
            "Assets/Phonon",
            "Assets/Phonon/Core"
        };

        //
        // Phonon Scene Assets
        //

        static string[] PhononSceneScripts =
        {
            "Assets/Phonon/Export"
        };


        //
        // Phonon Simulator Assets
        //

        static string[] PhononSimulatorScripts =
        {
            "Assets/Phonon/Simulator"
        };

        //
        // Phonon Plugin Assets
        //

        static string[] PhononPluginWindows =
        {
            "Assets/Plugins/x86/phonon.dll",
            "Assets/Plugins/x86/AudioPlugin_phonon_unity5.dll",
            "Assets/Plugins/x86_64/phonon.dll",
            "Assets/Plugins/x86_64/AudioPlugin_phonon_unity5.dll"
        };
        
        static string[] PhononPluginOSX =
        {
            "Assets/Plugins/phonon.bundle",
            "Assets/Plugins/AudioPlugin_phonon_unity5.bundle"
        };

        static string[] PhononPluginLinux =
        {
            "Assets/Plugins/x86/libphonon.so",
            "Assets/Plugins/x86/libAudioPlugin_phonon_unity5.so",
            "Assets/Plugins/x86_64/libphonon.so",
            "Assets/Plugins/x86_64/libAudioPlugin_phonon_unity5.so"
        };


        static string[] PhononPluginAndroid =
        {
            "Assets/Plugins/android/libphonon.so",
            "Assets/Plugins/android/libAudioPlugin_phonon_unity5.so"
        };


        //
        // BuildAssetList
        // Builds an asset list given an array of asset groups.
        //
        static string[] BuildAssetList(string[][] assetGroups)
        {
            int numAssets = 0;
            foreach (string[] assetGroup in assetGroups)
                numAssets += assetGroup.Length;

            string[] assets = new string[numAssets];

            int offset = 0;
            foreach (string[] assetGroup in assetGroups)
            {
                Array.Copy(assetGroup, 0, assets, offset, assetGroup.Length);
                offset += assetGroup.Length;
            }

            return assets;
        }

        //
        // BuildPhonon
        // Builds a Unity package for Phonon.
        //
        public static void BuildPhonon()
        {
            string[][] assetGroups = { PhononCoreScripts, PhononSceneScripts, PhononSimulatorScripts, PhononPluginWindows, PhononPluginOSX, PhononPluginLinux, PhononPluginAndroid };
            string[] assets = BuildAssetList(assetGroups);

            AssetDatabase.ExportPackage(assets, "Phonon.unitypackage", ExportPackageOptions.Recurse);
        }
    }
}