/** Ported from legacy cloudflare-site scripts. Keep STATS_ID_BY_MOD_KEY aligned with scripts/stats/fetch-stats.js */

export const STATS_ID_BY_MOD_KEY = {
  senpaischest: "senpais-chest",
  havensbirthright: "havens-birthright",
  sunhavenmuseumutilitytracker: "museum-utility-tracker",
  squirrelsbirthdayreminder: "squirrels-birthday-reminder",
  sunhaventodo: "havens-todo",
  thevault: "the-vault",
  havendevtools: "haven-dev-tools",
  havensalmanac: "havens-almanac",
  fasterraces: "faster-races",
  trinketfortune: "trinket-fortune",
  cropoptimizer: "crop-optimizer",
  havensrespec: "havens-respec",
  giftingassistant: "gifting-assistant",
};

export const MOD_META = {
  senpaischest: { icon: "📦", lane: "Storage" },
  havensbirthright: { icon: "⚔️", lane: "Races" },
  sunhavenmuseumutilitytracker: { icon: "🏛️", lane: "Tracking" },
  squirrelsbirthdayreminder: { icon: "🎂", lane: "Social" },
  sunhaventodo: { icon: "📝", lane: "Planning" },
  thevault: { icon: "🔒", lane: "Currency" },
  havendevtools: { icon: "🛠️", lane: "Dev" },
  havensalmanac: { icon: "📖", lane: "Dashboard" },
  fasterraces: { icon: "🏃", lane: "Movement" },
  trinketfortune: { icon: "🎣", lane: "Fishing" },
  cropoptimizer: { icon: "🌱", lane: "Farming" },
  havensrespec: { icon: "📜", lane: "Skills" },
  giftingassistant: { icon: "🎁", lane: "Social" },
};

export const MOD_PRESENTATION = {
  senpaischest: {
    icon: "📦",
    status: "Stable",
    tags: ["QoL", "Storage", "Automation"],
    related: ["sunhavenmuseumutilitytracker", "sunhaventodo", "thevault"],
  },
  havensbirthright: {
    icon: "⚔️",
    status: "Actively Maintained",
    tags: ["QoL", "Combat", "Races"],
    related: ["fasterraces", "havensrespec", "havensalmanac"],
  },
  sunhavenmuseumutilitytracker: {
    icon: "🏛️",
    status: "Actively Maintained",
    tags: ["Tracking", "Museum", "UI"],
    related: ["senpaischest", "trinketfortune", "havensalmanac"],
  },
  squirrelsbirthdayreminder: {
    icon: "🎂",
    status: "Stable",
    tags: ["Social", "Reminder", "QoL"],
    related: ["sunhaventodo", "havensalmanac", "senpaischest"],
  },
  sunhaventodo: {
    icon: "📝",
    status: "Stable",
    tags: ["UI", "Tracking", "Planning"],
    related: ["squirrelsbirthdayreminder", "sunhavenmuseumutilitytracker", "havensalmanac"],
  },
  thevault: {
    icon: "🔒",
    status: "Stable",
    tags: ["Storage", "Currency", "QoL"],
    related: ["senpaischest", "sunhavenmuseumutilitytracker", "havensalmanac"],
  },
  havendevtools: {
    icon: "🛠️",
    status: "Stable",
    tags: ["Dev", "Debug", "Utilities"],
    related: ["havensalmanac", "cropoptimizer", "thevault"],
  },
  havensalmanac: {
    icon: "📖",
    status: "Stable",
    tags: ["UI", "Dashboard", "Tracking"],
    related: ["sunhaventodo", "cropoptimizer", "sunhavenmuseumutilitytracker"],
  },
  fasterraces: {
    icon: "🏃",
    status: "Stable",
    tags: ["QoL", "Movement", "Races"],
    related: ["havensbirthright", "havensrespec", "havensalmanac"],
  },
  trinketfortune: {
    icon: "🎣",
    status: "Stable",
    tags: ["Fishing", "Tracking", "QoL"],
    related: ["sunhavenmuseumutilitytracker", "havensalmanac", "cropoptimizer"],
  },
  cropoptimizer: {
    icon: "🌱",
    status: "Stable",
    tags: ["Farming", "Forecasting", "UI"],
    related: ["havensalmanac", "sunhaventodo", "trinketfortune"],
  },
  havensrespec: {
    icon: "📜",
    status: "Stable",
    tags: ["Skills", "QoL", "UI"],
    related: ["havensbirthright", "fasterraces", "havensalmanac"],
  },
  giftingassistant: {
    icon: "🎁",
    status: "New",
    tags: ["Social", "QoL", "Planning"],
    related: ["squirrelsbirthdayreminder", "sunhaventodo", "havensalmanac"],
  },
};

