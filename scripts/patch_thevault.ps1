# Fix TheVault uk and ko translations
$path = 'TheVault/Localization/strings.json'
$json = Get-Content -Raw -Encoding UTF8 $path | ConvertFrom-Json

# Ukrainian fixes
$uk = @{
    'vault.debug.disabledHint' = 'Виведення/збір заборонено в режимі налагодження. Вимкніть налагодження в Налаштуваннях, щоб використовувати їх.'
    'vault.debug.intro' = 'Сирий знімок усіх словників сховища (Сезонні, Спільнота, Ключі, Квитки, Сфери, Користувацькі).'
    'vault.deposit' = 'Внести'
    'vault.empty.category' = 'Немає предметів у цій категорії'
    'vault.empty.custom' = 'Ще немає користувацьких валют.'
    'vault.empty.keys' = 'У сховищі немає ключів.'
    'vault.empty.seasonal' = 'У сховищі немає сезонних жетонів.'
    'vault.empty.special' = 'У сховищі немає спеціальних валют.'
    'vault.hint.custom' = 'Інші моди реєструють записи за допомогою TheVault.Api.VaultIntegration.RegisterCustomCurrency (після завантаження The Vault).'
    'vault.hint.customId' = 'Використовуйте короткий ідентифікатор (a-z, 0-9, підкреслення) та необов\'язковий ідентифікатор предмета Sun Haven для виведення/автоматичного внеску.'
    'vault.hint.keys' = 'Ключі, які ви переміщуєте до сховища, з\'являються тут. Використовуйте Збір або автоматичний внесок для кожного предмета, щоб додати їх.'
    'vault.hint.seasonal' = 'Внесіть фестивальні жетони зі свого інвентарю або увімкніть автоматичний внесок для цих предметів.'
    'vault.hint.special' = 'Дублони, квитки, уламки та подібні валюти відображаються тут після внеску.'
    'vault.inBag' = 'В інвентарі (сумка): {0}'
    'vault.noDeposit' = 'Немає відповідного предмета інвентарю для цього рядка — внесок недоступний.'
    'vault.notInitialized' = '(Сховище не ініціалізовано)'
    'vault.qty' = 'Кількість:'
    'vault.row.hint' = 'Натисніть на рядок, щоб вибрати, або використовуйте кнопки швидкого виведення/внеску'
    'vault.settings.altKey' = 'Клавіша Alt:'
    'vault.settings.autoSave' = 'Зміни зберігаються автоматично.'
    'vault.settings.debug' = 'Налагодження (повний інспектор сховища)'
    'vault.settings.display' = 'Дисплей'
    'vault.settings.hud' = 'HUD'
    'vault.settings.hudDensity' = 'Щільність HUD:'
    'vault.settings.hudDragHint' = 'Перетягніть панель валюти за верхню смужку, щоб перемістити її; позиція зберігається в конфігурації (як у Sun Haven Todo). Зміна позиції [HUD] скидає власне розташування.'
    'vault.settings.hudScale' = 'Масштаб HUD:'
    'vault.settings.key' = 'Клавіша:'
    'vault.settings.openVault' = 'Відкрити сховище'
    'vault.settings.title' = '⚙  Налаштування (збережено у файл конфігурації)'
    'vault.settings.windowScale' = 'Масштаб вікна:'
    'vault.subtitle' = 'Система зберігання валюти'
    'vault.sweep' = 'Зібрати інвентар (автоматичний внесок зараз)'
    'vault.tab.debug' = '‼ Налагодження'
    'vault.tab.settings' = '⚙ Налаштування'
    'vault.title' = '✧  Сховище  ✧'
    'vault.withdraw' = 'Вивести'
}

# Korean fixes
$ko = @{
    'vault.debug.disabledHint' = '디버그 보기에서 인출/스위프가 비활성화되었습니다. 사용하려면 설정에서 디버그를 끄세요.'
    'vault.debug.intro' = '모든 금고 사전(계절, 커뮤니티, 열쇠, 티켓, 오브, 사용자 지정)의 원시 스냅샷입니다.'
    'vault.deposit' = '입금'
    'vault.empty.category' = '이 카테고리에 항목이 없습니다'
    'vault.empty.custom' = '아직 사용자 지정 통화가 없습니다.'
    'vault.empty.keys' = '금고에 저장된 열쇠가 없습니다.'
    'vault.empty.seasonal' = '금고에 계절 토큰이 없습니다.'
    'vault.empty.special' = '금고에 특수 통화가 없습니다.'
    'vault.hint.custom' = '다른 모드는 TheVault.Api.VaultIntegration.RegisterCustomCurrency를 사용하여 항목을 등록합니다(The Vault 로드 후).'
    'vault.hint.customId' = '짧은 ID(a-z, 0-9, 밑줄)와 선택적인 Sun Haven 항목 ID를 사용하여 인출/자동 입금합니다.'
    'vault.hint.keys' = '금고로 이동한 열쇠가 여기에 표시됩니다. 스위프 또는 항목별 자동 입금을 사용하여 추가하세요.'
    'vault.hint.seasonal' = '인벤토리에서 축제 토큰을 입금하거나 해당 항목에 대해 자동 입금을 활성화하세요.'
    'vault.settings.hud' = 'HUD'
}

$changed = $false
foreach ($key in $uk.Keys) {
    $old = [string]$json.$key.uk
    $new = $uk[$key]
    if ($old -ne $new) {
        $json.$key.uk = $new
        $changed = $true
        Write-Host "uk $key : '$old' -> '$new'"
    }
}
foreach ($key in $ko.Keys) {
    $old = [string]$json.$key.ko
    $new = $ko[$key]
    if ($old -ne $new) {
        $json.$key.ko = $new
        $changed = $true
        Write-Host "ko $key : '$old' -> '$new'"
    }
}

if ($changed) {
    ($json | ConvertTo-Json -Depth 6) | Set-Content -Path $path -Encoding UTF8
    Write-Host "Saved $path" -ForegroundColor Green
} else {
    Write-Host "No changes needed"
}
