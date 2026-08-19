/** Hub catalog presentation. Versions come from versions.json at runtime. */

export const HUB_MODS = [
  {
    id: "the-vault",
    jsonKey: "com.azraelgodking.thevault",
    name: "The Vault",
    tagline: "Secure Currency Storage",
    description:
      "Automatically stores seasonal tokens, community tokens, and keys in an encrypted vault. Seamless integration with shops and doors — your currencies are always available when you need them.",
    features: ["Auto-Deposit", "Per-Character Saves", "Encrypted Storage", "Shop Integration"],
    tags: ["QoL", "Storage"],
    status: "Stable",
    accent: "#3b82f6",
    icon: "TheVault/icon.png",
    href: "TheVault/TheVault.html",
  },
  {
    id: "havens-birthright",
    jsonKey: "com.azraelgodking.havensbirthright",
    name: "Haven's Birthright",
    tagline: "Racial Bonuses System",
    description:
      "Adds unique racial bonuses and abilities to Sun Haven. Each of the 12 races gains distinct gameplay advantages based on their heritage, making your character choice more meaningful.",
    features: ["12 Unique Races", "Configurable Bonuses", "Balanced Gameplay", "In-Game Settings"],
    tags: ["QoL"],
    status: "Actively Maintained",
    accent: "#dc2626",
    icon: "RacialBonuses/icon.png",
    href: "RacialBonuses/RacialBonuses.html",
  },
  {
    id: "faster-races",
    jsonKey: "com.azraelgodking.fasterraces",
    name: "Faster Races",
    tagline: "Configurable Movement Speed",
    description:
      "Adds a configurable movement speed bonus for all races. When used with Haven's Birthright, disables HB's speed buffs so you get one consistent bonus — no double speed.",
    features: ["Configurable %", "HB Compatible", "All Races", "Quick Mod"],
    tags: ["QoL"],
    status: "Stable",
    accent: "#f59e0b",
    icon: "FasterRaces/icon.png",
    href: "FasterRaces/FasterRaces.html",
  },
  {
    id: "trinket-fortune",
    jsonKey: "com.azraelgodking.trinketfortune",
    name: "Trinket Fortune",
    tagline: "Fishing Trinket Bias",
    description:
      "Increases odds of unowned fishing trinkets dropping as you complete the aquarium. The sea rewards the patient — that last relic drifts closer with each cast.",
    features: ["Museum-Aware", "Configurable", "S.M.U.T. Compatible", "HavenDevTools Panel"],
    tags: ["QoL", "Tracking"],
    status: "Stable",
    accent: "#0ea5e9",
    icon: "TrinketFortune/icon.png",
    href: "TrinketFortune/TrinketFortune.html",
  },
  {
    id: "museum-utility-tracker",
    jsonKey: "com.azraelgodking.sunhavenmuseumutilitytracker",
    name: "S.M.U.T.",
    tagline: "Museum Donation Tracker",
    description:
      "Track your museum donation progress across all three sections: Hall of Gems, Hall of Culture, and Aquarium. Never forget which items you've already donated or still need to find.",
    features: ["Donation Tracking", "Per-Character Saves", "Progress Stats", "Item Icons"],
    tags: ["Tracking", "UI"],
    status: "Actively Maintained",
    accent: "#b45309",
    icon: "SMUT/icon.png",
    href: "SMUT/SMUT.html",
  },
  {
    id: "havens-todo",
    jsonKey: "com.azraelgodking.sunhaventodo",
    name: "Sun Haven Todo",
    tagline: "In-Game Task Tracker",
    description:
      "Keep track of your farming goals, quests, and daily tasks with a beautiful parchment-themed todo list. Other mods can auto-create and auto-complete todos for birthdays, museum donations, and more.",
    features: ["Task Management", "Per-Character Saves", "Categories", "Cross-Mod Todos"],
    tags: ["QoL", "Tracking", "UI"],
    status: "Stable",
    accent: "#16a34a",
    icon: "Todo/icon.png",
    href: "Todo/todo.html",
  },
  {
    id: "squirrels-birthday-reminder",
    jsonKey: "com.azraelgodking.squirrelsbirthdayreminder",
    name: "A Squirrel's Birthday Reminder",
    tagline: "Never Miss a Birthday",
    description:
      "Automatically reminds you when NPCs have birthdays. Shows gift suggestions for loved and liked items, tracks who you've already gifted. With Todo installed, birthday gifts auto-create and auto-complete todos.",
    features: ["Auto Reminders", "Gift Suggestions", "Gift Tracking", "Todo Integration"],
    tags: ["Social", "QoL"],
    status: "Stable",
    accent: "#ec4899",
    icon: "BirthdayReminder/icon.png",
    href: "BirthdayReminder/BirthdayReminder.html",
  },
  {
    id: "gifting-assistant",
    jsonKey: "com.azraelgodking.giftingassistant",
    name: "Gifting Assistant",
    tagline: "Daily Gift Roster",
    description:
      "A per-character daily gift roster with loved/liked suggestions, item icons with bag counts, priorities, and gifted-today tracking. Soft-integrates with Sun Haven Todo, Birthday Reminder, and Haven's Almanac.",
    features: ["Daily Roster", "Loved/Liked Icons", "Todo Sync", "Almanac Progress"],
    tags: ["Social", "QoL"],
    status: "New",
    accent: "#e11d48",
    icon: "GiftingAssistant/icon.png",
    href: "GiftingAssistant/GiftingAssistant.html",
  },
  {
    id: "senpais-chest",
    jsonKey: "com.azraelgodking.senpaischest",
    name: "Senpai's Chest",
    tagline: "Smart Chest Sorting",
    description:
      "Rule-driven chest sorting with categories, groups, and museum-aware exclusions when S.M.U.T. is installed. Keep loot flowing into the right places without the busywork.",
    features: ["Smart Rules", "Category Groups", "Museum-Aware", "Controller Support"],
    tags: ["QoL", "Storage"],
    status: "Stable",
    accent: "#d97706",
    icon: "SenpaisChest/icon.png",
    href: "SenpaisChest/SenpaisChest.html",
  },
  {
    id: "havens-almanac",
    jsonKey: "com.azraelgodking.havensalmanac",
    name: "Haven's Almanac",
    tagline: "Unified Dashboard & Briefing",
    description:
      "A morning briefing and HUD that aggregates signals across your suite — todos, birthdays, crops, vault, and more — so you start each day knowing what matters.",
    features: ["Morning Briefing", "HUD Overlay", "Cross-Mod Signals", "Mod Health"],
    tags: ["QoL", "UI", "Tracking"],
    status: "Stable",
    accent: "#0284c7",
    icon: "HavensAlmanac/icon.png",
    href: "HavensAlmanac/HavensAlmanac.html",
  },
  {
    id: "crop-optimizer",
    jsonKey: "com.azraelgodking.cropoptimizer",
    name: "Crop Optimizer",
    tagline: "Farm Forecast HUD & Hover Tooltip",
    description:
      "A draggable HUD that forecasts every crop in the ground, plus a hover tooltip showing quality, growth ETA, water / fertilizer state, projected gold, and tile coords. Soft-integrates with Todo, Birthday Reminder, The Vault, and the Almanac.",
    features: ["Crop HUD", "Hover Tooltip", "Quality Tiers", "Projected Gold"],
    tags: ["QoL", "UI", "Tracking"],
    status: "Stable",
    accent: "#65a30d",
    icon: "CropOptimizer/icon.png",
    href: "CropOptimizer/CropOptimizer.html",
  },
  {
    id: "havens-respec",
    jsonKey: "com.azraelgodking.havensrespec",
    name: "Haven's Respec",
    tagline: "Safe Skill Tree Respec",
    description:
      "Reset any profession's skill tree with a confirmation dialog, one-step Undo, and optional gold/gem cost. Clean-room and tree-driven — no hardcoded node names, so it survives Sun Haven updates.",
    features: ["Per-Tab Reset", "Undo", "Configurable Cost", "Hotkeys"],
    tags: ["QoL", "UI", "Skills"],
    status: "Stable",
    accent: "#7c3aed",
    icon: "HavensRespec/icon.png",
    href: "HavensRespec/HavensRespec.html",
  },
  {
    id: "haven-dev-tools",
    jsonKey: "com.azraelgodking.havendevtools",
    name: "HavenDevTools",
    tagline: "Developer & Debug Utilities",
    description:
      "A comprehensive development toolkit for mod developers. Real-time game state inspection, advanced logging, debug overlays, and utilities for testing and troubleshooting.",
    features: ["State Inspector", "Debug Console", "Performance Monitor", "Hot Reload"],
    tags: ["Dev"],
    status: "Stable",
    accent: "#64748b",
    icon: "HavenDevTools/icon.png",
    href: "HavenDevTools/HavenDevTools.html",
  },
];

