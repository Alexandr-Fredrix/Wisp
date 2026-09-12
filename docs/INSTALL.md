# Wisp 1.1.0 — установка / installation

Windows x64 · Hollow Knight 1.5.12620 · BepInEx 5.4.23.5 x64.

## Русский

1. Закрой игру. Установи BepInEx 5 x64 рядом с `hollow_knight.exe`, один раз запусти и закрой игру.
2. Распакуй `Wisp-1.1.0.zip` в папку игры. Проверь `BepInEx/plugins/Wisp/Wisp.dll`.
3. Загрузи сохранение, нажми **F8** или **оба стика**, выбери язык и прохождение в настройках.

**Обновление:** замени старую DLL при закрытой игре. Храни резервные DLL вне `BepInEx/plugins`, чтобы не загрузить две версии сразу. Отметки 1.0.x совместимы.

**Удаление:** убери папку `BepInEx/plugins/Wisp`. Сейвы и другие моды не затрагиваются.

**Окно не открывается:** проверь BepInEx 5 x64, путь DLL и загрузку сохранения. Это BepInEx-плагин, не пакет Modding API/Scarab. Сообщая об ошибке, укажи версии игры/Wisp и шаги воспроизведения.

**Картинка не загрузилась:** нажми сообщение об ошибке, **R** или **X** в соответствующей панели. Атлас и коллекции встроены; части дневника нужен первый доступ к сети.

Настройки и отметки: `%USERPROFILE%/AppData/LocalLow/Team Cherry/Hollow Knight/Wisp`; изображения — в соседней `WispCache`. Отметки слотов записываются после успешного сохранения игры. При невозможности восстановления Wisp сообщает об этом и блокирует перезапись повреждённых данных. Смена языка сохраняет прогресс.

## English

1. Close the game. Install BepInEx 5 x64 beside `hollow_knight.exe`, then launch and close the game once.
2. Extract `Wisp-1.1.0.zip` into the game directory. Verify `BepInEx/plugins/Wisp/Wisp.dll`.
3. Load a save, press **F8** or **both sticks**, then select language and walkthrough in Settings.

**Update:** replace the old DLL with the game closed. Keep backups outside `BepInEx/plugins` to avoid duplicate versions. Marks from 1.0.x remain compatible.

**Uninstall:** remove `BepInEx/plugins/Wisp`; game saves and other mods stay untouched.

**Window missing:** verify BepInEx 5 x64, DLL path and that a save is loaded. This is a BepInEx plugin, not a Modding API/Scarab package. Include game/mod versions and reproduction steps in bug reports.

**Image failed:** click the error, press **R**, or use **X** on the affected panel. Atlas and collection maps are embedded; some Journal images need internet on first use.

Settings/marks: `%USERPROFILE%/AppData/LocalLow/Team Cherry/Hollow Knight/Wisp`; images use the adjacent `WispCache`. Marks are written after a successful game save. If recovery fails, Wisp reports it and prevents overwriting damaged data. Language changes preserve progress.

BepInEx and game libraries are not included. BepInEx download: https://github.com/BepInEx/BepInEx/releases/tag/v5.4.23.5.
