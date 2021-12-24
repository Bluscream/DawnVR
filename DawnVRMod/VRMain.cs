﻿using System.Collections;
using DawnVR.Modules.VR;
using DawnVR.Modules;
using MelonLoader;
using UnityEngine;
using System.Linq;
using Valve.VR;
using System;
using System.Net;
using System.Net.NetworkInformation;
using Ping = System.Net.NetworkInformation.Ping;

namespace DawnVR
{
    // playthrough notes
    // customization broke somewhat

    // todo addition list
    // seated/standing with offset using alec's code: https://canary.discord.com/channels/@me/727403137337524264/917571166443552769
    public static class BuildInfo
    {
        public const string Name = "DawnVR";
        public const string Author = "trev";
        public const string Company = null;
        public const string Version = "0.0.1";
        public const string DownloadLink = null;
    }

    public class VRMain : MelonMod
    {
        private const string githubApiUrl = "https://api.github.com/repos/TrevTV/DawnVR/releases/latest";
        private bool vrEnabled;
        private bool steamInitRunning;

        public override void OnApplicationStart()
        {
            Preferences.Init();

            if (Preferences.CheckForUpdatesOnStart.Value)
                CheckForUpdates();

            if (Environment.GetCommandLineArgs().Contains("OpenVR"))
                vrEnabled = true;
            else
            {
                MelonLogger.Msg("Launch parameter \"-vrmode\" not set to OpenVR, not loading VR patches!");
                if (Preferences.EnableInternalLogging.Value)
                    OutputRedirect.Init(HarmonyInstance);
                HarmonyPatches.InitNoVR(HarmonyInstance);
                vrEnabled = false;
                return;
            }

            Modules.Resources.Init();
            HarmonyPatches.Init(HarmonyInstance);
            if (Preferences.EnableInternalLogging.Value)
                OutputRedirect.Init(HarmonyInstance);
        }

        private void CheckForUpdates()
        {
            MelonLogger.Msg("Checking for updates...");

            string scriptPath = System.IO.Path.Combine(MelonUtils.UserDataDirectory, "CheckForUpdate.ps1");

            if (!System.IO.File.Exists(scriptPath))
                ResourceLoader.WriteResourceToFile(scriptPath, "CheckForUpdate.ps1");

            var process = new System.Diagnostics.Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.FileName = "powershell.exe";
            process.StartInfo.Arguments = $"-file \"{scriptPath}\" \"{githubApiUrl}\"";

            process.Start();
            string returnVal = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (returnVal == "ERR:NO_INTERNET")
            {
                MelonLogger.Warning("You are not connected to the internet, skipping update check.");
                return;
            }

            SimpleJSON.JSONNode node = SimpleJSON.JSON.Parse(returnVal);
            Version version = new Version(node["tag_name"].Value);

            if (version > new Version(BuildInfo.Version))
            {
                MelonLogger.Warning("============================================================");
                MelonLogger.Warning($"    A new version of DawnVR ({version}) is available!     ");
                MelonLogger.Warning("Download it here, https://github.com/TrevTV/DawnVR/releases");
                MelonLogger.Warning("============================================================");
            }
            else
                MelonLogger.Msg("Up to date.");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (!vrEnabled) return;

            if (SteamVR.initializedState != SteamVR.InitializedStates.InitializeSuccess && !steamInitRunning)
                MelonCoroutines.Start(InitSteamVR());
        }

        private IEnumerator InitSteamVR()
        {
            yield return new WaitForSeconds(1f);
            steamInitRunning = true;
            SteamVR.Initialize(false);

            while (SteamVR.initializedState != SteamVR.InitializedStates.InitializeSuccess)
            {
                if (SteamVR.initializedState == SteamVR.InitializedStates.InitializeFailure)
                {
                    MelonLogger.Error("[SteamVR] Initialization failure! Disabling VR modules.");
                    vrEnabled = false;
                    yield break;
                }
                yield return null;
            }

            steamInitRunning = false;

            CreateCameraRig();
        }

        private void CreateCameraRig()
        {
            GameObject rig = GameObject.Instantiate(Modules.Resources.VRCameraRig);
            if (!VRRig.Instance)
                VRRig.Instance = rig.AddComponent<VRRig>();
        }
    }
}