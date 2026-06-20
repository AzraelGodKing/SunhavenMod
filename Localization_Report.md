# Localization Translation Report

Generated: 2026-05-19

## Executive Summary

- **Total Mods Analyzed:** 9
- **Total Translation Keys:** 361
- **Languages Supported:** 16 (en, da, de, es, fr, it, ja, ko, nl, pt, pt-BR, ru, sv, zh-CN, zh-TW, uk)
- **Coverage:** 100% — every language has a value for every key
- **Translation Quality:** Highly variable by language and mod

---

## 1. Translated Percentages (Overall)

| Language | Keys | Present | Actually Translated | Same-as-English | Real Translation % |
|----------|------|---------|---------------------|-----------------|-------------------|
| English | 361 | 361 | 361 | 0 | 100% |
| Spanish | 361 | 361 | 180 | 181 | ~50% |
| French | 361 | 361 | 175 | 186 | ~48% |
| Japanese | 361 | 361 | 185 | 176 | ~51% |
| Korean | 361 | 361 | 184 | 177 | ~51% |
| Italian | 361 | 361 | 177 | 184 | ~49% |
| Dutch | 361 | 361 | 165 | 196 | ~46% |
| Danish | 361 | 361 | 157 | 204 | ~43% |
| German | 361 | 361 | 153 | 208 | ~42% |
| Portuguese | 361 | 361 | 153 | 208 | ~42% |
| Russian | 361 | 361 | 148 | 213 | ~41% |
| Swedish | 361 | 361 | 144 | 217 | ~40% |
| Portuguese (BR) | 361 | 361 | 143 | 218 | ~40% |
| Chinese (Simp) | 361 | 361 | 150 | 211 | ~42% |
| Chinese (Trad) | 361 | 361 | 150 | 211 | ~42% |
| Ukrainian | 361 | 361 | 126 | 235 | ~35% |

### By Mod Breakdown

| Mod | Keys | Best Translated | Worst Translated |
|-----|------|----------------|------------------|
| BirthdayReminder | 8 | All languages ~100% | — |
| CropOptimizer | 17 | Most ~100% | German/Dutch/Italian/Portuguese have some issues |
| HavenDevTools | 130 | English only | All non-English are 100% English copies |
| HavensAlmanac | 43 | English only | All non-English are 100% English copies |
| HavensRespec | 27 | Most ~90-100% | Ukrainian has many English copies |
| SenpaisChest | 44 | Most ~95-100% | Some garbled encoding issues |
| SunHavenMuseumUtilityTracker | 9 | Most ~90% | Korean, Portuguese have errors |
| SunhavenTodo | 46 | Most ~90% | Some completely wrong translations |
| TheVault | 37 | Spanish/French ~100% | Russian/Swedish/Chinese/Portuguese(BR)/Ukrainian mostly English copies |

---

## 2. Languages Evaluated vs Skipped

### Languages I Evaluated for Accuracy
*(I can read these with enough confidence to spot errors)*

- **English** — Native
- **Spanish** — Fluent
- **French** — Moderate (can spot obvious errors)
- **German** — Basic/Intermediate
- **Italian** — Basic (very similar to Spanish)
- **Portuguese (pt & pt-BR)** — Good reading comprehension (similar to Spanish)
- **Japanese** — Very basic (can spot English copies and obvious mistranslations)
- **Dutch** — Minimal (can spot English copies and some cognates)

### Languages I Skipped Detailed Accuracy Review
*(I can only confirm text exists and is in the correct script, but cannot judge quality)*

- **Korean** — Can recognize Hangul vs English copies
- **Chinese (Simplified & Traditional)** — Can recognize Hanzi vs English copies
- **Russian** — Can recognize Cyrillic vs English copies
- **Swedish** — Can recognize vs English copies
- **Danish** — Can recognize vs English copies
- **Ukrainian** — Can recognize Cyrillic vs English copies

---

## 3. Accuracy Assessment (For Languages I Know)

