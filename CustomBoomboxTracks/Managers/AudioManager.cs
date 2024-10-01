using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using Concentus;
using Concentus.Oggfile;
using CustomBoomboxTracks.Configuration;
using CustomBoomboxTracks.Utilities;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;

namespace CustomBoomboxTracks.Managers
{
    internal static class AudioManager
    {
        public static event Action OnAllSongsLoaded;
        public static bool FinishedLoading => finishedLoading;

        static string[] allSongPaths;
        static List<AudioClip> clips = new List<AudioClip>();
        static bool firstRun = true;
        static bool finishedLoading = false;

        static readonly string customSongsFolder = Path.Combine(Paths.BepInExRootPath, "Custom Songs", "Boombox Music");
        static readonly string configFolder = Path.Combine(Paths.ConfigPath, "Custom Songs");

        public static bool HasNoSongs => allSongPaths.Length == 0;

        public static void GenerateFolders()
        {
            if (Config.DeleteCustomSongsFolder)
            {
                if (Directory.Exists(customSongsFolder))
                {
                    Directory.Delete(customSongsFolder, true);
                    BoomboxPlugin.LogInfo($"Option was set to true. Deleted {customSongsFolder}");
                }
            }
            else if (Config.UseCustomSongsFolder)
            {
                Directory.CreateDirectory(customSongsFolder);
                BoomboxPlugin.LogInfo($"Created directory at {customSongsFolder}");
            }

            if (Config.UseConfigFolder && !Directory.Exists(configFolder))
            {
                Directory.CreateDirectory(configFolder);
                BoomboxPlugin.LogInfo($"Created directory at {configFolder}");
            }
        }

        public static void Load()
        {
            if (firstRun)
            {
                firstRun = false;
                allSongPaths = SearchForSongs().ToArray();

                if (allSongPaths.Length == 0)
                {
                    BoomboxPlugin.LogWarning("No songs found!");
                    return;
                }

                BoomboxPlugin.LogInfo("Preparing to load AudioClips...");

                var coroutines = new List<Coroutine>();
                foreach (var track in allSongPaths)
                {
                    var coroutine = SharedCoroutineStarter.StartCoroutine(LoadAudioClip(track));
                    coroutines.Add(coroutine);
                }

                SharedCoroutineStarter.StartCoroutine(WaitForAllClips(coroutines));
            }
        }

        private static IEnumerable<string> SearchForSongs()
        {
            IEnumerable<string> result = new List<string>();
            if (Config.UseCustomSongsFolder && Directory.Exists(customSongsFolder))
            {
                result = result.Concat(Directory.GetFiles(customSongsFolder));
                BoomboxPlugin.LogInfo($"Pulling songs from {customSongsFolder}");
            }
            if (Config.UseConfigFolder && Directory.Exists(configFolder))
            {
                result = result.Concat(Directory.GetFiles(configFolder));
                BoomboxPlugin.LogInfo($"Pulling songs from {configFolder}");
            }

            if (Config.SearchForCustomSongs)
            {
                BoomboxPlugin.LogInfo($"Searching for custom songs!");

                string[] pluginPaths = Directory.GetDirectories(Paths.PluginPath);
                foreach (string pluginPath in pluginPaths)
                {
                    string pluginCustomSongsPath = Path.Combine(pluginPath, "Custom Songs");
                    if (!Directory.Exists(pluginCustomSongsPath))
                    {
                        continue;
                    }

                    BoomboxPlugin.LogInfo($"Found some in {pluginCustomSongsPath}");

                    string[] pluginCustomSongsPaths = Directory.GetFiles(pluginCustomSongsPath);
                    foreach (string pluginCustomSongPath in pluginCustomSongsPaths)
                    {
                        if (GetAudioType(pluginCustomSongPath) == AudioType.UNKNOWN && !GetIsOpus(pluginCustomSongPath))
                        {
                            BoomboxPlugin.LogWarning($"Not a valid song {pluginCustomSongPath}");
                            continue;
                        }

                        BoomboxPlugin.LogInfo($"Song found {pluginCustomSongPath}");
                        result = result.AddItem(pluginCustomSongPath);
                    }
                }
            }

            return result;
        }

