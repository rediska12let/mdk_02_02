# Электронная медицинская карта — МДК.02.02, вариант №2

Учебный сервер **MedicalRecords API** на ASP.NET Core 8, Entity Framework Core и PostgreSQL. Проект демонстрирует интеграцию модулей, контракт API, разделение доступа, структуру репозитория и документацию ADR (практические работы №1–9).

> Это **учебный прототип**, не медицинская информационная система для реального использования. Настоящие медицинские данные, пароли, токены и ключи в репозиторий не загружаются. Схема не прошла аудит на требования к обработке медицинских данных; применять для реальных пациентов нельзя.

## Возможности

- просмотр, поиск и добавление карточек пациентов;
- посещения пациента и медицинские заметки;
- ролевой доступ: `Clinician` и `Admin`;
- ограничение доступа врача карточками назначенных ему пациентов;
- вход по JWT от доверенного поставщика токенов (или локальному учебному генератору токенов);
- PostgreSQL через EF Core, проверка входных данных, базовые unit-тесты;
- `.gitignore`, `.gitattributes`, GitHub Actions, OpenAPI и ADR.

## Состав

- `src/MedicalRecords.Api` — Web API, модели, БД, правила доступа;
- `tests/MedicalRecords.Tests` — модульные тесты;
- `docs/api/openapi.yaml` — контракт HTTP API;
- `docs/ADR-001.md` — решение PostgreSQL + EF Core;
- `docs/branch-protection.md` — рекомендуемые правила для main;
- `tools/issue_dev_token.py` — **только для локальной разработки**, создаёт учебный JWT без обращения к реальным пользователям.

## Запуск (Windows PowerShell / Linux shell)

Нужны .NET SDK **8**, Docker (для PostgreSQL) и Python 3 (для выпуска учебного JWT).

1. Задайте собственный пароль PostgreSQL и запустите БД:

```powershell
$env:POSTGRES_PASSWORD = "CHANGE_ME_db_unique_password"
docker compose up -d
```

2. Настройте переменные окружения (секрет JWT — случайная строка длиной от 32 байт):

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__MedicalDb = "Host=localhost;Port=5432;Database=medical_records;Username=medical_app;Password=$env:POSTGRES_PASSWORD"
$env:Auth__SigningKey = "CHANGE_ME_super_long_random_32_byte_key_at_least"
```

3. Запустите API:

```powershell
dotnet restore src/MedicalRecords.Api/MedicalRecords.Api.csproj
dotnet run --project src/MedicalRecords.Api/MedicalRecords.Api.csproj --urls http://127.0.0.1:5076
```

В режиме Development таблицы учебной БД создаются методом `EnsureCreated`. Для продакшена нужны настоящие миграции EF Core и отдельный механизм управления секретами.

4. В другом терминале с тем же `Auth__SigningKey` создайте **локальный демонстрационный токен**:

```powershell
python tools/issue_dev_token.py clinician-001 Clinician
```

Скопируйте токен и передайте его в заголовке `Authorization: Bearer <token>`.

5. Пример запроса (подставьте токен):

```powershell
$token = "PASTE_DEV_JWT"
Invoke-RestMethod -Method Post -Uri http://127.0.0.1:5076/api/v1/patients \
  -Headers @{Authorization="Bearer $token"} -ContentType "application/json" \
  -Body '{"familyName":"Тестов","givenName":"Иван","birthDate":"2000-01-01"}'
```

GET `/health` доступен без авторизации. Доступные маршруты приведены в `docs/api/openapi.yaml`.

## Тестирование

```bash
dotnet test tests/MedicalRecords.Tests/MedicalRecords.Tests.csproj
```

Unit-тесты проверяют правила доступа и валидации. Проверка CI настроена, но до получения результатов GitHub Actions **успех сборки не утверждается**. В текущей среде подготовки отсутствует .NET SDK, поэтому локальная компиляция не проводилась.

## Безопасность и ограничения

- Не используйте учебный генератор JWT за пределами локального стенда.
- Не храните реальные данные пациентов, секреты или дампы в Git.
- Назначение врача пациенту в MVP выполняется автоматически при создании карточки. Реальное назначение должно идти через отдельный авторизованный процесс.
- Реальная ЭМК требует управления согласиями, расширенного аудита, шифрования, ротации ключей и формальной оценки соответствия требованиям законодательства.

## Связь с практическими работами

**ПР8:** `docs/ADR-001.md`, `docs/api/openapi.yaml` и структура модулей — проектные артефакты.

**ПР9:** `.gitignore`, `.gitattributes`, `.github/workflows/ci.yml`, `docs/branch-protection.md` — настройки репозитория. Защита ветки на GitHub настраивается отдельно в Settings → Branches/Rulesets.