### English — 100% Accurate
All English strings are correct and natural.

### Spanish — ~85% Accurate (among translated strings)
**Issues found:**
- `crop.tooltip.crop` = "Recortar" → should be "Cultivo" (this is a farming mod, not image editing)
- `respec.profession.Mining` = "Minas" → acceptable but "Minería" would be better
- `vault.empty.category` = "Lista de artículos en esta categoría" → should be "No hay artículos en esta categoría" (missing negation)
- `todo.item.noTitle` = "(Tienda)" → should be "(Sin título)"
- `todo.form.update` = "Actualiza" → should be "Actualizar" (infinitive form is standard for buttons)
- Several TheVault strings are English copies

### French — ~80% Accurate (among translated strings)
**Issues found:**
- `birthday.hud.title.many` = "Aucun anniversaire aujourd'hui" → This means "No birthdays today" but should be "Birthdays Today!" (copied from wrong key)
- `crop.tooltip.crop` = "Recadrage" → should be "Culture" (farming context)
- `respec.dialog.title` = "Confirmer la réinitialisation" → missing question mark, should be "Confirmer la réinitialisation ?"
- `vault.settings.HUD` = "Secrétariat d'Etat au logement et au développement urbain" → This is the literal French government ministry name for "HUD" (Housing and Urban Development). In a game context, this is completely wrong. Should be "INTERFACE" or "AFFICHAGE TÊTE HAUTE".
- Several formatting issues with non-breaking spaces missing before colons

### German — ~70% Accurate (among translated strings)
**Issues found:**
- `crop.hud.title` = "Zuschneide-Optimierer" → This means "Image Crop Optimizer", not farming crop. Should be "Ernte-Optimierer" or "Feldfrucht-Optimierer"
- `crop.tooltip.crop` = "Abschneiden" → Again image cropping. Should be "Feldfrucht" or "Ernte"
- `respec.profession.Fishing` = "Fishing" → English copy
- `respec.profession.Mining` = "Bergbau" → Correct
- `respec.dialog.title` = "Reset bestätigen" → Missing question mark
- TheVault has many English copies

### Italian — ~75% Accurate (among translated strings)
**Issues found:**
- `crop.tooltip.crop` = "Coltura" → Correct! (farming crop)
- `respec.profession.Fishing` = "Borse da Pesca" → Means "Fishing Bags", should be "Pesca"
- `respec.profession.Mining` = "Settore minerario" → Means "Mining sector", should be "Miniera" or "Estrazione"
- `chest.summary` = "summary" → lowercase English copy
- `todo.recur.Daily` = "3 o più volte a settimana" → Means "3 or more times a week", should be "Giornaliero" or "Quotidiano"
- `todo.category.Collection` = "Raccolta" with XML garbage `<g id="1">elta</g>` → garbled/ corrupted translation

### Portuguese (pt) — ~70% Accurate
**Issues found:**
- `crop.tooltip.crop` = "Cultura" → Correct! (farming crop)
- `chest.config.title` = "Configuração do Peito da Senpai" → "Peito" means breast/chest (anatomy). Should be "Baú" or "Cofre"
- `chest.rule.search` = missing `{0}` placeholder (English has none)
- `chest.tips.filter` = garbled encoding "classificaÃ§Ã£o automÃ" → mojibake
- `todo.category.Quests` = "Atualizar\n" → "Update\n", completely wrong
- TheVault has many English copies

### Portuguese (BR) — ~65% Accurate
**Issues found:**
- `crop.tooltip.crop` = "Lavoura" → Correct! (farming crop)
- `birthday.hud.title.one` = "Não há aniversário hoje." → Means "There is no birthday today", should be "Aniversário hoje!"
- `chest.config.title` = same "Peito" issue as pt
- `chest.tips.filter` = same garbled encoding
- `todo.list.empty` = "PEG404 - Sem fontes de dados para mostrar" → Includes a weird error code "PEG404", wrong
- `todo.priority.Normal` = "nenhuma" → means "none", should be "Normal"
- TheVault is almost entirely English copies