export const FILTER_TAGS = ["all", "QoL", "Storage", "UI", "Social", "Tracking", "Skills", "Dev"];

export const STARTER_PACKS = [
  {
    id: "curator",
    name: "The Curator",
    icon: "🏛️",
    accent: "#0b3d4a",
    desc: "Track, sort, and store your museum collectibles",
    mods: [
      { label: "S.M.U.T.", href: "SMUT/SMUT.html" },
      { label: "Senpai's Chest", href: "SenpaisChest/SenpaisChest.html" },
      { label: "The Vault", href: "TheVault/TheVault.html" },
    ],
  },
  {
    id: "socialite",
    name: "The Socialite",
    icon: "💕",
    accent: "#c45c3e",
    desc: "Build relationships and never miss a birthday or daily gift",
    mods: [
      { label: "Birthday Reminder", href: "BirthdayReminder/BirthdayReminder.html" },
      { label: "Gifting Assistant", href: "GiftingAssistant/GiftingAssistant.html" },
      { label: "Haven's Birthright", href: "RacialBonuses/RacialBonuses.html" },
    ],
  },
  {
    id: "homesteader",
    name: "The Homesteader",
    icon: "🌾",
    accent: "#3d6b4f",
    desc: "Stay on track with tasks, crops, and sorted storage",
    mods: [
      { label: "Sun Haven Todo", href: "Todo/todo.html" },
      { label: "Crop Optimizer", href: "CropOptimizer/CropOptimizer.html" },
      { label: "Senpai's Chest", href: "SenpaisChest/SenpaisChest.html" },
    ],
  },
  {
    id: "full",
    name: "The Full Suite",
    icon: "⭐",
    accent: "#e8a317",
    wide: true,
    desc: "All twelve gameplay mods — vault, races, museum, storage, tasks, social, gifting, fishing luck, crop planning, skill respec, and the Almanac dashboard. Add HavenDevTools if you build mods yourself.",
    mods: [
      { label: "Almanac", href: "HavensAlmanac/HavensAlmanac.html" },
      { label: "The Vault", href: "TheVault/TheVault.html" },
      { label: "Birthright", href: "RacialBonuses/RacialBonuses.html" },
      { label: "Faster Races", href: "FasterRaces/FasterRaces.html" },
      { label: "Trinket Fortune", href: "TrinketFortune/TrinketFortune.html" },
      { label: "Crop Optimizer", href: "CropOptimizer/CropOptimizer.html" },
      { label: "Respec", href: "HavensRespec/HavensRespec.html" },
      { label: "S.M.U.T.", href: "SMUT/SMUT.html" },
      { label: "Senpai's Chest", href: "SenpaisChest/SenpaisChest.html" },
      { label: "Todo", href: "Todo/todo.html" },
      { label: "Birthday", href: "BirthdayReminder/BirthdayReminder.html" },
      { label: "Gifting", href: "GiftingAssistant/GiftingAssistant.html" },
    ],
  },
];

