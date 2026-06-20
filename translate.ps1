Add-Type -AssemblyName System.Web.Extensions
$js = New-Object System.Web.Script.Serialization.JavaScriptSerializer
$json = Get-Content 'HavenDevTools/Localization/strings.json' -Raw
$data = $js.DeserializeObject($json)

# Define all changes: key -> lang -> newValue
$changes = @{
    "devtools.console.output" = @{ "da" = "Uddata:"; "it" = "Risultato:" }
    "devtools.currency.orbs" = @{ "de" = "Sphären: {0:N0}"; "nl" = "Sferen: {0:N0}" }
    "devtools.currency.tickets" = @{ "es" = "Entradas: {0:N0}"; "fr" = "Billets: {0:N0}"; "nl" = "Kaartjes: {0:N0}" }
    "devtools.museum.spawn" = @{ "da" = "Fremkald{0}"; "nl" = "Spawnen{0}" }
    "devtools.overlay.race" = @{ "da" = "Ras:" }
    "devtools.spawn" = @{ "da" = "Fremkald:"; "nl" = "Spawnen:" }
    "devtools.spawn.one" = @{ "da" = "Fremkald 1"; "nl" = "Spawnen 1" }
    # Completely untranslated mod titles
    "azrael.almanac.title" = @{
        "da" = "Havens Almanak"; "de" = "Havens Almanach"; "es" = "Almanaque de Haven"; "fr" = "Almanach de Haven"
        "it" = "Almanacco di Haven"; "ja" = "ヘイブンのアルマナック"; "ko" = "Haven의 알마낙"; "nl" = "Havens Almanak"
        "pt" = "Almanaque de Haven"; "pt-BR" = "Almanaque de Haven"; "ru" = "Альманах Хавена"; "sv" = "Havens Almanacka"
        "zh-CN" = "Haven的年鉴"; "zh-TW" = "Haven的年鑑"; "uk" = "Альманах Гейвена"
    }
    "azrael.birthday.title" = @{
        "da" = "Fødselsdagspåmindelse"; "de" = "Geburtstagserinnerung"; "es" = "Recordatorio de Cumpleaños"; "fr" = "Rappel d'Anniversaire"
        "it" = "Promemoria Compleanno"; "ja" = "誕生日リマインダー"; "ko" = "생일 알림"; "nl" = "Verjaardagsherinnering"
        "pt" = "Lembrete de Aniversário"; "pt-BR" = "Lembrete de Aniversário"; "ru" = "Напоминание о Дне Рождения"; "sv" = "Födelsedagspåminnelse"
        "zh-CN" = "生日提醒"; "zh-TW" = "生日提醒"; "uk" = "Нагадування про День Народження"
    }
    "azrael.birthright.title" = @{
        "da" = "Havens Birthright - Race Modifikator Tracker"; "de" = "Havens Birthright - Rassen-Modifikator-Tracker"; "es" = "Birthright de Haven - Rastreador de Modificadores de Raza"; "fr" = "Birthright de Haven - Suivi des Modificateurs de Race"
        "it" = "Birthright di Haven - Tracciatore Modificatori Razza"; "ja" = "ヘイブンのBirthright - 種族修正トラッカー"; "ko" = "Haven의 Birthright - 종족 수정 추적기"; "nl" = "Havens Birthright - Ras Modifier Tracker"
        "pt" = "Birthright de Haven - Rastreador de Modificadores de Raça"; "pt-BR" = "Birthright de Haven - Rastreador de Modificadores de Raça"; "ru" = "Birthright Хавена - Отслеживание Модификаторов Расы"; "sv" = "Havens Birthright - Rasmodifikatorspårare"
        "zh-CN" = "Haven的Birthright - 种族修正追踪器"; "zh-TW" = "Haven的Birthright - 種族修正追蹤器"; "uk" = "Birthright Гейвена - Відстеження Модифікаторів Раси"
    }
    "azrael.senpai.title" = @{
        "da" = "Senpais Kiste"; "de" = "Senpais Truhe"; "es" = "Cofre de Senpai"; "fr" = "Coffre de Senpai"
        "it" = "Cassa di Senpai"; "ja" = "先輩の宝箱"; "ko" = "선배의 상자"; "nl" = "Senpais Kist"
        "pt" = "Baú do Senpai"; "pt-BR" = "Baú do Senpai"; "ru" = "Сундук Сенпая"; "sv" = "Senpais Kista"
        "zh-CN" = "Senpai的箱子"; "zh-TW" = "Senpai的箱子"; "uk" = "Скриня Сенпая"
    }
    "azrael.tab.birthright" = @{
        "da" = "Birthright"; "de" = "Birthright"; "es" = "Birthright"; "fr" = "Birthright"
        "it" = "Birthright"; "ja" = "Birthright"; "ko" = "Birthright"; "nl" = "Birthright"
        "pt" = "Birthright"; "pt-BR" = "Birthright"; "ru" = "Birthright"; "sv" = "Birthright"
        "zh-CN" = "Birthright"; "zh-TW" = "Birthright"; "uk" = "Birthright"
    }
    "azrael.tab.senpais_chest" = @{
        "da" = "Senpais Kiste"; "de" = "Senpais Truhe"; "es" = "Cofre de Senpai"; "fr" = "Coffre de Senpai"
        "it" = "Cassa di Senpai"; "ja" = "先輩の宝箱"; "ko" = "선배의 상자"; "nl" = "Senpais Kist"
        "pt" = "Baú do Senpai"; "pt-BR" = "Baú do Senpai"; "ru" = "Сундук Сенпая"; "sv" = "Senpais Kista"
        "zh-CN" = "Senpai的箱子"; "zh-TW" = "Senpai的箱子"; "uk" = "Скриня Сенпая"
    }
    "azrael.tab.smut" = @{
        "da" = "S.M.U.T."; "de" = "S.M.U.T."; "es" = "S.M.U.T."; "fr" = "S.M.U.T."
        "it" = "S.M.U.T."; "ja" = "S.M.U.T."; "ko" = "S.M.U.T."; "nl" = "S.M.U.T."
        "pt" = "S.M.U.T."; "pt-BR" = "S.M.U.T."; "ru" = "S.M.U.T."; "sv" = "S.M.U.T."
        "zh-CN" = "S.M.U.T."; "zh-TW" = "S.M.U.T."; "uk" = "S.M.U.T."
    }
    "azrael.tab.the_vault" = @{
        "da" = "Kisten"; "de" = "Der Tresor"; "es" = "La Bóveda"; "fr" = "Le Vault"
        "it" = "La Cassaforte"; "ja" = "ザ・ヴォルト"; "ko" = "볼트"; "nl" = "De Kluis"
        "pt" = "O Cofre"; "pt-BR" = "O Cofre"; "ru" = "Хранилище"; "sv" = "Kistan"
        "zh-CN" = "宝库"; "zh-TW" = "金庫"; "uk" = "Сховище"
    }
    "azrael.tab.trinket_fortune" = @{
        "da" = "Trinket Fortune"; "de" = "Trinket Fortune"; "es" = "Trinket Fortune"; "fr" = "Trinket Fortune"
        "it" = "Trinket Fortune"; "ja" = "Trinket Fortune"; "ko" = "Trinket Fortune"; "nl" = "Trinket Fortune"
        "pt" = "Trinket Fortune"; "pt-BR" = "Trinket Fortune"; "ru" = "Trinket Fortune"; "sv" = "Trinket Fortune"
        "zh-CN" = "Trinket Fortune"; "zh-TW" = "Trinket Fortune"; "uk" = "Trinket Fortune"
    }
    "azrael.todo.title" = @{
        "da" = "Sunhaven Opgaver"; "de" = "Sunhaven Aufgaben"; "es" = "Sunhaven Tareas"; "fr" = "Sunhaven Tâches"
        "it" = "Sunhaven Compiti"; "ja" = "Sunhaven タスク"; "ko" = "Sunhaven 할 일"; "nl" = "Sunhaven Taken"
        "pt" = "Sunhaven Tarefas"; "pt-BR" = "Sunhaven Tarefas"; "ru" = "Sunhaven Задачи"; "sv" = "Sunhaven Att Göra"
        "zh-CN" = "Sunhaven 待办"; "zh-TW" = "Sunhaven 待辦"; "uk" = "Sunhaven Завдання"
    }
    "azrael.vault.title" = @{
        "da" = "Kisten"; "de" = "Der Tresor"; "es" = "La Bóveda"; "fr" = "Le Vault"
        "it" = "La Cassaforte"; "ja" = "ザ・ヴォルト"; "ko" = "볼트"; "nl" = "De Kluis"
        "pt" = "O Cofre"; "pt-BR" = "O Cofre"; "ru" = "Хранилище"; "sv" = "Kistan"
        "zh-CN" = "宝库"; "zh-TW" = "金庫"; "uk" = "Сховище"
    }
    "devtools.mod.birthright" = @{
        "da" = "Birthright: {0}"; "de" = "Birthright: {0}"; "es" = "Birthright: {0}"; "fr" = "Birthright: {0}"
        "it" = "Birthright: {0}"; "ja" = "Birthright: {0}"; "ko" = "Birthright: {0}"; "nl" = "Birthright: {0}"
        "pt" = "Birthright: {0}"; "pt-BR" = "Birthright: {0}"; "ru" = "Birthright: {0}"; "sv" = "Birthright: {0}"
        "zh-CN" = "Birthright: {0}"; "zh-TW" = "Birthright: {0}"; "uk" = "Birthright: {0}"
    }
    "devtools.mod.senpai" = @{
        "da" = "Senpai: {0}"; "de" = "Senpai: {0}"; "es" = "Senpai: {0}"; "fr" = "Senpai: {0}"
        "it" = "Senpai: {0}"; "ja" = "先輩: {0}"; "ko" = "선배: {0}"; "nl" = "Senpai: {0}"
        "pt" = "Senpai: {0}"; "pt-BR" = "Senpai: {0}"; "ru" = "Сенпай: {0}"; "sv" = "Senpai: {0}"
        "zh-CN" = "Senpai: {0}"; "zh-TW" = "Senpai: {0}"; "uk" = "Сенпай: {0}"
    }
    "devtools.mod.smut" = @{
        "da" = "S.M.U.T.: {0}"; "de" = "S.M.U.T.: {0}"; "es" = "S.M.U.T.: {0}"; "fr" = "S.M.U.T.: {0}"
        "it" = "S.M.U.T.: {0}"; "ja" = "S.M.U.T.: {0}"; "ko" = "S.M.U.T.: {0}"; "nl" = "S.M.U.T.: {0}"
        "pt" = "S.M.U.T.: {0}"; "pt-BR" = "S.M.U.T.: {0}"; "ru" = "S.M.U.T.: {0}"; "sv" = "S.M.U.T.: {0}"
        "zh-CN" = "S.M.U.T.: {0}"; "zh-TW" = "S.M.U.T.: {0}"; "uk" = "S.M.U.T.: {0}"
    }
    "devtools.mod.thevault" = @{
        "da" = "Kisten: {0}"; "de" = "Tresor: {0}"; "es" = "Bóveda: {0}"; "fr" = "Vault: {0}"
        "it" = "Cassaforte: {0}"; "ja" = "ヴォルト: {0}"; "ko" = "볼트: {0}"; "nl" = "Kluis: {0}"
        "pt" = "Cofre: {0}"; "pt-BR" = "Cofre: {0}"; "ru" = "Хранилище: {0}"; "sv" = "Kistan: {0}"
        "zh-CN" = "宝库: {0}"; "zh-TW" = "金庫: {0}"; "uk" = "Сховище: {0}"
    }
    "devtools.mod.todo" = @{
        "da" = "Opgaver: {0}"; "de" = "Aufgaben: {0}"; "es" = "Tareas: {0}"; "fr" = "Tâches: {0}"
        "it" = "Compiti: {0}"; "ja" = "タスク: {0}"; "ko" = "할 일: {0}"; "nl" = "Taken: {0}"
        "pt" = "Tarefas: {0}"; "pt-BR" = "Tarefas: {0}"; "ru" = "Задачи: {0}"; "sv" = "Att Göra: {0}"
        "zh-CN" = "待办: {0}"; "zh-TW" = "待辦: {0}"; "uk" = "Завдання: {0}"
    }
}

