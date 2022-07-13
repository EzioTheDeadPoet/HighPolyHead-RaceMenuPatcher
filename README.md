# High Poly Head - RaceMenu Patcher

This Tool patches RaceMenu relevant records to use High Poly Head records instead of their equivalent vanilla Records for character creation, making the default presets use the High Poly Head parts, normally you need to manually select the High Poly Head parts not anymore.
And it disables all their vanilla equivalent parts for character creation to prevent the use of the replaced parts.

To some this might look like a useless thing since "it is only a few clicks", well while that is correct those are clicks you can save yourself (if you love creating characters a lot even more so) or others (if you are making a collection or wabbajack modlist).

## Requirements

- You need to have finished your NPC replacer setup.
    - It can be changed afterwards, but you should sort out all your NPC overwrites and patches before using this.
    - I highly recommend [focustense's EasyNPC Tool](https://www.nexusmods.com/skyrimspecialedition/mods/52313) for managing your NPCs.
- You need to remove all other patches you might have that address the character creation with in regards to High Poly Head.
    - This means patches that remove Vanilla headparts from character creation by tweaking heaparts in the headpart records or the race records. (Using the Vampire fix included with the High Poly Head FOMOD is fine!)
- A working [Synthesis](https://github.com/Mutagen-Modding/Synthesis/wiki/Installation) setup.
- Make sure to run Synthesis through your Mod Manager (in my case Mod Organizer 2).

## Installation

- Download the .synth file from this modpage.
- Launch your Synthesis installation.
    - From your mod manager of choice.
- Double-click the .synth file.

Instructions for this step with a gif are found [here](https://github.com/Mutagen-Modding/Synthesis/wiki/Typical-Usage#using-synth-files) in the official Synthesis Wiki.

## Usage

Well now that the patcher is imported into Synthesis only thing left is to select it (and other patchers you want to run) and to run them.  
Make sure that the generated esp is enabled in your loadorder after closing Synthesis.

## FAQ

### What exactly is this patcher doing ?

- It starts with searching for vanilla versions of High Poly Head (HPH) parts and stores that 1:1 binding.
- While searching it removes the playable flag from every headpart that has a HPH replacement (this flag decides which parts are selectable inside of the RaceMenu).
- But since that isn't enough (race presets still load the vanilla headparts into the RaceMenu) the patcher uses that 1:1 connection to replace the headparts for the races storing the replaced headparts categorized in a searchable way going from "Race -> Gender -> Headpart-list"
- Now the character creation works, but why did we store the replaced headparts ? Well the way headparts for NPCs work is based on object inheritance, so if an NPC is a nord it gets all the records and looks like the default nord, but if you want to change the NPCs looks the NPC record only stores what is different to the template it was build from. Causing and issue with the replaced head parts for the races and the facegen meshes for the NPCs causing the [Dark Face Bug](https://ck.uesp.net/wiki/Dark_Face_Bug). To fix that we now use the data stored in the previous step and go through every NPC and check if they are missing a headpart that was part of their race template and add those missing headparts back to the NPCs.
- And with that we are done.

### Can I look at the code for this ?

Sure, it is licensed under a GPL-3.0 License and can be inspected on [GitHub](https://github.com/EzioTheDeadPoet/HighPolyHead-RaceMenuPatcher).

### Why did you make this patcher ?

- Because I want to improve the quality of modded setups. Especially the ones I share in the form of Wabbajack Modlist similar to many other and collections.
- And because I am a programmer and we love to spent multiple hours on code to save ourself from having to do stuff manually. This has multiple reasons,
    - we recognized a pattern and want to automate it.
    - we are "lazy" and rather spent a lot of time one time in excitement, than maybe slightly less time but more often and more boring.
    - we like a challenge.
    - we started something and got the urge to finish it.

## Special Thanks - Credits

- Thanks to [@JanuarySnow](https://github.com/JanuarySnow) who worked for me on the [1st prototype](https://github.com/JanuarySnow/hph_replacer) of this, based on my back then still lacking knowledge of the NPC headpart problem (mainly because my test setup used a HPH NPC overhaul).
- Thanks to Noggog for creating Mutagen and Synthesis which made this possible at all.
- Thanks to my supporters listed in the [Hall of Fame](https://eziothedeadpoet.github.io/AboutMe/HALLOFFAME.html)
