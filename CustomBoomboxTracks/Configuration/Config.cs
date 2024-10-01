using System.IO;
using BepInEx;
using BepInEx.Configuration;
using CustomBoomboxTracks.Managers;

namespace CustomBoomboxTracks.Configuration
{
    internal static class Config
    {
        private const string CONFIG_FILE_NAME = "boombox.cfg";

        private static ConfigFile _config;
        private static ConfigEntry<bool> _useDefaultSongs;
        private static ConfigEntry<bool> _streamAudioFromDisk;
        private static ConfigEntry<int> _opusDecodeSampleRate;
        private static ConfigEntry<bool> _useCustomSongsFolder;
        private static ConfigEntry<bool> _useConfigFolder;
        private static ConfigEntry<bool> _searchForCustomSongs;
        private static ConfigEntry<bool> _deleteCustomSongsFolder;

        public static void Init()
        {
            BoomboxPlugin.LogInfo("Initializing config...");
            var filePath = Path.Combine(Paths.ConfigPath, CONFIG_FILE_NAME);
            _config = new ConfigFile(filePath, true);
            _useDefaultSongs = _config.Bind("Config", "Use Default Songs", false, "Include the default songs in the rotation.");
            _streamAudioFromDisk = _config.Bind("Config", "Stream Audio From Disk", false, "(Not possible for Opus) Requires less memory and takes less time to load, but prevents playing the same song twice at once.");
            _opusDecodeSampleRate = _config.Bind("Config", "Opus Decoding Sample Rate", 16000, "Higher values provide better quality, but use more memory. I think the supported values are 8000, 16000, 32000, and 48000 (not 41800). I've set the default to 16000 because slightly lower quality is funny.");
            _useCustomSongsFolder = _config.Bind("Config", "Use Custom Songs Folder", true, "Pull songs from the `Custom Songs/Boombox Music` folder. I don't think anyone ever pulls songs from here.");
            _useConfigFolder = _config.Bind("Config", "Use Config Folder", false, "Pull songs from the `Config/Custom Songs` folder. In case you share your songs via Thunderstore codes.");
            _searchForCustomSongs = _config.Bind("Config", "Search For Custom Songs", true, "Searches and pulls for songs from any downloaded Plugins or other Mods (in their Custom Songs folders). So you don't need any \"Fix\" mods.");
            _deleteCustomSongsFolder = _config.Bind("Config", "Delete Custom Songs Folder", false, "A lot of \"Fix\" mods just copy songs to the right spot, doubling the amount of space used. This option will clean that up for you. False by default to prevent accidental unwanted behaviour.");
            BoomboxPlugin.LogInfo("Config initialized!");
        }

        private static void PrintConfig()
        {
            BoomboxPlugin.LogInfo($"Use Default Songs: {_useDefaultSongs.Value}");
            BoomboxPlugin.LogInfo($"Stream From Disk: {_streamAudioFromDisk}");
            BoomboxPlugin.LogInfo($"Opus Decoding Sample Rate: {_opusDecodeSampleRate}");
            BoomboxPlugin.LogInfo($"Use Custom Songs Folder: {_useCustomSongsFolder}");
            BoomboxPlugin.LogInfo($"Use Config Folder: {_useConfigFolder}");
            BoomboxPlugin.LogInfo($"Search For Custom Songs: {_searchForCustomSongs}");
            BoomboxPlugin.LogInfo($"Delete Custom Songs Folder: {_deleteCustomSongsFolder}");
        }

        public static bool UseDefaultSongs => _useDefaultSongs.Value || AudioManager.HasNoSongs;
        public static bool StreamFromDisk => _streamAudioFromDisk.Value;
        public static int OpusDecodeSampleRate => _opusDecodeSampleRate.Value;
        public static bool UseCustomSongsFolder => _useCustomSongsFolder.Value;
        public static bool UseConfigFolder => _useConfigFolder.Value;
        public static bool SearchForCustomSongs => _searchForCustomSongs.Value;
        public static bool DeleteCustomSongsFolder => _deleteCustomSongsFolder.Value;
    }
}
