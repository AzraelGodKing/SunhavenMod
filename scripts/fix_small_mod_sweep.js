#!/usr/bin/env node
/** Fill remaining English-copy gaps in small mods + fix CropOptimizer Rich Text / JA placeholders. */
const fs = require("fs");
const path = require("path");

const ROOT = path.join(__dirname, "..");

const PATCHES = {
  CropOptimizer: {
    "crop.water.label": { nl: "Waterstand: {0}" },
  },
  SenpaisChest: {
    "chest.tips.header": { da: "Gode råd", nl: "Handige tips", sv: "Råd" },
  },
  TheVault: {
    "vault.settings.hud": {
      da: "Skærmvisning",
      de: "Anzeige",
      fr: "Affichage tête haute",
      it: "Interfaccia",
      ja: "画面上部表示",
      ko: "화면 표시",
      nl: "Schermweergave",
      pt: "Exibição na tela",
      "pt-BR": "Exibição na tela",
      sv: "Skärmvisning",
      "zh-CN": "抬头显示",
      uk: "Екранна панель",
    },
  },
  SunhavenTodo: {
    "todo.category.Collection": { fr: "Collecte" },
    "todo.category.Combat": { fr: "Affrontement" },
    "todo.category.Social": {
      da: "Socialt",
      es: "Relaciones sociales",
      fr: "Relations sociales",
      pt: "Relações sociais",
      "pt-BR": "Relações sociais",
    },
    "todo.priority.Normal": {
      da: "Almindelig",
      de: "Standard",
      es: "Estándar",
      pt: "Padrão",
      sv: "Vanlig",
    },
    "todo.priority.Urgent": { fr: "Prioritaire" },
    "todo.stats.total": {
      da: "I alt: {0}",
      es: "En total: {0}",
      pt: "No total: {0}",
      "pt-BR": "No total: {0}",
    },
  },
  HavensRespec: {
    "respec.button.reset": { it: "Reimposta", nl: "Resetten" },
        "respec.cost.gold": { de: "{0:N0} Goldmünzen" },
        "respec.cost.tickets": {
            de: "{0:N0} Eintrittskarten",
      es: "{0:N0} boletos",
      nl: "{0:N0} kaartjes",
    },
    "respec.dialog.confirm": { it: "Reimposta", nl: "Resetten" },
    "respec.profession.Combat": { fr: "Art du combat" },
    "respec.profession.Exploration": { fr: "Découverte" },
  },
  SunHavenMuseumUtilityTracker: {
    "smut.subtitle": {
      da: "Sun Haven museumsværktøjssporing",
      de: "Sun Haven Museum-Hilfsprogramm",
      fr: "Utilitaire de suivi du musée Sun Haven",
      nl: "Sun Haven museumhulpmiddel-tracker",
      sv: "Sun Haven museumverktygsspårning",
    },
  },
  HavensAlmanac: {
    "almanac.dashboard.error": { es: "Fallo: {0}" },
    "almanac.dashboard.title": {
      de: "Havens Almanach – Dashboard",
      nl: "Haven's Almanak – Dashboard",
    },
    "almanac.hud.title": {
      da: "Havens almanak",
      sv: "Havens almanacka",
      "zh-CN": "Haven's 年鉴",
      "zh-TW": "Haven's 年鑑",
    },
    "almanac.provider.birthright.race": { da: "Folk: {0}" },
    "almanac.provider.crop.plant": { nl: "gewas" },
    "almanac.provider.devtools.player": { de: "Spieler: {0}" },
    "almanac.provider.devtools.status": {
      da: "Tilstand: {0}",
      de: "Zustand: {0}",
      nl: "Toestand: {0}",
      pt: "Estado: {0}",
      "pt-BR": "Estado: {0}",
      sv: "Tillstånd: {0}",
    },
    "almanac.provider.museum.item": {
      nl: "voorwerp",
      pt: "artigo",
      "pt-BR": "objeto",
    },
    "almanac.provider.museum.items": { pt: "artigos" },
    "almanac.provider.vault.currency": {
      es: "moneda",
      fr: "monnaie",
      ja: "通貨",
      pt: "moeda",
    },
    "almanac.provider.birthday.gifted": { sv: " [Givet]" },
    "almanac.provider.birthday.notGifted": {
      ja: " [未贈呈]",
      "zh-CN": " [未送礼]",
      "zh-TW": " [未送禮]",
    },
    "almanac.provider.crop.topRow": {
      de: "  {0}. {1} – {2} Gold ({3} {4})",
      es: "  {0}. {1} — {2} oro ({3} {4})",
      fr: "  {0}. {1} — {2} pièces d'or ({3} {4})",
      it: "  {0}. {1} — {2} oro ({3} {4})",
      ko: "  {0}. {1} — {2}골 ({3} {4})",
      nl: "  {0}. {1} — {2} goud ({3} {4})",
      ru: "  {0}. {1} — {2} зол. ({3} {4})",
      sv: "  {0}. {1} — {2} g ({3} st.)",
      "zh-CN": "  {0}. {1} — {2}金 ({3}{4})",
      uk: "  {0}. {1} — {2} зол. ({3} {4})",
    },
  },
};

function fixXphPlaceholders(text) {
  return text
    .replace(/XPH\s*(\d+)\s*X/g, "{$1}")
    .replace(/XRT\s*(\d+)\s*X/g, "{$1}")
    .replace(/\{\s*(\d+)\s*:\s*([^}]+?)\s*\}/g, "{$1:$2}");
}