# Apply changes only where current value equals English
foreach ($key in $changes.Keys) {
    if (-not $data.ContainsKey($key)) { Write-Host "WARNING: Key not found: $key"; continue }
    $en = $data[$key]["en"]
    foreach ($lang in $changes[$key].Keys) {
        $current = $data[$key][$lang]
        if ($current -eq $en) {
            $data[$key][$lang] = $changes[$key][$lang]
            Write-Host "Changed $key [$lang]: '$current' -> '$($changes[$key][$lang])'"
        } else {
            Write-Host "Skipped $key [$lang]: already different ('$current')"
        }
    }
}

# Serialize using Newtonsoft.Json if available for proper formatting
try {
    Add-Type -Path "C:\Program Files\dotnet\sdk\*\Newtonsoft.Json.dll" -ErrorAction Stop
} catch {
    try {
        $ NewtonsoftPath = [System.AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.FullName -like "*Newtonsoft.Json*" } | Select-Object -First 1 -ExpandProperty Location
        if ($NewtonsoftPath) { Add-Type -Path $NewtonsoftPath }
    } catch {}
}

if ("Newtonsoft.Json.JsonConvert" -as [type]) {
    $settings = New-Object Newtonsoft.Json.JsonSerializerSettings
    $settings.Formatting = [Newtonsoft.Json.Formatting]::Indented
    $newJson = [Newtonsoft.Json.JsonConvert]::SerializeObject($data, $settings)
} else {
    # Use JavaScriptSerializer but fix formatting
    $newJson = $js.Serialize($data)
    # Pretty print manually
    $newJson = $newJson.Replace('{', "`n{").Replace('}', "`n}").Replace('","', '", "').Replace('":', '": ')
}

Set-Content -Path 'HavenDevTools/Localization/strings.json' -Value $newJson -Encoding UTF8
Write-Host "Done."
