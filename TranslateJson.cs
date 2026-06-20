using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

class Program
{
    static void Main()
    {
        string jsonPath = "HavenDevTools/Localization/strings.json";
        string json = File.ReadAllText(jsonPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip };
        var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json, options);

        var changes = new Dictionary<string, Dictionary<string, string>>
        {
            ["devtools.console.output"] = new() { ["da"] = "Uddata:", ["it"] = "Risultato:" },
            ["devtools.currency.orbs"] = new() { ["de"] = "Sph\u00e4ren: {0:N0}", ["nl"] = "Sferen: {0:N0}" },
            ["devtools.currency.tickets"] = new() { ["es"] = "Entradas: {0:N0}", ["fr"] = "Billets: {0:N0}", ["nl"] = "Kaartjes: {0:N0}" },
            ["devtools.museum.spawn"] = new() { ["da"] = "Fremkald{0}", ["nl"] = "Spawnen{0}" },
            ["devtools.overlay.race"] = new() { ["da"] = "Ras:" },
            ["devtools.spawn"] = new() { ["da"] = "Fremkald:", ["nl"] = "Spawnen:" },
            ["devtools.spawn.one"] = new() { ["da"] = "Fremkald 1", ["nl"] = "Spawnen 1" },
            ["azrael.almanac.title"] = new() {
                ["da"] = "Havens Almanak", ["de"] = "Havens Almanach", ["es"] = "Almanaque de Haven", ["fr"] = "Almanach de Haven",
                ["it"] = "Almanacco di Haven", ["ja"] = "\u30d8\u30a4\u30d6\u30f3\u306e\u30a2\u30eb\u30de\u30ca\u30c3\u30af", ["ko"] = "Haven\uc758 \uc54c\ub9c8\ub099", ["nl"] = "Havens Almanak",
                ["pt"] = "Almanaque de Haven", ["pt-BR"] = "Almanaque de Haven", ["ru"] = "\u0410\u043b\u044c\u043c\u0430\u043d\u0430\u0445 \u0425\u0430\u0432\u0435\u043d\u0430", ["sv"] = "Havens Almanacka",
                ["zh-CN"] = "Haven\u7684\u5e74\u9274", ["zh-TW"] = "Haven\u7684\u5e74\u945b", ["uk"] = "\u0410\u043b\u044c\u043c\u0430\u043d\u0430\u0445 \u0413\u0435\u0439\u0432\u0435\u043d\u0430"
            },
            ["azrael.birthday.title"] = new() {
                ["da"] = "F\u00f8dselsdagsp\u00e5mindelse", ["de"] = "Geburtstagserinnerung", ["es"] = "Recordatorio de Cumplea\u00f1os", ["fr"] = "Rappel d'Anniversaire",
                ["it"] = "Promemoria Compleanno", ["ja"] = "\u8a95\u751f\u65e5\u30ea\u30de\u30a4\u30f3\u30c0\u30fc", ["ko"] = "\uc0dd\uc77c \uc54c\ub9bc", ["nl"] = "Verjaardagsherinnering",
                ["pt"] = "Lembrete de Anivers\u00e1rio", ["pt-BR"] = "Lembrete de Anivers\u00e1rio", ["ru"] = "\u041d\u0430\u043f\u043e\u043c\u0438\u043d\u0430\u043d\u0438\u0435 \u043e \u0414\u043d\u0435 \u0420\u043e\u0436\u0434\u0435\u043d\u0438\u044f", ["sv"] = "F\u00f6delsedagsp\u00e5minnelse",
                ["zh-CN"] = "\u751f\u65e5\u63d0\u9192", ["zh-TW"] = "\u751f\u65e5\u63d0\u9192", ["uk"] = "\u041d\u0430\u0433\u0430\u0434\u0443\u0432\u0430\u043d\u043d\u044f \u043f\u0440\u043e \u0414\u0435\u043d\u044c \u041d\u0430\u0440\u043e\u0434\u0436\u0435\u043d\u043d\u044f"
            },
            ["azrael.birthright.title"] = new() {
                ["da"] = "Havens Birthright - Race Modifikator Tracker", ["de"] = "Havens Birthright - Rassen-Modifikator-Tracker", ["es"] = "Birthright de Haven - Rastreador de Modificadores de Raza", ["fr"] = "Birthright de Haven - Suivi des Modificateurs de Race",
                ["it"] = "Birthright di Haven - Tracciatore Modificatori Razza", ["ja"] = "\u30d8\u30a4\u30d6\u30f3\u306eBirthright - \u7a2e\u65cf\u4fee\u6b63\u30c8\u30e9\u30c3\u30ab\u30fc", ["ko"] = "Haven\uc758 Birthright - \uc885\uc871 \uc218\uc815 \ucd94\uc801\uae30", ["nl"] = "Havens Birthright - Ras Modifier Tracker",
                ["pt"] = "Birthright de Haven - Rastreador de Modificadores de Ra\u00e7a", ["pt-BR"] = "Birthright de Haven - Rastreador de Modificadores de Ra\u00e7a", ["ru"] = "Birthright \u0425\u0430\u0432\u0435\u043d\u0430 - \u041e\u0442\u0441\u043b\u0435\u0436\u0438\u0432\u0430\u043d\u0438\u0435 \u041c\u043e\u0434\u0438\u0444\u0438\u043a\u0430\u0442\u043e\u0440\u043e\u0432 \u0420\u0430\u0441\u044b", ["sv"] = "Havens Birthright - Rasmodifikatorsp\u00e5rare",
                ["zh-CN"] = "Haven\u7684Birthright - \u79cd\u65cf\u4fee\u6b63\u8ffd\u8e2a\u5668", ["zh-TW"] = "Haven\u7684Birthright - \u7a2e\u65cf\u4fee\u6b63\u8ffd\u8e64\u5668", ["uk"] = "Birthright \u0413\u0435\u0439\u0432\u0435\u043d\u0430 - \u0412\u0456\u0434\u0441\u0442\u0435\u0436\u0435\u043d\u043d\u044f \u041c\u043e\u0434\u0438\u0444\u0456\u043a\u0430\u0442\u043e\u0440\u0456\u0432 \u0420\u0430\u0441\u0438"
            },
            ["azrael.senpai.title"] = new() {
                ["da"] = "Senpais Kiste", ["de"] = "Senpais Truhe", ["es"] = "Cofre de Senpai", ["fr"] = "Coffre de Senpai",
                ["it"] = "Cassa di Senpai", ["ja"] = "\u5148\u8f29\u306e\u5b9d\u7bb1", ["ko"] = "\uc120\ubc30\uc758 \uc0c1\uacfc", ["nl"] = "Senpais Kist",
                ["pt"] = "Ba\u00fa do Senpai", ["pt-BR"] = "Ba\u00fa do Senpai", ["ru"] = "\u0421\u0443\u043d\u0434\u0443\u043a \u0421\u0435\u043d\u043f\u0430\u044f", ["sv"] = "Senpais Kista",
                ["zh-CN"] = "Senpai\u7684\u7bb1\u5b50", ["zh-TW"] = "Senpai\u7684\u7bb1\u5b50", ["uk"] = "\u0421\u043a\u0440\u0438\u043d\u044f \u0421\u0435\u043d\u043f\u0430\u044f"
            },
            ["azrael.tab.birthright"] = new() {
                ["da"] = "Birthright", ["de"] = "Birthright", ["es"] = "Birthright", ["fr"] = "Birthright",
                ["it"] = "Birthright", ["ja"] = "Birthright", ["ko"] = "Birthright", ["nl"] = "Birthright",
                ["pt"] = "Birthright", ["pt-BR"] = "Birthright", ["ru"] = "Birthright", ["sv"] = "Birthright",
                ["zh-CN"] = "Birthright", ["zh-TW"] = "Birthright", ["uk"] = "Birthright"
            },
            ["azrael.tab.senpais_chest"] = new() {
                ["da"] = "Senpais Kiste", ["de"] = "Senpais Truhe", ["es"] = "Cofre de Senpai", ["fr"] = "Coffre de Senpai",
                ["it"] = "Cassa di Senpai", ["ja"] = "\u5148\u8f29\u306e\u5b9d\u7bb1", ["ko"] = "\uc120\ubc30\uc758 \uc0c1\uacfc", ["nl"] = "Senpais Kist",
                ["pt"] = "Ba\u00fa do Senpai", ["pt-BR"] = "Ba\u00fa do Senpai", ["ru"] = "\u0421\u0443\u043d\u0434\u0443\u043a \u0421\u0435\u043d\u043f\u0430\u044f", ["sv"] = "Senpais Kista",
                ["zh-CN"] = "Senpai\u7684\u7bb1\u5b50", ["zh-TW"] = "Senpai\u7684\u7bb1\u5b50", ["uk"] = "\u0421\u043a\u0440\u0438\u043d\u044f \u0421\u0435\u043d\u043f\u0430\u044f"
            },
            ["azrael.tab.smut"] = new() {
                ["da"] = "S.M.U.T.", ["de"] = "S.M.U.T.", ["es"] = "S.M.U.T.", ["fr"] = "S.M.U.T.",
                ["it"] = "S.M.U.T.", ["ja"] = "S.M.U.T.", ["ko"] = "S.M.U.T.", ["nl"] = "S.M.U.T.",
                ["pt"] = "S.M.U.T.", ["pt-BR"] = "S.M.U.T.", ["ru"] = "S.M.U.T.", ["sv"] = "S.M.U.T.",
                ["zh-CN"] = "S.M.U.T.", ["zh-TW"] = "S.M.U.T.", ["uk"] = "S.M.U.T."
            },
            ["azrael.tab.the_vault"] = new() {
                ["da"] = "Kisten", ["de"] = "Der Tresor", ["es"] = "La B\u00f3veda", ["fr"] = "Le Vault",
                ["it"] = "La Cassaforte", ["ja"] = "\u30b6\u30fb\u30f4\u30a9\u30eb\u30c8", ["ko"] = "\ubcfc\ud2b8", ["nl"] = "De Kluis",
                ["pt"] = "O Cofre", ["pt-BR"] = "O Cofre", ["ru"] = "\u0425\u0440\u0430\u043d\u0438\u043b\u0438\u0449\u0435", ["sv"] = "Kistan",
                ["zh-CN"] = "\u5b9d\u5e93", ["zh-TW"] = "\u91d1\u5eab", ["uk"] = "\u0421\u0445\u043e\u0432\u0438\u0449\u0435"
            },
            ["azrael.tab.trinket_fortune"] = new() {
                ["da"] = "Trinket Fortune", ["de"] = "Trinket Fortune", ["es"] = "Trinket Fortune", ["fr"] = "Trinket Fortune",
                ["it"] = "Trinket Fortune", ["ja"] = "Trinket Fortune", ["ko"] = "Trinket Fortune", ["nl"] = "Trinket Fortune",
                ["pt"] = "Trinket Fortune", ["pt-BR"] = "Trinket Fortune", ["ru"] = "Trinket Fortune", ["sv"] = "Trinket Fortune",
                ["zh-CN"] = "Trinket Fortune", ["zh-TW"] = "Trinket Fortune", ["uk"] = "Trinket Fortune"
            },
            ["azrael.todo.title"] = new() {
                ["da"] = "Sunhaven Opgaver", ["de"] = "Sunhaven Aufgaben", ["es"] = "Sunhaven Tareas", ["fr"] = "Sunhaven T\u00e2ches",
                ["it"] = "Sunhaven Compiti", ["ja"] = "Sunhaven \u30bf\u30b9\u30af", ["ko"] = "Sunhaven \ud560 \uc77c", ["nl"] = "Sunhaven Taken",
                ["pt"] = "Sunhaven Tarefas", ["pt-BR"] = "Sunhaven Tarefas", ["ru"] = "Sunhaven \u0417\u0430\u0434\u0430\u0447\u0438", ["sv"] = "Sunhaven Att G\u00f6ra",
                ["zh-CN"] = "Sunhaven \u5f85\u529e", ["zh-TW"] = "Sunhaven \u5f85\u8fa6", ["uk"] = "Sunhaven \u0417\u0430\u0432\u0434\u0430\u043d\u043d\u044f"
            },
            ["azrael.vault.title"] = new() {
                ["da"] = "Kisten", ["de"] = "Der Tresor", ["es"] = "La B\u00f3veda", ["fr"] = "Le Vault",
                ["it"] = "La Cassaforte", ["ja"] = "\u30b6\u30fb\u30f4\u30a9\u30eb\u30c8", ["ko"] = "\ubcfc\ud2b8", ["nl"] = "De Kluis",
                ["pt"] = "O Cofre", ["pt-BR"] = "O Cofre", ["ru"] = "\u0425\u0440\u0430\u043d\u0438\u043b\u0438\u0449\u0435", ["sv"] = "Kistan",
                ["zh-CN"] = "\u5b9d\u5e93", ["zh-TW"] = "\u91d1\u5eab", ["uk"] = "\u0421\u0445\u043e\u0432\u0438\u0449\u0435"
            },
            ["devtools.mod.birthright"] = new() {
                ["da"] = "Birthright: {0}", ["de"] = "Birthright: {0}", ["es"] = "Birthright: {0}", ["fr"] = "Birthright: {0}",
                ["it"] = "Birthright: {0}", ["ja"] = "Birthright: {0}", ["ko"] = "Birthright: {0}", ["nl"] = "Birthright: {0}",
                ["pt"] = "Birthright: {0}", ["pt-BR"] = "Birthright: {0}", ["ru"] = "Birthright: {0}", ["sv"] = "Birthright: {0}",
                ["zh-CN"] = "Birthright: {0}", ["zh-TW"] = "Birthright: {0}", ["uk"] = "Birthright: {0}"
            },
            ["devtools.mod.senpai"] = new() {
                ["da"] = "Senpai: {0}", ["de"] = "Senpai: {0}", ["es"] = "Senpai: {0}", ["fr"] = "Senpai: {0}",
                ["it"] = "Senpai: {0}", ["ja"] = "\u5148\u8f29: {0}", ["ko"] = "\uc120\ubc30: {0}", ["nl"] = "Senpai: {0}",
                ["pt"] = "Senpai: {0}", ["pt-BR"] = "Senpai: {0}", ["ru"] = "\u0421\u0435\u043d\u043f\u0430\u0439: {0}", ["sv"] = "Senpai: {0}",
                ["zh-CN"] = "Senpai: {0}", ["zh-TW"] = "Senpai: {0}", ["uk"] = "\u0421\u0435\u043d\u043f\u0430\u0439: {0}"
            },
            ["devtools.mod.smut"] = new() {
                ["da"] = "S.M.U.T.: {0}", ["de"] = "S.M.U.T.: {0}", ["es"] = "S.M.U.T.: {0}", ["fr"] = "S.M.U.T.: {0}",
                ["it"] = "S.M.U.T.: {0}", ["ja"] = "S.M.U.T.: {0}", ["ko"] = "S.M.U.T.: {0}", ["nl"] = "S.M.U.T.: {0}",
                ["pt"] = "S.M.U.T.: {0}", ["pt-BR"] = "S.M.U.T.: {0}", ["ru"] = "S.M.U.T.: {0}", ["sv"] = "S.M.U.T.: {0}",
                ["zh-CN"] = "S.M.U.T.: {0}", ["zh-TW"] = "S.M.U.T.: {0}", ["uk"] = "S.M.U.T.: {0}"
            },
            ["devtools.mod.thevault"] = new() {
                ["da"] = "Kisten: {0}", ["de"] = "Tresor: {0}", ["es"] = "B\u00f3veda: {0}", ["fr"] = "Vault: {0}",
                ["it"] = "Cassaforte: {0}", ["ja"] = "\u30f4\u30a9\u30eb\u30c8: {0}", ["ko"] = "\ubcfc\ud2b8: {0}", ["nl"] = "Kluis: {0}",
                ["pt"] = "Cofre: {0}", ["pt-BR"] = "Cofre: {0}", ["ru"] = "\u0425\u0440\u0430\u043d\u0438\u043b\u0438\u0449\u0435: {0}", ["sv"] = "Kistan: {0}",
                ["zh-CN"] = "\u5b9d\u5e93: {0}", ["zh-TW"] = "\u91d1\u5eab: {0}", ["uk"] = "\u0421\u0445\u043e\u0432\u0438\u0449\u0435: {0}"
            },
            ["devtools.mod.todo"] = new() {
                ["da"] = "Opgaver: {0}", ["de"] = "Aufgaben: {0}", ["es"] = "Tareas: {0}", ["fr"] = "T\u00e2ches: {0}",
                ["it"] = "Compiti: {0}", ["ja"] = "\u30bf\u30b9\u30af: {0}", ["ko"] = "\ud560 \uc77c: {0}", ["nl"] = "Taken: {0}",
                ["pt"] = "Tarefas: {0}", ["pt-BR"] = "Tarefas: {0}", ["ru"] = "\u0417\u0430\u0434\u0430\u0447\u0438: {0}", ["sv"] = "Att G\u00f6ra: {0}",
                ["zh-CN"] = "\u5f85\u529e: {0}", ["zh-TW"] = "\u5f85\u8fa6: {0}", ["uk"] = "\u0417\u0430\u0432\u0434\u0430\u043d\u043d\u044f: {0}"
            },
        };

        foreach (var key in changes.Keys)
        {
            if (!data.ContainsKey(key)) { Console.WriteLine($"WARNING: Key not found: {key}"); continue; }
            string en = data[key]["en"];
            foreach (var lang in changes[key].Keys)
            {
                string current = data[key][lang];
                if (current == en)
                {
                    data[key][lang] = changes[key][lang];
                    Console.WriteLine($"Changed {key} [{lang}]: '{current}' -> '{changes[key][lang]}'");
                }
                else
                {
                    Console.WriteLine($"Skipped {key} [{lang}]: already different ('{current}')");
                }
            }
        }

        var writeOptions = new JsonSerializerOptions { WriteIndented = true };
        string newJson = JsonSerializer.Serialize(data, writeOptions);
        File.WriteAllText(jsonPath, newJson);
        Console.WriteLine("Done.");
    }
}