function fixCropOptimizer(data) {
  data["crop.tooltip.quality"].en = data["crop.tooltip.quality"].en.replace("Ã—", "×");

  const quality = {
    da: "Kvalitet: <b>{0}</b> <color={1}>(×{2:0.##})</color>",
    de: "Qualität: <b>{0}</b> <color={1}>(×{2:0.##})</color>",
    es: "Calidad: <b>{0}</b> <color={1}>(×{2:0.##})</color>",
    fr: "Qualité : <b>{0}</b> <color={1}>(×{2:0.##})</color>",
    it: "Qualità: <b>{0}</b> <color={1}>(×{2:0.##})</color>",
    ja: "品質: <b>{0}</b> <color={1}>(×{2:0.##})</color>",
    ko: "품질: <b>{0}</b> <color={1}>(×{2:0.##})</color>",
    nl: "Kwaliteit: <b>{0}</b> <color={1}>(×{2:0.##})</color>",
    pt: "Qualidade: <b>{0}</b> <color={1}>(×{2:0.##})</color>",
    "pt-BR": "Qualidade: <b>{0}</b> <color={1}>(×{2:0.##})</color>",
    ru: "Качество: <b>{0}</b> <color={1}>(×{2:0.##})</color>",
    sv: "Kvalitet: <b>{0}</b> <color={1}>(×{2:0.##})</color>",
    "zh-CN": "品质: <b>{0}</b> <color={1}>(×{2:0.##})</color>",
    "zh-TW": "品質: <b>{0}</b> <color={1}>(×{2:0.##})</color>",
    uk: "Якість: <b>{0}</b> <color={1}>(×{2:0.##})</color>",
  };
  Object.assign(data["crop.tooltip.quality"], quality);

  const ready = {
    da: "Klar om <b><color={0}>~{1:0.#} t</color></b>",
    de: "Bereit in <b><color={0}>~{1:0.#} Std.</color></b>",
    es: "Listo en <b><color={0}>~{1:0.#} h</color></b>",
    fr: "Prêt dans <b><color={0}>~{1:0.#} h</color></b>",
    it: "Pronto tra <b><color={0}>~{1:0.#} h</color></b>",
    ja: "<b><color={0}>~{1:0.#}時間</color></b>で収穫可能",
    ko: "<b><color={0}>~{1:0.#}시간</color></b> 후 수확 가능",
    nl: "Gereed over <b><color={0}>~{1:0.#} u</color></b>",
    pt: "Pronto em <b><color={0}>~{1:0.#} h</color></b>",
    "pt-BR": "Pronto em <b><color={0}>~{1:0.#} h</color></b>",
    ru: "Готово через <b><color={0}>~{1:0.#} ч</color></b>",
    sv: "Klar om <b><color={0}>~{1:0.#} h</color></b>",
    "zh-CN": "<b><color={0}>~{1:0.#}小时</color></b>后就绪",
    "zh-TW": "<b><color={0}>~{1:0.#}小時</color></b>後就緒",
    uk: "Готово через <b><color={0}>~{1:0.#} год</color></b>",
  };
  Object.assign(data["crop.tooltip.readyIn"], ready);

  const cachedSuffix = {
    da: " (cachelagret)",
    de: " (zwischengespeichert)",
    es: " (en caché)",
    fr: " (mis en cache)",
    it: " (in cache)",
    ja: " (キャッシュ済み)",
    ko: " (캐시됨)",
    nl: " (in cache)",
    pt: " (em cache)",
    "pt-BR": " (em cache)",
    ru: " (кэшировано)",
    sv: " (cachelagrat)",
    "zh-CN": " (已缓存)",
    "zh-TW": " (已快取)",
    uk: " (кешовано)",
  };
  for (const [lang, suffix] of Object.entries(cachedSuffix)) {
    data["crop.tooltip.readyInCached"][lang] =
      `${ready[lang]} <color={2}>${suffix}</color>`;
  }

  data["crop.hud.projected"].da =
    "Forventet salg: <color=#F7D982><b>{0:N0}g</b></color>";
  data["crop.hud.projected"].ko =
    "예상 판매: <color=#F7D982><b>{0:N0}g</b></color>";
  data["crop.hud.projected"]["zh-CN"] =
    "预计销售: <color=#F7D982><b>{0:N0}g</b></color>";
  data["crop.hud.projected"]["zh-TW"] =
    "預計賣出: <color=#F7D982><b>{0:N0}g</b></color>";
}

function fixJaXph(data) {
  for (const entry of Object.values(data)) {
    if (entry.ja) entry.ja = fixXphPlaceholders(entry.ja);
  }
}

for (const mod of Object.keys(PATCHES)) {
  const filePath = path.join(ROOT, mod, "Localization", "strings.json");
  const raw = fs.readFileSync(filePath, "utf8").replace(/^\uFEFF/, "");
  const data = JSON.parse(raw);

  for (const [key, langs] of Object.entries(PATCHES[mod])) {
    for (const [lang, value] of Object.entries(langs)) {
      data[key][lang] = value;
    }
  }

  if (mod === "CropOptimizer") fixCropOptimizer(data);
  fixJaXph(data);

  fs.writeFileSync(filePath, JSON.stringify(data, null, 4) + "\n", "utf8");
  console.log(`Updated ${mod}`);
}