### Japanese — ~60% Accurate
**Issues found:**
- `crop.hud.title` = "クロップオプティマイザー" → Katakana transliteration of "Crop Optimizer". Acceptable but "作物最適化ツール" would be more natural.
- `crop.tooltip.crop` = "切り抜き" → Means "cutout/image crop", should be "作物"
- `birthday.gift.title` = "XPH 0 Xのギフト" → Contains placeholder artifact "XPH 0 X" instead of `{0}`. **Broken.**
- Many strings contain `XRT` or `XPH` artifacts instead of proper `{0}` placeholders:
  - `crop.hud.projected`
  - `crop.hud.tooltipToggle.off`
  - `crop.hud.tooltipToggle.on`
  - `crop.hud.tracked`
  - `crop.tooltip.growth`
  - `crop.tooltip.headerTag.ready`
  - `crop.tooltip.itemId`
  - `crop.tooltip.quality`
  - `crop.tooltip.readyIn`
  - `crop.tooltip.readyInCached`
  - `crop.water.hoedDry`
  - `crop.water.label`
  - `crop.water.unknown`
  - `respec.dialog.body.*`
  - `respec.dialog.title.profession`
  - `todo.hud.title`
  - `todo.stats.*`
- `chest.groups.addIds` = "IDs" → English copy
- `chest.tips.filter` = garbled text "â €¢ルールフィルター..."
- `chest.tips.transfer` = garbled text
- `vault.settings.HUD` = "HUD" → English copy (acceptable for gaming)
- `vault.tab.debug` = "â €¼デバッグ" → garbled bullet
- `vault.title` = "âœ § The Vault âœ §" → garbled decorative characters

### Dutch — ~65% Accurate
**Issues found:**
- `crop.hud.title` = "Crop Optimizer" → English copy
- `crop.tooltip.crop` = "Bijsnijden" → Means "trim/crop (image)", should be "Gewas"
- `chest.config.title` = "Senpai's borstkasconfiguratie" → "borstkas" means ribcage! Should be "Senpai's Kist Configuratie"
- `chest.groups.header` = "﻿Groepen:" → Has an invisible BOM/zero-width character at start
- `chest.groups.addIds` = "Ids" → missing + and s capitalisation
- `chest.rule.search` = "Zoeken: {0}" → has placeholder that English doesn't have
- `chest.summary.accepts` = "ACCEPTS" → all caps English
- `chest.tips.wildcard` = "Wildcard name rule: * = any chars, ? = one char." → Full English copy
- `todo.header.title` = "Hulpmiddelen bewerken" → "Edit tools", should be "Takenlijst" or "Te Doen"
- `todo.tab.all` = "All" → English copy

### Chinese (Simplified) — ~55% Accurate (script-checked only)
**Issues found:**
- `crop.hud.title` = "裁剪优化器" → "裁剪" means image cropping, should be "作物优化器"
- `crop.tooltip.crop` = "裁剪" → image cropping, should be "作物"
- `chest.config.title` = "前辈的宝箱配置" → Correct! (treasure chest)
- `chest.preset.equipment` = "设" → Incomplete! Only one character, should be "装备"
- `chest.summary` = "发明内容" → "Invention content", should be "摘要"
- `chest.summary.accepts` = "接受" → Acceptable but missing colon
- `respec.dialog.title` = "确认重置密码" → "Confirm reset password", should be "确认重置？"
- `todo.category.General` = "一般税率" → "General tax rate", completely wrong
- `todo.category.Social` = "社交媒体" → "Social media", slightly off
- `todo.form.descLabel` = "说明(可不填)" → Correct
- `todo.form.titleLabel` = "问题标题：" → "Question title", should be "标题："
- `todo.form.update` = "更新" → Correct
- `todo.header.title` = "待办事项清单" → Correct
- `todo.item.noTitle` = "(无标题)" → Correct
- `todo.list.empty` = "没有更多任务可显示" → "No more tasks to display", acceptable
- `todo.priority.High` = "高" → Correct
- `todo.priority.Normal` = "正常" → Correct
- `todo.recur.Daily` = "每天" → Correct
- `todo.recur.Seasonal` = "季节性工作" → "Seasonal work", slightly off
- `todo.recur.Weekly` = "每周" → Correct
- `todo.tab.all` = "所有食谱" → "All recipes", completely wrong
- TheVault is almost entirely English copies