        private static IEnumerator LoadAudioClip(string filePath)
        {
            BoomboxPlugin.LogInfo($"Loading {filePath}!");

            AudioType audioType = GetAudioType(filePath);
            if (audioType == AudioType.UNKNOWN)
            {
                bool isOpus = GetIsOpus(filePath);
                if (isOpus)
                {
                    yield return LoadOpus(filePath);
                }
                else
                {
                    BoomboxPlugin.LogError($"Failed to load AudioClip from {filePath}\nUnsupported file extension!");
                    yield break;
                }
            }
            else yield return LoadNormally(filePath, audioType);
        }

        private static IEnumerator LoadOpus(string filePath)
        {
            BoomboxPlugin.LogInfo($"It's an Opus file!");

            List<float> fileOut = new List<float>();
            using (FileStream fileIn = new FileStream(filePath, FileMode.Open))
            {
                IOpusDecoder decoder = OpusCodecFactory.CreateDecoder(Config.OpusDecodeSampleRate, 1, Console.Out);
                OpusOggReadStream oggIn = new OpusOggReadStream(decoder, fileIn);
                while (oggIn.HasNextPacket)
                {
                    short[] packet = oggIn.DecodeNextPacket();
                    if (packet != null)
                    {
                        float[] binary = new float[packet.Length];
                        for (int i = 0; i < packet.Length; i++)
                        {
                            binary[i] = packet[i] / 32768.0f;
                        }
                        fileOut.AddRange(binary);
                    }
                }

                decoder.Dispose();
                fileIn.Dispose();
            }

            AudioClip clip = AudioClip.Create(Path.GetFileName(filePath), fileOut.Count, 1, Config.OpusDecodeSampleRate, false);
            clip.SetData(fileOut.ToArray(), 0);
            clips.Add(clip);

            fileOut.Clear();

            BoomboxPlugin.LogInfo($"Loaded Opus file {filePath}");

            yield break;
        }

        private static IEnumerator LoadNormally(string filePath, AudioType audioType)
        {
            BoomboxPlugin.LogInfo($"It's a normal file!");

            UnityWebRequest loader = UnityWebRequestMultimedia.GetAudioClip(filePath, audioType);

            if (Config.StreamFromDisk) (loader.downloadHandler as DownloadHandlerAudioClip).streamAudio = true;

            loader.SendWebRequest();

            while (true)
            {
                if (loader.isDone) break;
                yield return null;
            }

            if (loader.error != null)
            {
                BoomboxPlugin.LogError($"Error loading clip from path: {filePath}\n{loader.error}");
                BoomboxPlugin.LogError(loader.error);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(loader);
            if (clip && clip.loadState == AudioDataLoadState.Loaded)
            {
                BoomboxPlugin.LogInfo($"Loaded normal file {filePath}");
                clip.name = Path.GetFileName(filePath);
                clips.Add(clip);
                yield break;
            }

            // Failed to load.
            BoomboxPlugin.LogError($"Failed to load clip at: {filePath}\nThis might be due to an mismatch between the audio codec and the file extension!");
        }

        private static IEnumerator WaitForAllClips(List<Coroutine> coroutines)
        {
            foreach (var coroutine in coroutines)
            {
                yield return coroutine;
            }

            clips.Sort((first, second) => first.name.CompareTo(second.name));

            finishedLoading = true;
            OnAllSongsLoaded?.Invoke();
            OnAllSongsLoaded = null;
        }

        public static void ApplyClips(BoomboxItem __instance)
        {
            BoomboxPlugin.LogInfo($"Applying clips!");

            if (Config.UseDefaultSongs)
                __instance.musicAudios = __instance.musicAudios.Concat(clips).ToArray();
            else
                __instance.musicAudios = clips.ToArray();

            BoomboxPlugin.LogInfo($"Total Clip Count: {__instance.musicAudios.Length}");
        }

        private static AudioType GetAudioType(string path)
        {
            var extension = Path.GetExtension(path).ToLower();

            if (extension == ".wav")
                return AudioType.WAV;
            if (extension == ".ogg")
                return AudioType.OGGVORBIS;
            if (extension == ".mp3")
                return AudioType.MPEG;

            return AudioType.UNKNOWN;
        }

        private static bool GetIsOpus(string path)
        {
            var extension = Path.GetExtension(path).ToLower();

            return extension == ".opus";
        }
    }
}
