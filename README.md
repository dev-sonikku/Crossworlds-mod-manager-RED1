# Blue Star: The CrossWorlds Mod Manager

**Blue Star** is a powerful mod manager for *Team Sonic Racing* on PC, designed to work seamlessly with the **CrossWorlds** overhaul mod. It provides an easy-to-use interface for players to install and manage their mods, and offers robust features for mod creators to build configurable and advanced modifications.

## Features

- **Easy Mod Management**: Simple interface to enable and disable mods.
- **Mod Configuration**: In-app UI for configuring mods that have options (e.g., character skins, color variants, gameplay tweaks).
- **GameBanana Integration**: Browse and install mods directly from GameBanana without leaving the manager.
- **Advanced Mod Support**:
    - **File Patching**: Automatically handles `.pak`, `.ucas`, and `.utoc` files.
    - **Text Merging**: Allows mods to modify in-game text via a simple `mod.json` file.
- **Automatic Updates**: Keeps your mods and the manager itself up-to-date.

---

## For Players: How to Use

1. **Download**: Grab the latest release of Blue Star Manager.
2. **Setup**: Place the manager anywhere and run it. It will automatically detect your *Team Sonic Racing* installation (Steam version required). If it can't find it, you can set the game path manually in the settings.
3. **Install Mods**:
    - Use the "GameBanana" tab to browse and install mods with one click.
    - Or, manually download a mod and drag-and-drop its `.zip` or folder into the manager.
4. **Enable/Disable**: Click the checkbox next to a mod to enable or disable it.
5. **Configure Mods**: If a mod has a "Configure" button, you can click it to open a window with different options for that mod.
6. **Play**: Click "Launch Game" to play with your selected mods!

---

## For Mod Creators: Creating Compatible Mods

To make your mods compatible with Blue Star Manager, you need to include a `mod.ini` file in the root of your mod's folder. This file contains metadata and defines configuration options.

### 1. Basic `mod.ini` (Metadata)

The `[Main]` section provides information about your mod that is displayed in the manager.

**Example:**
```ini
; This is a comment
[Main]
Name = My Awesome Mod
Author = Your Name
Version = 1.1
Description = This mod replaces the main character's car with a cool new model.
```

-   **Name**: The display name of your mod.
-   **Author**: Your name or alias.
-   **Version**: The version number of your mod.
-   **Description**: A short description of what your mod does.

> **Note:** If you don't provide a `mod.ini`, the manager will use the mod's folder name as its name and won't show any other details.

### 2. Creating Configuration Options

You can add configuration options that will appear in a "Configure" window for your mod using `[Config:GroupName]` sections. `GroupName` is a unique name for your option group (e.g., "Color", "Character").

-   **Type**: `SelectOne` (radio buttons) or `SelectMultiple` (checkboxes).
-   **Description**: Text displayed above the options.
-   **Options**: A comma-separated list of choices.

**Example: `SelectOne` (Radio Buttons)**
```ini
[Config:Color]
Type = SelectOne
Description = Choose the color for the new car model:
Options = Red, Blue, Green, Black
```

**Example: `SelectMultiple` (Checkboxes)**
```ini
[Config:Extra Effects]
Type = SelectMultiple
Description = Select additional visual effects:
Options = Neon Underglow, Sparkling Exhaust, Custom Horn
```

### 3. Linking Configuration to Files

The `[Files]` section maps your configuration options to the files they control. The format is `FilePath = GroupName.OptionName`.

-   `FilePath` is the relative path to your mod file.
-   `GroupName.OptionName` is the unique identifier for the option.

> **Important:** The manager automatically handles all related UE4 pak files (`.pak`, `.ucas`, `.utoc`, `.pak.disabled`, etc.). You only need to specify the base file path once.

**Example:**
```ini
[Files]
; Car color files
Cars/MyCar_Red.pak = Color.Red
Cars/MyCar_Blue.pak = Color.Blue

; Extra effect files
Effects/Neon.pak = Extra Effects.Neon Underglow
Audio/Horns/CustomHorn.pak = Extra Effects.Custom Horn
```

### 4. Text Merging with `mod.json`

For advanced mods, you can directly edit in-game text by creating a `mod.json` file in the same directory as your `mod.ini`. This allows you to add or overwrite text entries in the game's localization tables.

The `mod.json` file contains a JSON array of objects, where each object represents a single text change:
-   **Language**: The language code (e.g., `"en"`).
-   **Namespace**: The localization namespace (e.g., `"DB_CharacterName"`).
-   **Key**: The unique identifier for the text string.
-   **Value**: The new text you want to display.

**Example `mod.json`:**
```json
[
    {
        "Language": "en",
        "Namespace": "DB_CharacterName",
        "Key": "chara12001",
        "Value": "RED1"
    },
    {
        "Language": "en",
        "Namespace": "DB_RaceItem",
        "Key": "raceitem_name_006",
        "Value": "Teleport Ring"
    }
]
```
