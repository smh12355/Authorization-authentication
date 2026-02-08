# Authorization & Authentication with Keycloak

ASP.NET Core Web API with Keycloak authentication and role-based authorization.

## Технологии

- ASP.NET Core 9.0
- Keycloak (Identity Provider)
- PostgreSQL 15
- Docker & Docker Compose
- JWT Bearer Authentication

## Требования

- .NET 9 SDK
- Docker & Docker Compose
- Postman или curl (для тестирования)

## Быстрый старт

### 1. Запуск Keycloak и PostgreSQL
```bash
# Скопируй .env.example в .env
cp .env.example .env

# Отредактируй .env (укажи пароли)

# Запусти контейнеры
docker-compose up -d

# Проверь что запустились
docker-compose ps
```

Keycloak будет доступен на: http://localhost:8080

### 2. Настройка Keycloak

1. Открой http://localhost:8080/admin
2. Войди: `admin` / `admin` (или пароль из `.env`)
3. Создай Realm: `my_keycloak_project`
4. Создай Client: `webapi-client`
5. Создай Roles: `User`, `Admin`
6. Создай Users и назначь роли

### 3. Запуск API
```bash
cd Src/Authorization~authentication
dotnet restore
dotnet run
```

API будет доступен на: http://localhost:5275

Swagger UI: http://localhost:5275/

### 4. Тестирование

#### Получить токен:
```bash
curl -X POST "http://localhost:8080/realms/my_keycloak_project/protocol/openid-connect/token" \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=webapi-client" \
  -d "client_secret=YOUR_SECRET" \
  -d "username=testuser" \
  -d "password=password123" \
  -d "grant_type=password"
```

#### Тестировать endpoints:
```bash
# Публичный (без токена)
curl http://localhost:5275/api/public

# Защищенный (с токеном)
curl http://localhost:5275/api/protected \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Endpoints

| Endpoint | Method | Auth | Roles | Description |
|----------|--------|------|-------|-------------|
| `/api/public` | GET | ❌ No | - | Публичный endpoint |
| `/api/protected` | GET | ✅ Yes | Any | Требует аутентификацию |
| `/api/user` | GET | ✅ Yes | User | Требует роль User |
| `/api/admin` | GET | ✅ Yes | Admin | Требует роль Admin |
| `/weatherforecast` | GET | ✅ Yes | Any | Прогноз погоды |

## Конфигурация

### appsettings.json
```json
{
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/my_keycloak_project",
    "Audience": "account",
    "MetadataAddress": "http://localhost:8080/realms/my_keycloak_project/.well-known/openid-configuration",
    "RequireHttpsMetadata": false
  }
}
```

## Структура проекта
```
.
├── docker-compose.yml          # Keycloak + PostgreSQL
├── .env.example               # Пример переменных окружения
├── Src/
│   └── Authorization~authentication/
│       ├── Controllers/       # API контроллеры
│       ├── Extensions/        # Extension methods
│       ├── appsettings.json   # Конфигурация
│       └── Program.cs         # Entry point
└── README.md
```

## Остановка сервисов
```bash
# Остановить контейнеры
docker-compose down

# Остановить и удалить volumes (УДАЛИТ ДАННЫЕ!)
docker-compose down -v
```

## Troubleshooting

### Keycloak не запускается
```bash
docker-compose logs keycloak
```

### API возвращает 401
- Проверь что токен не истек (живет 5 минут)
- Проверь что realm правильный в appsettings.json
- Проверь что Keycloak доступен на http://localhost:8080

### API возвращает 403
- Проверь что у пользователя есть нужная роль
- Проверь claims в токене: `/api/protected` покажет все claims

## Лицензия

MIT