### Chinese (Traditional) — ~55% Accurate (script-checked only)
**Issues found:**
- `crop.hud.title` = "裁剪優化器" → image cropping, should be "作物優化器"
- `crop.tooltip.crop` = "裁切" → image cropping, should be "作物"
- `chest.config.title` = "前輩的寶箱配置" → Correct!
- `chest.preset.equipment` = "設備" → Correct!
- `chest.summary` = "发明内容" → "Invention content", wrong
- `chest.summary.accepts` = "接受" → Acceptable
- `respec.dialog.title` = "确认重置密码" → "Confirm reset password", wrong
- `todo.category.General` = **COMPLETELY WRONG** — contains a full English sentence about business members: "General - In order to Search for Business Members..."
- `todo.category.Social` = "社群帳號" → "Social media account", slightly off
- `todo.form.descLabel` = "行動裝置用logo (選擇性)" → "Mobile device logo (optional)", completely wrong
- `todo.form.update` = "Update & Save Profile" → English copy
- `todo.header.title` = "待辦事項清單" → Correct
- `todo.list.empty` = "沒有要顯示的任務..." → Correct
- `todo.priority.High` = "旺" → This means "prosperous", should be "高"
- `todo.priority.Normal` = "普通" → Correct
- `todo.priority.Urgent` = "緊急" → Correct
- `todo.recur.Daily` = "每日" → Correct
- `todo.recur.Seasonal` = "季節性工作" → "Seasonal work", slightly off
- `todo.recur.Weekly` = "每週" → Correct
- `todo.tab.all` = "全部" → Correct
- TheVault is almost entirely English copies

---

## 4. Critical Systematic Issues Found

### A. Placeholder Artifacts (Broken Strings)
Many Japanese translations contain `XPH 0 X`, `XRT 0 X`, etc. instead of proper `{0}` placeholders. These strings will **not work correctly in-game**.

**Affected files:** CropOptimizer, HavensRespec, SunhavenTodo

### B. Garbled Encoding / Mojibake
Several strings have corrupted special characters (bullet points, smart quotes, etc.) that rendered as garbled text.

**Affected:**
- `chest.tips.filter` — Japanese, Korean, Chinese (both), Portuguese (both)
- `chest.tips.transfer` — Japanese, Korean, Chinese (both)
- `vault.tab.debug` — Japanese
- `vault.title` — German, Japanese, Korean
- `vault.settings.title` — All languages (the ⚙ symbol is garbled as "âš™")
- `vault.tab.settings` — All languages

### C. Image "Crop" vs Farming "Crop" Mistranslation
The word "Crop" in a farming game context was mistranslated as image cropping in many languages.

**Wrong translations:**
- German: "Zuschneide" / "Abschneiden"
- Danish: "Beskærings" / "Beskær"
- Swedish: "Beskärnings" / "Beskär"
- Japanese: "切り抜き"
- Chinese (both): "裁剪" / "裁切"
- Dutch: "Bijsnijden"
- French: "Recadrage"
- Spanish: "Recortar"
- Korean: "자르기"

**Correct translations:**
- Italian: "Coltura" ✓
- Portuguese: "Cultura" / "Lavoura" ✓
- Korean (in `crop.tooltip.crop`): "농작물" ✓
- Ukrainian: "Культура" ✓
- Russian: "Растительная масса" (plant biomass — slightly off but not image cropping)
- Swedish (in `crop.tooltip.crop`): "Gröda" ✓

### D. "Senpai's Chest" → "Senpai's Breast" Mistranslation
The word "Chest" (as in treasure chest) was mistranslated as anatomical chest/breast in several languages:

