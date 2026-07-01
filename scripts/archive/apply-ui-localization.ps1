# Replaces hardcoded UI strings with ModLocalization.T calls (keys must exist in strings.json).
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")

function Apply-Replacements {
    param([string]$RelativePath, [hashtable]$Map)
    $path = Join-Path $root $RelativePath
    if (-not (Test-Path $path)) { Write-Warning "Skip missing $RelativePath"; return }
    $text = Get-Content -Raw -Encoding UTF8 $path
    if ($text -notmatch "using SunhavenMods\.Shared;") {
        $text = $text -replace "(using [^\r\n]+;\r?\n)(\r?\nnamespace )", "`$1using SunhavenMods.Shared;`$2"
        if ($text -notmatch "using SunhavenMods\.Shared;") {
            $text = "using SunhavenMods.Shared;`r`n" + $text
        }
    }
    foreach ($k in $Map.Keys) {
        $text = $text.Replace($k, $Map[$k])
    }
    Set-Content -Path $path -Value $text -Encoding UTF8 -NoNewline
    Write-Host "Patched $RelativePath"
}

# BirthdayReminder
Apply-Replacements "BirthdayReminder\UI\BirthdayHUD.cs" @{
    'GUILayout.Label("No birthdays today", _hintStyle)' = 'GUILayout.Label(ModLocalization.T("birthday.hud.noBirthdays"), _hintStyle)'
    'GUILayout.Button("Gifts", _moreButtonStyle' = 'GUILayout.Button(ModLocalization.T("birthday.hud.gifts"), _moreButtonStyle'
    'GUILayout.Label($"{_selectedNPC.NPCName}''s Gifts", _headerStyle' = 'GUILayout.Label(ModLocalization.T("birthday.gift.title", _selectedNPC.NPCName), _headerStyle'
}

# CropOptimizer
Apply-Replacements "CropOptimizer\UI\CropHudView.cs" @{
    'CreateText(header.transform, "Crop Optimizer", 16f' = 'CreateText(header.transform, ModLocalization.T("crop.hud.title"), 16f'
    '"Tracked crops: <b>{trackedCount}</b>"' = 'ModLocalization.T("crop.hud.tracked", trackedCount)'
    '"Projected sell: <color=#F7D982><b>{projectedGold:N0}g</b></color>"' = 'ModLocalization.T("crop.hud.projected", projectedGold)'
    '"Toggle Crop Tooltips <color=#B8A078>(on)</color>"' = 'ModLocalization.T("crop.hud.tooltipToggle.on")'
    '"Toggle Crop Tooltips <color=#B8A078>(off)</color>"' = 'ModLocalization.T("crop.hud.tooltipToggle.off")'
}

# Senpai Chest (main labels)
Apply-Replacements "SenpaisChest\UI\SmartChestUI.cs" @{
    'GUILayout.Label("Senpai''s Chest Config", _configTitleStyle' = 'GUILayout.Label(ModLocalization.T("chest.config.title"), _configTitleStyle'
    'GUILayout.Label("Item Rules", sectionHeaderStyle)' = 'GUILayout.Label(ModLocalization.T("chest.section.rules"), sectionHeaderStyle)'
    'GUILayout.Label("  No rules configured. Add rules below.",' = 'GUILayout.Label(ModLocalization.T("chest.rules.none"),'
    'GUILayout.Label("Add New Rule", sectionHeaderStyle)' = 'GUILayout.Label(ModLocalization.T("chest.section.addRule"), sectionHeaderStyle)'
    'GUILayout.Label("Presets",' = 'GUILayout.Label(ModLocalization.T("chest.presets"),'
    'GUILayout.Button("Equipment",' = 'GUILayout.Button(ModLocalization.T("chest.preset.equipment"),'
    'GUILayout.Button("Consumables",' = 'GUILayout.Button(ModLocalization.T("chest.preset.consumables"),'
    'GUILayout.Button("Crafting",' = 'GUILayout.Button(ModLocalization.T("chest.preset.crafting"),'
    'GUILayout.Button("Manage Groups",' = 'GUILayout.Button(ModLocalization.T("chest.manageGroups"),'
    'GUILayout.Button("Add Rule",' = 'GUILayout.Button(ModLocalization.T("chest.addRule"),'
    'GUILayout.Button("Remove Smart Chest",' = 'GUILayout.Button(ModLocalization.T("chest.removeSmartChest"),'
    'GUILayout.Button("Close",' = 'GUILayout.Button(ModLocalization.T("chest.close"),'
    'GUILayout.Label("Summary", _chestSectionHeaderStyle' = 'GUILayout.Label(ModLocalization.T("chest.summary"), _chestSectionHeaderStyle'
    'GUILayout.Label("Tips", _chestSectionHeaderStyle' = 'GUILayout.Label(ModLocalization.T("chest.tips.header"), _chestSectionHeaderStyle'
}