export const FAQ_ITEMS = [
  {
    q: "A mod didn't load after installing?",
    a: "Make sure BepInEx 5.x is installed correctly and the mod DLL sits in <code>BepInEx/plugins/</code>. Check <code>BepInEx/LogOutput.log</code> for errors, and confirm you're on a compatible game version.",
  },
  {
    q: "Where are the config files?",
    a: "In <code>BepInEx/config/</code> inside your Sun Haven game folder — one <code>.cfg</code> per mod. Edit them with any text editor while the game is closed, or use in-game settings where a mod supports them.",
  },
  {
    q: "Can I run several mods at once?",
    aHtml: true,
    a: `<p>Yes — all mods here are designed to work together with no conflicts, and several form powerful combinations:</p>
<ul>
<li><strong>Senpai's Chest + S.M.U.T.</strong> — auto-excludes already-donated museum items from sorting</li>
<li><strong>Birthday Reminder + Todo</strong> — birthday gift reminders auto-create todos that complete when you give the gift</li>
<li><strong>Gifting Assistant + Todo</strong> — the daily roster pushes “Gift {NPC} today” tasks that auto-complete when you gift</li>
<li><strong>Gifting Assistant + Birthday Reminder + Almanac</strong> — birthday NPCs surface in the roster; progress appears on the Almanac dashboard</li>
<li><strong>Senpai's Chest + S.M.U.T. + Todo</strong> — undonated museum items found in chests auto-create reminders that complete on donation</li>
</ul>
<p>Every combination is optional — each mod works perfectly on its own.</p>`,
  },
  {
    q: "How do I update a mod?",
    a: "Replace the old DLL in <code>BepInEx/plugins/</code> with the new version — config settings and save data are preserved automatically. With r2modman or Thunderstore, updates are handled for you.",
  },
  {
    q: "My settings changed after upgrading?",
    a: "If a mod adds new config options, BepInEx may regenerate the config file. Existing settings are preserved, but new options start at their defaults — check the <code>.cfg</code> file to verify your preferences.",
  },
];

