# Adds SunhavenModUiProject, shared loc sources, and embedded strings.json to UI mod csprojs.
$ErrorActionPreference = "Stop"
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

$mods = @(
    @{ Folder = "BirthdayReminder"; Guid = "com.azraelgodking.squirrelsbirthdayreminder" },
    @{ Folder = "CropOptimizer"; Guid = "com.azraelgodking.cropoptimizer" },
    @{ Folder = "SenpaisChest"; Guid = "com.azraelgodking.senpaischest" },
    @{ Folder = "TheVault"; Guid = "com.azraelgodking.thevault" },
    @{ Folder = "HavensAlmanac"; Guid = "com.azraelgodking.havensalmanac" },
    @{ Folder = "SunHavenMuseumUtilityTracker"; Guid = "com.azraelgodking.sunhavenmuseumutilitytracker" },
    @{ Folder = "HavenDevTools"; Guid = "com.azraelgodking.havendevtools" },
    @{ Folder = "HavensRespec"; Guid = "com.azraelgodking.havensrespec" }
)

foreach ($m in $mods) {
    $path = Join-Path $RepoRoot $m.Folder "$($m.Folder).csproj"
    if (-not (Test-Path $path)) {
        $path = Get-ChildItem (Join-Path $RepoRoot $m.Folder) -Filter "*.csproj" | Select-Object -First 1 -ExpandProperty FullName
    }
    if (-not $path) { Write-Warning "No csproj for $($m.Folder)"; continue }

    $xml = Get-Content -Raw $path
    if ($xml -notmatch "SunhavenModUiProject") {
        $xml = $xml -replace "(<PropertyGroup>\s*\r?\n\s*<TargetFramework>)", "`$1`r`n    <SunhavenModUiProject>true</SunhavenModUiProject>`r`n    <!-- was: TargetFramework - fix -->"
        # simpler: insert after first PropertyGroup opening that has TargetFramework
        $xml = $xml -replace "(<TargetFramework>net48</TargetFramework>)", "`$1`r`n    <SunhavenModUiProject>true</SunhavenModUiProject>"
    }
    if ($xml -notmatch "ModLocalization.cs") {
        $xml = $xml -replace "(</ItemGroup>\s*\r?\n\s*<!-- Copy to BepInEx)", @"
    <Compile Include="..\SharedUtilities\ModLocalization.cs" Link="Shared\ModLocalization.cs" />
    <Compile Include="..\SharedUtilities\LanguageChangeWatcher.cs" Link="Shared\LanguageChangeWatcher.cs" />
  </ItemGroup>

  <ItemGroup>
    <EmbeddedResource Include="Localization\strings.json">
      <LogicalName>$($m.Guid).Localization.strings.json</LogicalName>
    </EmbeddedResource>
  </ItemGroup>

  <!-- Copy to BepInEx
"@
        if ($xml -notmatch "ModLocalization.cs") {
            $xml = $xml -replace "(  </ItemGroup>\r?\n\r?\n  <!-- Copy)", @"
    <Compile Include="..\SharedUtilities\ModLocalization.cs" Link="Shared\ModLocalization.cs" />
    <Compile Include="..\SharedUtilities\LanguageChangeWatcher.cs" Link="Shared\LanguageChangeWatcher.cs" />
  </ItemGroup>

  <ItemGroup>
    <EmbeddedResource Include="Localization\strings.json">
      <LogicalName>$($m.Guid).Localization.strings.json</LogicalName>
    </EmbeddedResource>
  </ItemGroup>

  <!-- Copy
"@
        }
    }
    Set-Content -Path $path -Value $xml -Encoding UTF8 -NoNewline
    Write-Host "Updated $path"
}
