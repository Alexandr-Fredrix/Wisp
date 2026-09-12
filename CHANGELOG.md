# 1.0.1 — предварительный выпуск

## 2026-09-12 — исправления и оптимизация

- Исправлены заражённое Перепутье, независимые посещения, очередь изображений при смене языка, повтор загрузки и выбор первой карты по области.
- Добавлены управление сведениями дневника с контроллера, восстановление резервной копии и защита повреждённых отметок от перезаписи. Последняя выбранная инструкция восстанавливается при загрузке.
- Удалены карта сейва, расчёты HUD, поиск неиспользуемых портретов и загрузка декораций. Кэш текстур ограничен 128 МиБ/24 изображениями и использует LRU; списки и разметка кэшируются.
- 214 PNG проверены при сжатии без изменения RGBA-пикселей: 194 130 883 → 142 349 463 байта. Оригиналы сохранены в отдельном редакторском комплекте. JPEG-кандидаты не применены.
- Создан офлайн-комплект: 39 разделов, 300 пунктов, 392 уникальных изображения, исходный снимок и поля замечаний.
- Добавлены регрессионные и файловые тесты, проверка настоящего загрузчика с подставным транспортом и настоящего обработчика смены языка с заполненным прогрессом. Финальный прогон: 21 Python-тест, 43 основных и 35 регрессионных проверок C#, 14 проверок загрузчика с подставным транспортом, каталог и настоящий обработчик смены языка прошли. Протоколы: artifacts/Wisp/validation.json.
- 1.0.1 выпускается как Pre-release; 1.0.0 остаётся основным выпуском Latest. Проверки в игре, физический контроллер и замеры производительности остаются на проверке.

## 2026-09-12

- Добавлены правила агента в `AGENTS.md`: краткое общение на русском, автор и коммитер Alex, ведение журнала изменений и обновление сводки общения.
- Проверена локальная идентичность Git: Alex. Изменения относятся к документации; сборка и игровые тесты не требуются.

# 1.0.0

Русский / English — один мод с переключателем языка в настройках.

- Полный английский перевод меню, маршрутов, справочника, названий врагов, достижений и подписей к иллюстрациям. Выбор языка сохраняется, прогресс остаётся прежним.
- Маршруты A: 112% и достижения, B: скорость и противоположные выборы, C: Стальная душа.
- Иллюстрированные шаги, коллекции и семь последовательных встреч с Господином Грибом.
- Дневник с выбором локации, состоянием записей и картами мест обитания.
- Атлас, включая Бездну, Сады королевы и Туманный каньон; просмотр карт внутри мода.
- Прокрутка описаний с контроллера, выравнивание навигации; убраны карта сейва, всплывающая задача поверх игры и упоминания источника документа в интерфейсе.
- Обновлены пять игровых скриншотов на странице проекта.

Install: extract **Wisp-1.0.0.zip** into the Hollow Knight folder with BepInEx 5 x64 installed. Open a save, press **F8**, then select **Settings → Language**. Both languages are included; no separate download is needed.

Compatibility: Windows x64, Hollow Knight 1.5.12620, BepInEx 5.4.23.5. Wiki images that are not embedded download on first view and remain cached. Text baked into third-party artwork retains its original language; the English atlas uses English reference maps.

Validation: 17 content/localization tests, 45 core assertions, successful game-reference build, and packaged catalog switching RU → EN → RU with stable identifiers. The supplied screenshots document the Russian interface before the language toggle was added. The final bilingual UI has not yet been exercised in a running game; report layout/controller issues with your resolution and controller model.

## 0.4.0-alpha.19

- Название фильтра локации переносится отдельно от подсказки X и использует единое русское название области.
- Пересверены привязки врагов по таблицам Enemies/Bosses страниц областей, включая подобласти. Соседние входы больше не считаются местом обитания.
- Дополнительные записи идут после обязательных, внутри каждой группы незавершённые остаются первыми.
- Проверка регрессии: стражник остаётся в Перепутье, но исключён из Кристального пика и Туманного каньона.

Источник: https://hollowknight.wiki/w/Husk_Guard и страницы областей; контрольный список — content/region-enemy-sources.json.

## 0.4.0-alpha.18

- Выбор локации в Дневнике охотника: текущая область, все области, отдельные зоны; мышь и геймпад.
- Карты и скриншоты мест получения предметов, боссов, гусеничек, амулетов и станций; 205 дополнительных встроенный файл с Hollow Knight Wiki.
- Перелистывание иллюстраций и разворачивание с масштабом и перемещением. Первая часть инструкции видна перед иллюстрацией.
- Уточнены описания поиска, разбиты слитные нумерованные действия. Сохранены идентификаторы и условия прогресса.

Источники изображений: https://hollowknight.wiki/ ; исходные названия файлов сохранены в content/locations и content/step-media.json.

## 0.4.0-alpha.17

- Маршрут Господина Гриба: условия, семь встреч по порядку, 7 карт и 7 скриншотов, встроенных в мод. Справочные пункты не меняют счётчик задач.

Источник: https://hollowknight.wiki/w/Mister_Mushroom_(Hollow_Knight)