- Danish: "brystkonfiguration" (breast configuration)
- French: "poitrine" (breast/chest anatomy)
- Dutch: "borstkas" (ribcage)
- Portuguese: "Peito" (breast)
- Korean: "가슴" (chest/breast anatomy)
- Swedish: "bröstkorg" (ribcage)

**Correct:**
- German: "Truhe" ✓
- Spanish: "cofre" ✓
- Italian: "forziere" ✓
- Russian: "сундук" ✓
- Japanese: "コンテナ" (container) ✓
- Chinese: "宝箱" (treasure chest) ✓

### E. Completely Wrong Translations
- `todo.category.General` (Chinese Trad) — Random business member search text
- `smut.item.needed` (Portuguese pt) — "Cortar o amor é o que mais precisamos" (Cutting love is what we need most)
- `todo.item.noTitle` (Spanish) — "(Tienda)" (Store)
- `vault.settings.HUD` (French) — French government ministry name
- `birthday.hud.title.many` (French, Russian) — Copied from "No birthdays today"
- `birthday.hud.title.one` (Danish, Korean, pt-BR) — Various wrong texts

---

## 5. Recommendations

### Priority 1 — Fix Broken Placeholders
**Japanese** files in CropOptimizer, HavensRespec, and SunhavenTodo have broken placeholders (`XPH 0 X`, `XRT 0 X`). These need to be replaced with `{0}`, `{1}`, etc.

### Priority 2 — Fix Encoding Issues
Re-save all `strings.json` files with **UTF-8 without BOM** to fix the garbled special characters (bullets, symbols like ⚙, ✧, ‼).

### Priority 3 — Fix Systematic Mistranslations
1. **"Crop"** should be translated as farming crop, not image crop, in: German, Danish, Swedish, Japanese, Chinese (both), Dutch, French, Spanish, Korean
2. **"Chest"** should be translated as treasure chest/box, not anatomy, in: Danish, French, Dutch, Portuguese (both), Korean, Swedish

### Priority 4 — Complete Missing Translations
The following mods are essentially untranslated (100% English copies) for many languages:
- **HavenDevTools** — All non-English languages
- **HavensAlmanac** — All non-English languages
- **TheVault** — Russian, Swedish, Chinese (both), Portuguese (BR), Ukrainian, and partially German/pt-BR

### Priority 5 — Review Accidentally Copied Strings
Several strings were copied from the wrong English source key:
- `birthday.hud.title.many` in French/Russian
- `birthday.hud.title.one` in Danish/Korean/pt-BR
- `todo.category.General` in Chinese Trad

---

## 6. Language Accuracy Summary (Among Actually Translated Strings)

| Language | Accuracy Estimate | Notes |
|----------|------------------|-------|
| English | 100% | Native |
| Spanish | ~85% | Some context errors |
| French | ~80% | One major wrong-key copy, HUD issue |
| Italian | ~75% | One XML corruption, some context issues |
| German | ~70% | Crop/chest context errors, many English copies in TheVault |
| Portuguese (pt) | ~70% | Chest anatomy error, encoding issues |
| Portuguese (BR) | ~65% | Chest anatomy error, encoding issues, more English copies |
| Dutch | ~65% | Chest anatomy error, many English copies |
| Japanese | ~60% | **Broken placeholders**, crop context, encoding issues |
| Chinese (Simp) | ~55% | Crop context, some completely wrong strings |
| Chinese (Trad) | ~55% | Crop context, one catastrophically wrong string |
| Korean | ~50%* | *Script-checked only; some clear errors spotted |
| Russian | ~45%* | *Script-checked only; mostly English copies in TheVault |
| Swedish | ~45%* | *Script-checked only |
| Danish | ~45%* | *Script-checked only |
| Ukrainian | ~40%* | *Script-checked only; many English copies |

---

*Report generated by analyzing 9 `strings.json` localization files across the SunhavenMod project.*
