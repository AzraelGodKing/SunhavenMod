$json = Get-Content 'HavenDevTools/Localization/strings.json' -Raw -Encoding UTF8 | ConvertFrom-Json
$keys = ($json | Get-Member -MemberType NoteProperty).Name | Sort-Object
for ($i = 0; $i -lt $keys.Count; $i++) {
    Write-Output "$i : $($keys[$i])"
}
