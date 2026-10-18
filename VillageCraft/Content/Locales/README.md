# Localization authoring

Locales are TOML files loaded once at startup. Copy `en-US.toml`, give the copy a valid culture `Id` and a readable `Name`, then translate the values under `[Text]`.

```toml
Id = "es-ES"
Name = "Español"

[Text]

# Button that opens the game options menu.
"ui.options" = "Opciones"

# People panel counter. {0}: hidden babies waiting to mature.
"people.growing_babies" = "Bebés en crecimiento: {0}"

# Construction notification. {0}: localized building name.
"message.building_placed" = "Se construyó {0}."
```

The left side is a **stable ID**, not the English source sentence. Keep IDs unchanged when editing wording or adding languages. Every translation entry must have a short comment immediately above it explaining where it appears and, for formatted messages, what its arguments mean.

| ID family | Use |
|---|---|
| `ui.*`, `menu.*`, `options.*` | Interface labels, menu actions and settings |
| `people.*`, `villager.*`, `building.*` | Management and inspector text |
| `message.*`, `format.*` | Notifications and formatted display rows |
| `gameover.*`, `statistics.*` | Defeat screen and save statistics |
| `prefab.<id>.name`, `prefab.<id>.description` | Building/road names and descriptions |
| `profession.<id>`, `entity.<id>`, `resource.<id>` | Professions, creatures and resource names |
| `task.<TaskId>` | Villager activity labels |
| `category.<id>`, `activity.<id>`, `effect.<id>` | Build categories, workplace activities and effects |
| `tutorial.<stage>.title`, `tutorial.<stage>.description` | Guided tutorial text |
| `upgrade.<id>` | Equipment upgrade names |

Preserve numbered placeholders such as `{0}`, `{1}` and `{0:0.##}`. You can reorder arguments to suit the language. Numeric values use the selected culture. Use `{{` and `}}` when you need literal braces.

A language can be incomplete: missing IDs fall back to `en-US`. Unknown IDs remain visible, helping authors identify missing entries. Content names with no translation use their TOML fallback names. The loader validates IDs and formatting and reports the locale and key on errors.

In C#, use `L.T("ui.options")` or `L.F("message.building_placed", buildingName)`. In JavaScript, use `world.translate("ui.options")` or `world.format("message.building_placed", [buildingName])`. Script logs accept the resulting string. Do not use translated labels as entity, resource, prefab or script IDs.

Restart after editing locale files. Choose the language under Options; Brazilian Portuguese remains the default.