## 0.4.0-alpha.16

- Добавлена встроенная русская карта Бездны в атлас и карты шагов Теневого плаща и Сердца пустоты. Исправлена область Бездны для дневника.

## 0.4.0-alpha.15

- Названия достижений, врагов и предметов переведены по русской локализации игры. Убраны английские названия из маршрута и запасных подписей дневника.

## 0.4.0-alpha.14

- Прокрутка описания с геймпада доступна сразу при выборе вкладки, без дополнительного нажатия A.

## 0.4.0-alpha.13

- Удалена карта сохранения из маршрута, дневника и атласа вместе с переключателями и управлением с геймпада. Остались карты с вики.

## 0.4.0-alpha.12

- Исправлен фильтр текущей области дневника; добавлено название области и приоритет незавершённых записей.

# 0.4.0-alpha.11

Удалён блок задания поверх игры.

# 0.4.0-alpha.10

Карточки достижений и автоматическая синхронизация статуса профиля.

# 0.4.0-alpha.9

Иллюстрации целей в описаниях 79 шагов.

# 0.4.0-alpha.8

Выравнивание стрелок дневника и подчёркиваний вкладок.

# 0.4.0-alpha.7

Недостающие карты и загрузка карт зон с вики; удалена лишняя стрелка.

# 0.4.0-alpha.6

Карточки списков, форматирование PDF, отдельный атлас зон и исправление построения карты сейва.

# 0.4.0-alpha.5

- Whole-row route pagination, compact labels and separate PDF overview.
- Less repeated description text and more journal detail space.

# 0.4.0-alpha.4

- Align journal and route columns and context rows; separate controller hints.
- Remove passive hover fills and page frames; compact settings spacing.

# 0.4.0-alpha.3

- Match mockup geometry: 16:9, wider columns, solid selection, boxed hints and flat scrollbars.
- Larger JetBrains Mono text and PDF paragraph reflow.

# 0.4.0-alpha.2

- Fix invisible text: direct TTF loading, glyph validation and fallback fonts.

# 0.4.0-alpha.1

- Local gameplay candidate: bundled JetBrainsMono Nerd Font, explicit controller content focus, aligned navigation rows.
- PDF route A1–A15, B/C goals and collection reference pages; preserved existing objective IDs and manual marks.
- No separate completion checkbox for returning after credits.
- Live controller and visual verification pending.

# Unreleased

- Read earned achievements through the game's profile provider, including other saves; label these confirmations separately from current-save progress.
- Keep current-run HUD prerequisites and manual post-credits tasks independent of profile achievements.
- Show exploration question marks only for location chapters, not ending or checklist sections.

# 0.3.0-alpha.2 — controller focus

- Keep an unexplored (?) indicator beside chapter progress even when spoilers reveal the name; require visit evidence independently of completed tasks.
- Press the right stick in the map pane to switch reference/save maps; label the shortcut beside the controls and explain unavailable save maps.
- Separate the currently open item (underline) from controller focus (silver corners).
- Add LB/RB hints beside main tabs and contextual navigation help.
- Keep the left stick in chapter/step lists until focus enters the map pane.
- A enters panels or opens the highlighted detail tab; Y marks steps; B returns one panel.
- Reset route focus when changing main tabs; remove large filled selection boxes.
- Local build installed and guide opening observed; physical controller flow still needs verification. Published alpha.1 archive is unchanged.

# 0.3.0-alpha.1

- Port to Hollow Knight 1.5.12620 / BepInEx 5, without replacing game assemblies.
- One installable Wisp.dll; routes and generated interface art embedded.
- Compact task HUD, illustrated journal and pan/zoom habitat maps.
- Per-save 112% / short Steel Soul goal; import legacy Wisp marks.
- Personal-use terms for newly published changes; earlier MIT grants preserved.

## 0.2.0-alpha.3

Bounded ornamental interface, in-game habitat maps and per-save route goals. See docs/releases/0.2.0-alpha.3.md.

# Changelog

## 0.2.0-alpha.2

- Restore visited areas from existing saves and add 29 automatic milestone rules.
- Preserve the shared EventSystem; support opening from gameplay and controller navigation.
- Add a contextual gameplay HUD and journal notifications; restyle the guide as a framed, silver-blue journal.
- Add six tests for save discovery and isolation. Physical controller verification remains pending.

## Unreleased

### 0.2.0-alpha.1 development implementation

- Pause-screen guide (F8), 22 chapters / 98 steps, per-save manual progress and 23 read-only automatic rules.
- 164 journal records with 163 game-counter mappings, game portraits, location filters and contextual reminders.
- Area-map rendering from existing game meshes, pan/zoom and spoiler filtering.
- Core C# tests, content validation, local Roslyn build and development-archive tooling.
- Runtime testing is pending. This is not a verified public gameplay release.

## 0.1.0-alpha.1 — 2026-09-08

Initial source preview, **not an installable mod**.

- Russian/English documentation, MIT license and third-party policy.
- C# entry-point scaffold; game compatibility not tested.
- Repository checks, source packaging, checksums and prerelease automation.
- Contribution guidelines and issue templates.

No game binaries or external artwork are distributed.