# The Vault
Apply-Replacements "TheVault\UI\VaultUI.cs" @{
    'GUILayout.Label("\u2727  The Vault  \u2727", _titleStyle)' = 'GUILayout.Label(ModLocalization.T("vault.title"), _titleStyle)'
    'GUILayout.Label("Currency Storage System", _subtitleStyle)' = 'GUILayout.Label(ModLocalization.T("vault.subtitle"), _subtitleStyle)'
    'GUILayout.Button("Close", _closeButtonStyle' = 'GUILayout.Button(ModLocalization.T("vault.close"), _closeButtonStyle'
    'GUILayout.Button("Withdraw", _buttonStyle' = 'GUILayout.Button(ModLocalization.T("vault.withdraw"), _buttonStyle'
    'GUILayout.Button("Deposit", _buttonStyle' = 'GUILayout.Button(ModLocalization.T("vault.deposit"), _buttonStyle'
}

# Almanac
Apply-Replacements "HavensAlmanac\UI\AlmanacDashboard.cs" @{
    'GUILayout.Label("Haven''s Almanac - Dashboard", _titleStyle)' = 'GUILayout.Label(ModLocalization.T("almanac.dashboard.title"), _titleStyle)'
    'GUILayout.Label("No supported companion mods detected.", _noModsStyle)' = 'GUILayout.Label(ModLocalization.T("almanac.dashboard.noMods"), _noModsStyle)'
}
Apply-Replacements "HavensAlmanac\UI\DailyBriefing.cs" @{
    'GUILayout.Label("Good Morning!", _titleStyle)' = 'GUILayout.Label(ModLocalization.T("almanac.briefing.title"), _titleStyle)'
    'GUILayout.Label("Nothing noteworthy today. Have a great day!", _contentStyle)' = 'GUILayout.Label(ModLocalization.T("almanac.briefing.empty"), _contentStyle)'
    'GUILayout.Button("Dismiss", _dismissButtonStyle' = 'GUILayout.Button(ModLocalization.T("almanac.briefing.dismiss"), _dismissButtonStyle'
}
Apply-Replacements "HavensAlmanac\UI\AlmanacHUD.cs" @{
    'GUILayout.Label("Haven''s Almanac", _titleStyle)' = 'GUILayout.Label(ModLocalization.T("almanac.hud.title"), _titleStyle)'
}

# SMUT
Apply-Replacements "SunHavenMuseumUtilityTracker\UI\MuseumTrackerUI.cs" @{
    'GUILayout.Label("S.M.U.T.", _titleStyle)' = 'GUILayout.Label(ModLocalization.T("smut.title"), _titleStyle)'
    'GUILayout.Label("Sun Haven Museum Utility Tracker", _subtitleStyle)' = 'GUILayout.Label(ModLocalization.T("smut.subtitle"), _subtitleStyle)'
    'GUILayout.Label("Search", _searchLabelStyle' = 'GUILayout.Label(ModLocalization.T("smut.search"), _searchLabelStyle'
    'GUILayout.Button("Clear", _buttonStyle' = 'GUILayout.Button(ModLocalization.T("smut.clear"), _buttonStyle'
    'GUILayout.Button("Sync with Game", _syncButtonStyle' = 'GUILayout.Button(ModLocalization.T("smut.sync"), _syncButtonStyle'
    'GUILayout.Label("Donated", _checkStyleCached' = 'GUILayout.Label(ModLocalization.T("smut.item.donated"), _checkStyleCached'
    'GUILayout.Label("Needed", _neededStyleCached' = 'GUILayout.Label(ModLocalization.T("smut.item.needed"), _neededStyleCached'
}

# Haven Dev Tools (header + close)
Apply-Replacements "HavenDevTools\UI\DebugWindow.cs" @{
    'GUILayout.Label("Haven Dev Tools", _headerStyle)' = 'GUILayout.Label(ModLocalization.T("devtools.title"), _headerStyle)'
    'GUILayout.Button("Close (Esc)", _buttonStyle)' = 'GUILayout.Button(ModLocalization.T("devtools.close"), _buttonStyle)'
}
Apply-Replacements "HavenDevTools\UI\DebugOverlay.cs" @{
    'GUILayout.Label("Haven Dev Tools", _valueStyle)' = 'GUILayout.Label(ModLocalization.T("devtools.title"), _valueStyle)'
    'GUILayout.Label($"F11: Window | F6: Toggle", _labelStyle)' = 'GUILayout.Label(ModLocalization.T("devtools.overlay.keys"), _labelStyle)'
}

Write-Host "UI localization patches applied."
