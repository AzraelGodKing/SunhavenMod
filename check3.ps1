Add-Type -AssemblyName System.Web.Extensions
$js = New-Object System.Web.Script.Serialization.JavaScriptSerializer
$data = $js.DeserializeObject((Get-Content 'HavenDevTools/Localization/strings.json' -Raw))
$langs = @('da','de','es','fr','it','ja','ko','nl','pt','pt-BR','ru','sv','zh-CN','zh-TW','uk')
$results = @()
foreach ($key in $data.Keys) {
    $en = $data[$key]['en']
    $sameLangs = @()
    foreach ($lang in $langs) {
        if ($data[$key][$lang] -eq $en) { $sameLangs += $lang }
    }
    if ($sameLangs.Count -gt 0) {
        $results += "$key ($($sameLangs.Count)): $($sameLangs -join ', ')"
    }
}
$results | Sort-Object
