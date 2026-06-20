import os

# Define fixes for each file as list of (old, new) tuples
fixes = {
    "CropOptimizer/Localization/strings.json": [
        # Fertilized English copies
        ('"de":  "\u003cb\u003eFertilized\u003c/b\u003e",', '"de":  "\u003cb\u003eGedüngt\u003c/b\u003e",'),
        ('"it":  "\u003cb\u003eFertilized\u003c/b\u003e",', '"it":  "\u003cb\u003eConcimato\u003c/b\u003e",'),
        ('"pt":  "\u003cb\u003eFertilized\u003c/b\u003e",', '"pt":  "\u003cb\u003eFertilizado\u003c/b\u003e",'),
        ('"pt-BR":  "\u003cb\u003eFertilized\u003c/b\u003e",', '"pt-BR":  "\u003cb\u003eFertilizado\u003c/b\u003e",'),
        # Broken ja fertilized
        ('"ja":  "XRT 0 X受精XRT 1 X",', '"ja":  "\u003cb\u003e施肥済み\u003c/b\u003e",'),
        # Water unknown English copies
        ('"de":  "Wasser: \u003ccolor=#B8A078\u003eunknown\u003c/color\u003e",', '"de":  "Wasser: \u003ccolor=#B8A078\u003eunbekannt\u003c/color\u003e",'),
        ('"es":  "Agua: \u003ccolor=#B8A078\u003eunknown\u003c/color\u003e",', '"es":  "Agua: \u003ccolor=#B8A078\u003edesconocido\u003c/color\u003e",'),
        ('"fr":  "Eau\u00a0: \u003ccolor=#B8A078\u003eunknown\u003c/color\u003e",', '"fr":  "Eau\u00a0: \u003ccolor=#B8A078\u003einconnu\u003c/color\u003e",'),
        ('"it":  "Acqua: \u003ccolor=#B8A078\u003eunknown\u003c/color\u003e",', '"it":  "Acqua: \u003ccolor=#B8A078\u003esconosciuto\u003c/color\u003e",'),
        ('"ko":  "물: \u003ccolor=#B8A078\u003eunknown\u003c/color\u003e",', '"ko":  "물: \u003ccolor=#B8A078\u003e알 수 없음\u003c/color\u003e",'),
        ('"pt":  "Água: \u003ccolor=#B8A078\u003eunknown\u003c/color\u003e",', '"pt":  "Água: \u003ccolor=#B8A078\u003edesconhecido\u003c/color\u003e",'),
        ('"pt-BR":  "Água: \u003ccolor=#B8A078\u003eunknown\u003c/color\u003e",', '"pt-BR":  "Água: \u003ccolor=#B8A078\u003edesconhecido\u003c/color\u003e",'),
        ('"ru":  "Вода: \u003ccolor=#B8A078\u003eunknown\u003c/color\u003e",', '"ru":  "Вода: \u003ccolor=#B8A078\u003eнеизвестно\u003c/color\u003e",'),
        ('"sv":  "Vatten: \u003ccolor=#B8A078\u003eunknown\u003c/color\u003e",', '"sv":  "Vatten: \u003ccolor=#B8A078\u003eokänt\u003c/color\u003e",'),
        ('"zh-CN":  "水： \u003ccolor=#B8A078\u003eunknown\u003c/color\u003e",', '"zh-CN":  "水： \u003ccolor=#B8A078\u003e未知\u003c/color\u003e",'),
        ('"zh-TW":  "水： \u003ccolor=#B8A078\u003eunknown\u003c/color\u003e",', '"zh-TW":  "水： \u003ccolor=#B8A078\u003e未知\u003c/color\u003e",'),
        ('"uk":  "Вода: \u003ccolor=#B8A078\u003eunknown\u003c/color\u003e",', '"uk":  "Вода: \u003ccolor=#B8A078\u003eневідомо\u003c/color\u003e",'),
    ],
    "HavensRespec/Localization/strings.json": [
        ('"uk":  "{0:N0} gold",', '"uk":  "{0:N0} золота",'),
        ('"uk":  "{0:N0} tickets",', '"uk":  "{0:N0} квитків",'),
        ('"uk":  "Reset"', '"uk":  "Скинути"'),  # in respec.dialog.confirm
        ('"uk":  "Confirm reset?"', '"uk":  "Підтвердити скидання?"'),
        ('"uk":  "Reset {0}?"', '"uk":  "Скинути {0}?"'),
        ('"uk":  "Reset all professions?"', '"uk":  "Скинути всі професії?"'),
        ('"uk":  "Exploration"', '"uk":  "Дослідження"'),
    ],
    "SenpaisChest/Localization/strings.json": [
        ('"es":  "New Group",', '"es":  "Nuevo Grupo",'),
        ('"pt-BR":  "Group:",', '"pt-BR":  "Grupo:",'),
        ('"it":  "Rules: {0} | Status: {1}",', '"it":  "Regole: {0} | Stato: {1}",'),
        ('"ru":  "• Правила фильтра автосортировки. Multiple = AND.",', '"ru":  "• Правила фильтра автосортировки. Несколько = И.",'),
        ('"ko":  "• Manage Groups (* = any chars,? = one char) 에 와일드카드 패턴을 추가합니다.",', '"ko":  "• 그룹 관리 (* = 모든 문자,? = 한 문자) 에 와일드카드 패턴을 추가합니다.",'),
    ],
    "SunHavenMuseumUtilityTracker/Localization/strings.json": [
        ('"da":  "Sun Haven Museum Utility Tracker",', '"da":  "Sun Haven Museum Værktøjssporer",'),
        ('"de":  "Sun Haven Museum Utility Tracker",', '"de":  "Sun Haven Museum Dienstprogramm-Tracker",'),
        ('"fr":  "Sun Haven Museum Utility Tracker",', '"fr":  "Sun Haven Museum Outil de suivi",'),
        ('"nl":  "Sun Haven Museum Utility Tracker",', '"nl":  "Sun Haven Museum Hulpprogramma-tracker",'),
        ('"sv":  "Sun Haven Museum Utility Tracker",', '"sv":  "Sun Haven Museum Verktygsspårare",'),
    ],
    "SunhavenTodo/Localization/strings.json": [
        ('"it":  "Sticky",', '"it":  "Fissato",'),
        ('"nl":  "Sticky",', '"nl":  "Vastgeplakt",'),
        ('"sv":  "Sticky",', '"sv":  "Fastklistrad",'),
        ('"de":  "Collection",', '"de":  "Sammlung",'),
        ('"de":  "Fishing",', '"de":  "Angeln",'),
        ('"fr":  "[ ] Show Done",', '"fr":  "[ ] Afficher terminé",'),
        ('"fr":  "[v] Show Done",', '"fr":  "[v] Afficher terminé",'),
        ('"nl":  "[v] Show Done",', '"nl":  "[v] Toon voltooid",'),
        ('"es":  "Add Task",', '"es":  "Añadir tarea",'),
        ('"ru":  "Priority:",', '"ru":  "Приоритет:",'),
        ('"uk":  "(No title)",', '"uk":  "(Без назви)",'),
        ('"da":  "Todo ({0})",', '"da":  "Opgaveliste ({0})",'),
        ('"de":  "ToDo ({0})",', '"de":  "Aufgabenliste ({0})",'),
        ('"es":  "Todo ({0})",', '"es":  "Lista ({0})",'),
        ('"fr":  "Todo ({0})",', '"fr":  "À faire ({0})",'),
        ('"nl":  "Todo ({0})",', '"nl":  "Takenlijst ({0})",'),
        ('"pt":  "Todo ({0})",', '"pt":  "Lista ({0})",'),
        ('"pt-BR":  "Todo ({0})",', '"pt-BR":  "Lista ({0})",'),
        ('"ru":  "Todo ({0})",', '"ru":  "Список дел ({0})",'),
        ('"uk":  "Todo ({0})",', '"uk":  "Список завдань ({0})",'),
        ('"fr":  "Update",', '"fr":  "Mettre à jour",'),
        ('"de":  "Daily",', '"de":  "Täglich",'),
        ('"de":  "Resets",', '"de":  "Zurücksetzen:",'),
        ('"zh-CN":  "农业 Farming",', '"zh-CN":  "农业",'),
    ],
}

for filepath, replacements in fixes.items():
    with open(filepath, 'r', encoding='utf-8-sig') as f:
        content = f.read()
    
    for old, new in replacements:
        if old in content:
            content = content.replace(old, new)
            print(f"  OK: {filepath} - {old[:50]}")
        else:
            print(f"  MISSING: {filepath} - {old[:50]}")
    
    with open(filepath, 'w', encoding='utf-8-sig') as f:
        f.write(content)

print("Done.")
