# Pantophobia Repo Code Style Guide

This document explains the conventions and tools used for writing **Pantophobia’s** C# code in Unity. Keeping a consistent code style makes the repo easier to maintain and navigate for everyone.

---

## Recommended IDE & Extensions

We recommend **Visual Studio Code** for Pantophobia development.

These extensions will give you a similar experience to the original Haxe setup:

* **C# Dev Kit** (`ms-dotnettools.csdevkit`) → Main C# development support.
* **Roslynator** (`josefpihrt-vscode.roslynator`) → Advanced C# refactoring, code analysis, and formatting suggestions.
* **C# XML Documentation Comments** (`k--kato.docomment`) → Easy generation of XML doc comments.
* **Unity Tools** (`Tobiah.unity-tools`) → Helpful Unity-specific code navigation.
* **EditorConfig for VS Code** (`EditorConfig.EditorConfig`) → Uses `.editorconfig` to enforce style rules.

> **Note:** Instead of `hxformat.json`, we’ll use `.editorconfig` and **C# Dev Kit** settings to auto-format code. Enable **Format on Save** in VS Code (`editor.formatOnSave: true`).

---

## Whitespace & Indentation

* Use **tabs** for indentation (1 tab = 4 spaces in VS Code settings).
* No trailing whitespace at the end of lines.
* Add a single blank line between method definitions and before return statements for clarity.
* Use braces `{}` for all control blocks, even single-line statements.


## Variable & Method Names

* Use **lowerCamelCase** for variables and private methods.

  ```csharp
int currentScore;
  string playerName;
  void resetLevel() { ... }
```

* Use **UpperCamelCase** (PascalCase) for public methods, properties, and class names.

  ```csharp
public class SongManager { ... }
  public void LoadSong(string name) { ... }
```

* Descriptive names are preferred over short ones — clarity over brevity.


## Naming: "Phobia" Prefix for Core Classes Only

Only **core classes** should use the `Phobia` prefix in their names (e.g., `PhobiaSave`, `PhobiaCamera`).

- **Do not** use the `Phobia` prefix for non-core, derived, or feature-specific classes. For example, a custom controls class that extends `PhobiaSave` should **not** be named `PhobiaControls`.
- Reserve the `Phobia` prefix for foundational/core systems only.

This helps keep the codebase organized and makes it clear which classes are part of the core Pantophobia architecture.

## Code Comments

Use **XML documentation comments** for all **public** methods, properties, and classes.

Example:

```csharp
/// <summary>
/// Finds the largest deviation from the target time in this VoicesGroup.
/// </summary>
/// <param name="targetTime">
/// The time to check against. If null, checks against the first member.
/// </param>
/// <returns>
/// The largest deviation from the target time found.
/// </returns>
public float CheckSyncError(float? targetTime)
{
    ...
}
```

For **internal notes** or explaining complex logic, use `//` single-line comments sparingly.

---

## Documentation .md files

For all CORE utility classes (PhobiaSound, PhobiaSprite, PhobiaModel, etc), you should include a {utilityname}.md file that has focused documentation on the utility for new coders of the team.

---

## Unused Code

Do **not** leave large commented-out code blocks in files.
Old code can always be retrieved from Git history. Removing unused chunks keeps files shorter and easier to read.

---

## License Headers

No per-file license headers — our main `LICENSE.md` applies to the entire repository.

---

## Imports

Place `using` statements at the top of the file in a single group, sorted alphabetically.
Exception: Conditional `#if UNITY_EDITOR` imports should be grouped at the bottom, also alphabetized.

Example:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
```

---

## Argument Formatting

In C#, **optional parameters** (`= defaultValue`) should not be combined with nullable types unless intentional.

✅ Good:

```csharp
public void PlaySound(string name = "default");
public void SetVolume(float? volume);
```

🚫 Avoid:

```csharp
public void SetVolume(float? volume = 1.0f); // Redundant default
```

---

## Extra Unity-Specific Notes

* Avoid relying on Inspector-assigned references unless necessary. Prefer **`GetComponent<T>()`**, **`Resources.Load<T>()`**, or programmatic instantiation.
* Keep **MonoBehaviour** scripts lean — heavy logic should live in non-MonoBehaviour classes.
* Organize scripts into folders by role (e.g., `/Gameplay/`, `/Audio/`, `/UI/`).

---

## `.editorconfig` Example

You can drop this in the root of the repo to enforce most style rules automatically:

```ini
root = true

[*.cs]
indent_style = tab
indent_size = 4
trim_trailing_whitespace = true
insert_final_newline = true
charset = utf-8

# C# code style
csharp_style_var_for_built_in_types = true:suggestion
csharp_style_var_when_type_is_apparent = true:suggestion
csharp_style_var_elsewhere = true:suggestion

# Braces
csharp_prefer_braces = true:warning
```