export const COMPARE_HEADERS = [
  "Capability",
  "Vault",
  "S.M.U.T.",
  "Chest",
  "Todo",
  "Birthday",
  "Gifting",
  "Birthright",
  "Faster",
  "Trinket",
  "Crop",
  "Respec",
  "Almanac",
  "DevTools",
];

/** Each row: [capability, ...13 cells] where true=Yes, false=— */
export const COMPARE_ROWS = [
  ["Per-Character Saves", true, true, true, true, true, true, true, true, true, true, true, true, false],
  ["In-Game UI", true, true, true, true, true, true, false, false, false, true, true, true, true],
  ["Configurable Settings", true, true, true, true, true, true, true, true, true, true, true, true, true],
  ["Keyboard Shortcuts", true, true, true, true, true, true, false, false, false, true, true, true, true],
  ["Controller Support", false, false, true, false, false, false, false, false, false, false, false, false, false],
  ["Auto Storage/Sorting", true, false, true, false, false, false, false, false, false, false, false, false, false],
  ["Progress Tracking", false, true, false, true, true, true, false, false, true, true, false, true, false],
  ["Cross-Mod Synergy", false, true, true, true, true, true, false, true, true, true, false, true, false],
];

export const INSTALL_STEPS = [
  {
    title: "Install BepInEx",
    html: `Download <a href="https://github.com/BepInEx/BepInEx/releases" target="_blank" rel="noopener noreferrer">BepInEx 5.x</a> for Unity games and unzip it into your Sun Haven folder. Launch the game once so it generates its folders.`,
  },
  {
    title: "Download the mods",
    html: `Grab the latest releases from <a href="https://thunderstore.io/c/sun-haven/p/AzraelGodKing/" target="_blank" rel="noopener noreferrer">Thunderstore</a> or <a href="https://www.nexusmods.com/profile/AzraelGodKing/mods" target="_blank" rel="noopener noreferrer">Nexus Mods</a> — or use a mod manager like r2modman.`,
  },
  {
    title: "Drop in the DLLs",
    html: `Place each mod's DLL (and any packaged files) into <code>BepInEx/plugins/</code>. Subfolders are fine.`,
  },
  {
    title: "Play",
    html: `Launch Sun Haven. Mods activate automatically — check each mod's page for hotkeys and config options.`,
  },
];
