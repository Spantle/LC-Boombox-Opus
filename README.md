# LC-Boombox / Custom Boombox Music (Now with Opus support!)
Find it on [Thunderstore](https://thunderstore.io/c/lethal-company/p/Spantle/Custom_Boombox_Music_Opus/)

A fork of [this mod by Steven](https://thunderstore.io/c/lethal-company/p/Steven/Custom_Boombox_Music/) that adds support for [Opus](https://opus-codec.org/) (.opus) files.

It also adds native support for loading music files from Mods/Plugins and the Config folder so [any fixer mods like this one](https://thunderstore.io/c/lethal-company/p/CodeEnder/Custom_Boombox_Fix/) are no longer needed.

## What are Opus files?
Opus is a relatively new audio format (think MP3, WAV, OGG, etc.) that is designed to be very efficient and high quality. Many popular apps already quietly use it (like Discord), but Unity doesn't support it out of the box. This mod manually adds support for it when loading custom Boombox music.

Some benefits of using Opus files (and this mod) are:
- Significantly better quality at smaller file sizes (even against MP3s)
- Uses significantly less RAM/memory
- Music no longer loops/ends early
- "Fix" mods that work by copying files (doubling storage usage) are no longer needed
- IDK I kind of just made this for my own music/modpack lol

## Installing Songs
This mod supports loading songs from the following locations (double check the config)
- `Custom Songs/Boombox Music`
- `plugins/*/Custom Songs`
- `config/Custom Songs`

It also adds an option to delete songs from `Custom Songs/Boombox Music`, just in case you used a fixer mod before (off by default).

## Credits
- This mod is mostly based off of [the Custom Boombox Music by Steven](https://thunderstore.io/c/lethal-company/p/Steven/Custom_Boombox_Music/)
- [Opus](https://opus-codec.org/) is a fantastic free and open source audio codec that is used in this mod (and should be used by many others)
- [Concentus](https://github.com/lostromb/concentus) and [Concentus.OggFile](https://github.com/lostromb/concentus.oggfile) for their C# Opus decoder port, and also mainly for their Ogg file decoder.