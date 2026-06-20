Add-Type -AssemblyName System.Web.Extensions
$js = New-Object System.Web.Script.Serialization.JavaScriptSerializer
$data = $js.DeserializeObject((Get-Content 'HavenDevTools/Localization/strings.json' -Raw))
$langs = @('da','de','es','fr','it','ja','ko','nl','pt','pt-BR','ru','sv','zh-CN','zh-TW','uk')
$required = @('azrael.almanac.refresh','azrael.birthday.check','azrael.birthday.refresh','azrael.birthday.test_notify','azrael.birthright.active_bonuses','azrael.senpai.trigger_scan','azrael.tab.almanac','azrael.tab.birthday','azrael.tab.todo','azrael.todo.save','azrael.todo.toggle_hud','azrael.todo.toggle_ui','azrael.trinket.not_loaded','azrael.vault.currencies','azrael.vault.empty','azrael.vault.inspector_toggle','devtools.auth.hint','devtools.auth.logHash','devtools.auth.title','devtools.clear','devtools.close','devtools.config','devtools.config.reload','devtools.console.execute','devtools.console.init','devtools.console.output','devtools.console.title','devtools.currency.gold','devtools.currency.inventory','devtools.currency.none','devtools.currency.orbs','devtools.currency.tickets','devtools.currency.title','devtools.currency.unavailable','devtools.currency.vault','devtools.currency.vaultEmpty','devtools.currency.vaultMissing','devtools.extensions','devtools.extensions.none','devtools.items.held','devtools.items.heldNone','devtools.items.notFound','devtools.items.title','devtools.log.export','devtools.log.init','devtools.log.level','devtools.log.title','devtools.logCurrencyIds','devtools.logging','devtools.logPlayerStats','devtools.museum.bundle','devtools.museum.character','devtools.museum.idInAssets','devtools.museum.itemsIn','devtools.museum.noData','devtools.museum.notLoaded','devtools.museum.progress','devtools.museum.section','devtools.museum.spawn','devtools.museum.title','devtools.museum.unavailable','devtools.no','devtools.overlay.gold','devtools.overlay.held','devtools.overlay.keys','devtools.overlay.player','devtools.overlay.position','devtools.overlay.race','devtools.perf.memory','devtools.perf.overlayHint','devtools.perf.title','devtools.player','devtools.race.birthrightMissing','devtools.race.bonuses','devtools.race.browse','devtools.race.current','devtools.race.noBonuses','devtools.race.noDefined','devtools.race.title','devtools.race.unavailable','devtools.search','devtools.selected','devtools.spawn','devtools.spawn.one','devtools.spawn.qty','devtools.spawn.toInventory','devtools.tab.azraels_mods','devtools.tab.console','devtools.tab.currencies','devtools.tab.extensions','devtools.tab.items','devtools.tab.log','devtools.tab.perf','devtools.tab.tools','devtools.tab.utility','devtools.title','devtools.utility','devtools.versions.checkAll','devtools.versions.checking','devtools.versions.notChecked','devtools.versions.testNotify','devtools.versions.title','devtools.versions.update','devtools.versions.upToDate','devtools.yes')
$whitelist = @('azrael.none_detected','azrael.smut.title','azrael.vault.inspector_hint','devtools.console.hint','devtools.overlay.keys','devtools.overlay.title','devtools.tab.azraels_mods','devtools.title')
foreach ($key in $required) {
    if (-not $data.ContainsKey($key)) { continue }
    if ($whitelist -contains $key) { continue }
    $en = $data[$key]['en']
    $allSame = $true
    foreach ($lang in $langs) {
        if ($data[$key][$lang] -ne $en) { $allSame = $false; break }
    }
    if ($allSame) {
        Write-Host "$key = '$en' (ALL languages same as en)"
    }
}