export const MOD_SCENES = {
  senpaischest: ["Sort chaos into labeled systems", "Build chest rules by category", "Keep inventory clean between sessions"],
  havensbirthright: ["Pick identity-defining perks", "Balance race utility with combat", "Shape long-run character expression"],
  sunhavenmuseumutilitytracker: ["Track remaining donations quickly", "Read section completion at a glance", "Avoid duplicate museum grind"],
  squirrelsbirthdayreminder: ["Catch birthdays before they pass", "Keep gift planning lightweight", "Reduce social progression misses"],
  sunhaventodo: ["Plan daily runs by priority", "Track completion with clarity", "Keep repeat tasks visible"],
  thevault: ["Centralize token/key value", "Spend directly from secured reserves", "Keep your bag focused on play items"],
  havendevtools: ["Inspect runtime state fast", "Debug interactions in live sessions", "Accelerate iteration loops"],
  havensalmanac: ["Read all key signals in one place", "Get morning briefing context", "Reduce tab-hopping between mods"],
  fasterraces: ["Tune movement for your pace", "Keep races feeling distinct", "Stay compatible with broader loadouts"],
  trinketfortune: ["Bias rewards toward unmet goals", "Pair chance with museum progress", "Make fishing outcomes feel fairer"],
  cropoptimizer: ["Forecast value before harvest", "Prioritize plots by projected yield", "Act on ETA and quality context"],
  havensrespec: ["Rebuild trees with confidence", "Preview cost before commitment", "Keep resets safe and controlled"],
  giftingassistant: ["Plan the day's gift run", "See loved/liked gifts with icons", "Mark gifted and stay in sync"],
};

export const MOD_LAYOUTS = {
  senpaischest: "checklist",
  sunhaventodo: "checklist",
  sunhavenmuseumutilitytracker: "timeline",
  squirrelsbirthdayreminder: "timeline",
  havendevtools: "console",
  havensalmanac: "dashboard",
  cropoptimizer: "dashboard",
  havensbirthright: "spotlight",
  fasterraces: "spotlight",
  trinketfortune: "spotlight",
  havensrespec: "spotlight",
  giftingassistant: "checklist",
  thevault: "vault",
};

