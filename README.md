# Creating `mod.ini` for Blue Star Manager

This document explains how to create a `mod.ini` file to make your mods compatible with the Blue Star Mod Manager, including adding metadata and creating configuration options.

A `mod.ini` file should be placed in the root directory of your mod.

## 1. Basic `mod.ini` (Metadata)

At its simplest, `mod.ini` provides information about your mod that is displayed in the manager. Every `mod.ini` file should start with a `[Main]` section.

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
-   **Description**: A short description of what your mod does. This is shown when the user selects the mod in the list.

> **Note:** If you don't provide a `mod.ini`, the manager will use the mod's folder name as its name and won't show any other details.

---

## 2. Creating Configuration Options

You can add configuration options that will appear in a "Configure" window for your mod. This is done by adding `[Config:GroupName]` sections. `GroupName` is a unique name for your option group (e.g., "Color", "Character", "Difficulty").

Each config section has the following properties:
-   **Type**: How the options are displayed. Can be `SelectOne` (radio buttons) or `SelectMultiple` (checkboxes).
-   **Description**: A short text displayed above the options.
-   **Options**: A comma-separated list of choices for the user.

### Example 1: `SelectOne` (Radio Buttons)

Use `SelectOne` when the user can only choose one option from a list.

```ini
[Config:Color]
Type = SelectOne
Description = Choose the color for the new car model:
Options = Red, Blue, Green, Black
```

### Example 2: `SelectMultiple` (Checkboxes)

Use `SelectMultiple` when the user can enable multiple options at once.

```ini
[Config:Extra Effects]
Type = SelectMultiple
Description = Select additional visual effects:
Options = Neon Underglow, Sparkling Exhaust, Custom Horn
```

---

## 3. Linking Configuration to Files

To make your configuration options actually do something, you must map them to the files they control in the `[Files]` section.

The format is `FilePath = GroupName.OptionName`.

-   `FilePath` is the path to your mod file, relative to the root of your mod folder.
-   `GroupName.OptionName` is the unique identifier for the option.

When a user selects an option, the manager will enable the corresponding file(s) (e.g., `MyCar_Red.pak`). When an option is deselected, the file(s) will be disabled by renaming them to `.disabled`.

> **Important:** The manager automatically handles all related UE4 pak files (`.pak`, `.ucas`, `.utoc`, `.pak.disabled`, etc.). You only need to specify the base file path once.

**Example:**

```ini
[Files]
; Car color files
Cars/MyCar_Red.pak = Color.Red
Cars/MyCar_Blue.pak = Color.Blue
Cars/MyCar_Green.pak = Color.Green
Cars/MyCar_Black.pak = Color.Black

; Extra effect files
Effects/Neon.pak = Extra Effects.Neon Underglow
Effects/Exhaust.pak = Extra Effects.Sparkling Exhaust
Audio/Horns/CustomHorn.pak = Extra Effects.Custom Horn
```

---

## 4. Complete `mod.ini` Example

Here is a complete `mod.ini` that combines all the concepts above.

```ini
[Main]
Name = Configurable Car
Author = Modder
Version = 2.0
Description = A custom car with selectable colors and effects.

[Config:Color]
Type = SelectOne
Description = Choose the color for the new car model:
Options = Red, Blue, Green

[Config:Extra Effects]
Type = SelectMultiple
Description = Select additional visual effects:
Options = Neon Underglow, Sparkling Exhaust

[Files]
Cars/MyCar_Red.pak = Color.Red
Cars/MyCar_Blue.pak = Color.Blue
Cars/MyCar_Green.pak = Color.Green
Effects/Neon.pak = Extra Effects.Neon Underglow
Effects/Exhaust.pak = Extra Effects.Sparkling Exhaust
```

---

## 5. Text Merging with `mod.json`

For more advanced mods, you can directly edit in-game text by creating a `mod.json` file and placing it in the same directory as your `mod.ini`. This allows you to add or overwrite text entries in the game's localization tables.

The `mod.json` file should contain a JSON array of objects, where each object represents a single text change.

Each object has the following properties:
-   **Language**: The language code for the text (e.g., `"en"` for English).
-   **Namespace**: The localization namespace the text belongs to (e.g., `"DB_CharacterName"`).
-   **Key**: The unique identifier for the text string.
-   **Value**: The new text you want to display.

### Example: Single Text Edit

Here is an example of a `mod.json` that changes a single character's name:
```json
[
    {
        "Language": "en",
        "Namespace": "DB_CharacterName",
        "Key": "chara12001",
        "Value": "RED1"
    }
]
```

### Example: Multiple Text Edits

You can also combine multiple text changes in the same file.

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