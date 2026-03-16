# MAIN_SYSTEM (1688 закупки, логистика, склад, FBS, P&L)

Оффлайн desktop-приложение на **.NET 8 + WPF + MVVM + EF Core + SQLite** с единой основной формой `MAIN_SYSTEM`.

## Что реализовано
- Единая структура проекта: `Domain`, `Data`, `Services`, `UI`, `Tests`.
- SQLite модель для Products/FxRates/Purchases/Logistics/Sales/Marketing/OtherCosts.
- Расчетные сервисы:
  - курс CNY->RUB по правилу `last FxDate <= PaidDate`;
  - расчет продаж (Revenue/COGS/Self-buy/NetProfit/Margin);
  - Inventory by SKU (остаток + средняя себестоимость);
  - Unit economics by SKU;
  - P&L + KPI агрегатор.
- WPF окно `MAIN_SYSTEM` с вертикальными блоками (Expander + DataGrid), фильтром периода и командами.
- Интерфейс приложения локализован на русский язык (подписи блоков/элементов и культура `ru-RU`).
- Excel экспорт в 1 лист `MAIN_SYSTEM` (ClosedXML).
- Тесты доменной логики (10 кейсов): возвраты, самовыкупы, отсутствие курса, нулевые продажи, P&L показатели.
- Вкладка **Файлы**: Новый, Открыть(.xlsx), Сохранить, Сохранить как…, Экспорт, Импорт, путь файла и статус сохранения.
- Импорт/экспорт Excel проекта по отдельным листам (Товары/Курсы валют/…/P&L+KPI) с валидацией и логом ошибок.
- Настройки (⚙️): язык интерфейса, тема, автосохранение с сохранением в `config.json`.
- Поддержка смены темы (Светлая/Тёмная) с применением без перезапуска через динамические ресурсы.
- Вкладка **Аналитика/Диаграммы** (базовый модуль для пользовательских таблиц/графиков).

## Структура
- `src/Domain` — доменные модели + расчетный сервис
- `src/Data` — `AppDbContext` EF Core SQLite
- `src/Services` — проект/отчеты/Excel сервисы
- `src/UI` — WPF View + ViewModel + MVVM infrastructure
- `Tests/UnitEconomics.Tests` — unit-тесты расчетов

## Запуск
1. Установить .NET SDK 8 и workload для WPF/WindowsDesktop.
2. Открыть решение `ProcurementDesktop.sln` в Visual Studio 2022+.
3. Запустить проект `UI`.
4. В приложении нажать **Создать проект** — будет создан `project.sqlite` рядом с exe.
5. Нажать **Пересчитать** для обновления витрин Inventory/Unit Economics.

## Single-file publish (один exe)
```bash
dotnet publish src/UI/UI.csproj -c Release -r win-x64 /p:PublishSingleFile=true /p:SelfContained=true /p:IncludeNativeLibrariesForSelfExtract=true
```
Результат в `src/UI/bin/Release/net8.0-windows/win-x64/publish/`.

## Импорт/Экспорт Excel
- Экспорт: сервис `ExcelService.ExportMainSystemAsync` создает `MAIN_SYSTEM.xlsx` с листом `MAIN_SYSTEM`.
- Импорт: можно добавить симметричный метод `ImportMainSystemAsync` в `ExcelService` (в текущей версии включен экспорт и база для загрузки блоков).

## Пользовательская инструкция (кратко)
- Вводите данные в блоках Products/Fx Rates/Purchases/Logistics/Sales/Marketing/Other Costs.
- Блоки Inventory и Unit Economics read-only и пересчитываются командой **Пересчитать**.
- Для самовыкупов используйте `SaleType=SelfBuy`: выручка = 0, но комиссии/логистика попадают в расход.
- Для возвратов `Status=Returned`: выручка и COGS будут отрицательными.

## Дальнейшее расширение
- Валидации через FluentValidation для всех CRUD форм.
- Полная реализация импорта `xlsx` в транзакции all-or-nothing.
- KPI карточки и графики LiveCharts2 в нижнем dashboard блоке.
- Подсветка DataGrid по правилам (маржа/остатки) через DataTrigger styles.

## Design System
- Документ дизайн-системы и UX-спеки: `docs/DesignSystem-PremiumNotebook.md`
- UI tokens вынесены в `src/UI/Styles/*` и подключены через `App.xaml` merged dictionaries.
