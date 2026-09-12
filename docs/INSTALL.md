> **1.0.0 — основной выпуск; 1.0.1 — предварительный выпуск для проверки.** Для установки 1.0.1 используйте `Wisp-1.0.1.zip` вместо указанного ниже архива 1.0.0. Порядок установки одинаков; сохраните старую DLL вне папки плагинов. Игровые проверки 1.0.1 ещё предстоят.

# Установка / Installation

Windows x64 · Hollow Knight 1.5.12620 · BepInEx 5.4.23.5 x64.

## Русский

1. Закройте игру. Перед первым изменением модов сохраните копию папки `%USERPROFILE%/AppData/LocalLow/Team Cherry/Hollow Knight`.
2. Установите BepInEx 5 x64 в папку с hollow_knight.exe. Старый Modding API одновременно не используется.
3. Распакуйте Wisp-1.0.0.zip в папку игры. Должен появиться `BepInEx/plugins/Wisp/Wisp.dll`.
4. Загрузите сейв, нажмите F8 или оба стика. В Настройках выберите цель и язык.

Мышь выбирает пункты; колесо прокручивает описание или приближает карту, перетаскивание двигает карту. На контроллере LB/RB меняют главные вкладки, стрелки выбирают панели и пункты, A открывает, B возвращает. Правый стик прокручивает описание. Следуйте подсказкам внизу: действия X/Y и LT/RT зависят от текущей панели. В дневнике X открывает выбор локации. В галерее Y листает иллюстрации, нажатие RS разворачивает их; в развёрнутом просмотре стики двигают карту, LT/RT меняют масштаб, Y вписывает.

Настройки хранятся в `Hollow Knight/Wisp/settings.json`, отметки — в `Wisp/userN.json` после успешного сохранения игры. Смена языка их не удаляет. Изображения кэшируются в `WispCache`. Карта сейва и отдельная задача поверх игры удалены.

Для обновления замените DLL при закрытой игре. Для удаления уберите только Wisp.dll; не удаляйте сейвы или файлы других модов.

## English

Close the game, install BepInEx 5 x64 beside hollow_knight.exe, then extract Wisp-1.0.0.zip there. Load a save and press F8 or both sticks. Open Настройки (Settings), then Язык: Русский to choose English. Both languages are included.

LB/RB switch main tabs; directional controls select panels/items; A opens and B returns. The right stick scrolls descriptions. Follow the contextual footer for X/Y and LT/RT actions. In the Journal, X opens the region selector. In the illustration gallery, Y changes images and pressing RS expands them. In expanded view, sticks pan, LT/RT zoom and Y fits the image.

Mouse clicks select, the wheel scrolls descriptions or zooms maps, and dragging pans maps. Settings and per-save marks are stored separately in the game's persistent-data Wisp folder. Language changes preserve progress. Wiki images download on demand into WispCache unless embedded.

Update or remove only Wisp.dll with the game closed. The release does not include BepInEx or game libraries.

## Управление 1.0.1 (предварительный выпуск)

В дневнике панели идут слева направо: враги → сведения → карта. A/стрелка вправо переходят дальше; B возвращает. В сведениях правый стик и вверх/вниз прокручивают текст. На карте RS разворачивает изображение, A включает перемещение, D-pad в режиме перемещения листает места. X повторяет неудавшуюся загрузку выбранной панели; при отсутствии ошибки открывает фильтр дневника. R повторяет загрузку с клавиатуры. Ошибку также можно нажать мышью.

При невозможности восстановить отметки из основного файла, резервной копии и старого формата запись отметок блокируется. Сообщение видно в шапке Wisp; исходные файлы не перезаписываются. Успешное восстановление также отмечено сообщением.