export const MOD_PROFILES = {
  senpaischest: {
    themeClass: "theme-amber",
    tagline: "Rule-driven sorting that keeps your storage clean without busywork.",
    motif: "Archive Wing",
    context:
      "Senpai's Chest is a storage automation mod focused on reducing inventory friction through chest rules and smart sorting behavior.",
    bestFor: "Players managing many chest categories and frequent loot drops.",
    synergy: "Pairs well with S.M.U.T. and Todo for museum-driven organization loops.",
    story:
      "Built for players with growing inventories, this page uses an archive-inspired style and organizational language to mirror smart sorting.",
    panelTitle: "Sorting highlights",
    highlights: ["Smart chest rules", "Automated sorting passes", "Inventory quality-of-life"],
  },
  havensbirthright: {
    themeClass: "theme-cobalt",
    tagline: "Profession-inspired powers tuned for smooth, readable gameplay.",
    motif: "Forged Paths",
    context:
      "Haven's Birthright introduces race-based gameplay identity with configurable bonuses and abilities for long-run character variety.",
    bestFor: "Players who want race choice to have stronger gameplay impact.",
    synergy: "Works well beside Faster Races and Haven's Respec for build experimentation.",
    story:
      "A bold, progression-focused profile centered on powers and profession flexibility with high readability for moment-to-moment decisions.",
    panelTitle: "Ability highlights",
    highlights: ["Ability enhancements", "Balanced stat flows", "In-run utility upgrades"],
  },
  sunhavenmuseumutilitytracker: {
    themeClass: "theme-slate",
    tagline: "Museum progress clarity so collecting goals stay obvious.",
    motif: "Curator Desk",
    context:
      "S.M.U.T. tracks museum donation progress with visibility improvements so collection goals stay actionable.",
    bestFor: "Completionists and players pushing Hall/Aquarium progress.",
    synergy: "Strong synergy with Senpai's Chest and Trinket Fortune collection loops.",
    story:
      "Curator-like presentation with clean status emphasis, helping collectors quickly read progress and identify what still needs attention.",
    panelTitle: "Collection highlights",
    highlights: ["Donation tracking", "Progress visibility", "Collector-focused workflows"],
  },
  squirrelsbirthdayreminder: {
    themeClass: "theme-teal",
    tagline: "Never miss birthdays again with lightweight reminder support.",
    motif: "Calendar Nook",
    context:
      "Birthday Reminder keeps social progression smooth by surfacing birthday timing and gift context at the right moments.",
    bestFor: "Relationship-focused players who do not want to miss social deadlines.",
    synergy: "Pairs naturally with Todo for auto-generated reminder workflows.",
    story:
      "Friendly reminder-oriented layout focused on clarity and low-noise signals that mimic a social planner board.",
    panelTitle: "Reminder highlights",
    highlights: ["Calendar reminders", "Low-noise alerts", "Friendly HUD prompts"],
  },
  sunhaventodo: {
    themeClass: "theme-violet",
    tagline: "Task tracking for focused days and cleaner farm planning.",
    motif: "Task Command",
    context:
      "Sun Haven Todo brings in-game planning structure for daily priorities, recurring chores, and cross-mod task tracking.",
    bestFor: "Players who like explicit goals and visible progress.",
    synergy: "Integrates smoothly with Birthday Reminder, Gifting Assistant, S.M.U.T., and Almanac flows.",
    story:
      "Checklist-forward design language mirrors the mod's planning flow, with explicit labels and strong visual hierarchy.",
    panelTitle: "Todo highlights",
    highlights: ["Daily objectives", "Completion states", "Simple progress views"],
  },
  thevault: {
    themeClass: "theme-vault",
    tagline: "Centralized vaulting built for fast deposits and inventory calm.",
    motif: "Grand Bank",
    context:
      "The Vault centralizes key/token currency handling so spending and storage happen seamlessly without manual juggling.",
    bestFor: "Players tired of carrying utility currencies in inventory slots.",
    synergy: "Complements storage and dashboard mods like Senpai's Chest and Almanac.",
    story:
      "Bank-inspired styling with a ledger feel, metallic accents, and monetary terminology to reinforce secure storage and value flow.",
    panelTitle: "Vault banking features",
    flavorTitle: "Vault prestige",
    flavorText:
      "From the entrance arch to the ledger cards, this profile leans into a grand-bank identity that celebrates control, order, and value.",
    highlights: ["Bulk storage flow", "Sweep actions", "Vault HUD totals"],
  },
  havendevtools: {
    themeClass: "theme-indigo",
    tagline: "Debug and inspection tools that speed up mod iteration loops.",
    motif: "Control Console",
    context:
      "HavenDevTools is a debugging utility layer for inspecting game state and accelerating mod development workflows.",
    bestFor: "Mod developers and testers doing runtime validation.",
    synergy: "Useful alongside any mod when debugging integration behavior.",
    story:
      "Engineering-console vibe for developer workflows, emphasizing diagnostics, traceability, and quick iteration loops.",
    panelTitle: "Dev highlights",
    highlights: ["Developer tooling", "Runtime introspection", "Faster troubleshooting"],
  },
  havensalmanac: {
    themeClass: "theme-sky",
    tagline: "Daily briefing surfaces what matters at a glance.",
    motif: "Morning Briefing",
    context:
      "Haven's Almanac aggregates multi-mod signals into digestible summaries, HUD views, and daily decision support.",
    bestFor: "Players running multiple mods who want one command center.",
    synergy: "Designed to integrate with tracking-heavy mods like Todo and Crop Optimizer.",
    story:
      "Report-like presentation with digest semantics to match how the mod summarizes key daily context for players.",
    panelTitle: "Briefing highlights",
    highlights: ["Briefing overlays", "Status summaries", "Clean information density"],
  },
  fasterraces: {
    themeClass: "theme-greenblue",
    tagline: "Race pacing refinements for a snappier feel during play.",
    motif: "Speed Track",
    context:
      "Faster Races focuses on movement pacing and responsiveness with controlled tuning for smoother traversal.",
    bestFor: "Players who want a snappier movement baseline.",
    synergy: "Can be combined with Birthright and Respec-centered builds.",
    story:
      "Motion-centric page character focused on pacing and responsiveness, while maintaining contrast-safe readability.",
    panelTitle: "Speed highlights",
    highlights: ["Movement tuning", "Smoother progression", "Configurable speed behavior"],
  },
  trinketfortune: {
    themeClass: "theme-orchid",
    tagline: "Smarter reward biasing to make trinket and museum outcomes feel better.",
    motif: "Lucky Atelier",
    context:
      "Trinket Fortune biases outcomes toward collection progression, making late-stage trinket completion less punishing.",
    bestFor: "Fishing-focused players chasing missing museum/trinket entries.",
    synergy: "Most effective when paired with S.M.U.T. tracking visibility.",
    story:
      "Treasure-hunt inspired profile emphasizing chance tuning and curated outcomes without sacrificing clear metric readability.",
    panelTitle: "Fortune highlights",
    highlights: ["Bias-aware selection", "Museum-aware outcomes", "Config-based control"],
  },
  cropoptimizer: {
    themeClass: "theme-gold",
    tagline: "Crop intelligence that helps prioritize profit and timing.",
    motif: "Field Analyst",
    context:
      "Crop Optimizer provides crop-level timing and value forecasting for better planting and harvest decisions.",
    bestFor: "Players optimizing farm profit and scheduling windows.",
    synergy: "Pairs well with Almanac and Todo planning loops.",
    story:
      "Yield-and-timing framing with pragmatic, planner-oriented language to match farming optimization workflows.",
    panelTitle: "Optimization highlights",
    highlights: ["Growth insight", "Profit forecasting", "Priority recommendations"],
  },
  havensrespec: {
    themeClass: "theme-rose",
    tagline: "Flexible profession resets with clearer cost feedback.",
    motif: "Reset Atelier",
    context:
      "Haven's Respec enables controlled profession reset workflows with clearer cost and safety feedback.",
    bestFor: "Players iterating builds without losing confidence in reset outcomes.",
    synergy: "Pairs with Birthright/Faster Races for broad build experimentation.",
    story:
      "A reset-and-rebuild narrative style with explicit cost visibility, reinforcing confidence before large progression changes.",
    panelTitle: "Respec highlights",
    highlights: ["Respec controls", "Cost previews", "Safer reset flow"],
  },
  giftingassistant: {
    themeClass: "theme-orchid",
    tagline: "A daily gift routine that keeps friendships on track.",
    motif: "Gift Desk",
    context:
      "Gifting Assistant turns daily gift-giving into a per-character routine with loved/liked suggestions, item icons, priorities, and gifted-today tracking.",
    bestFor: "Relationship-focused players who want a repeatable daily gift plan.",
    synergy: "Pairs with Sun Haven Todo (daily gift tasks), Birthday Reminder, and Almanac progress.",
    story:
      "Checklist-forward profile built around a daily roster, mirroring how the mod plans, prioritizes, and checks off each gift.",
    panelTitle: "Gifting highlights",
    highlights: ["Daily gift roster", "Loved/liked suggestions", "Todo auto-complete sync"],
  },
};

export const STARTER_BUNDLES = [
  { id: "collector", title: "Collector Stack", meta: "Museum and completion flow", mods: ["sunhavenmuseumutilitytracker", "trinketfortune", "senpaischest"] },
  { id: "farm", title: "Farm Planner Stack", meta: "Planning and crop optimization", mods: ["cropoptimizer", "sunhaventodo", "havensalmanac"] },
  { id: "power", title: "Power Tuning Stack", meta: "Build shaping and pace control", mods: ["havensbirthright", "fasterraces", "havensrespec"] },
];

export function slugify(value) {
  return String(value || "")
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

export function resolveStatsId(modKey) {
  const k = String(modKey || "");
  return STATS_ID_BY_MOD_KEY[k] || slugify(k);
}
