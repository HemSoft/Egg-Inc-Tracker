# Running Multiple Projects in Separate Terminals

This guide shows how to manually start each project in separate terminals for development.

## 1. Start the API (EggIncTrackerApi)

```bash
cd EggIncTrackerApi
dotnet run
```

API should start at https://localhost:7117 (or your configured port)

## 2. Start the Azure Function (UpdatePlayerFunction)

```bash
cd UpdatePlayerFunction
func start
```

Function should start at http://localhost:7071 (default port)

## 3. Start the Blazor App (EggDash)

```bash
cd EggDash
dotnet run
```

Blazor app should start at https://localhost:5001 (or your configured port)

## Note on appsettings.json Configuration

Make sure each project's configuration points to the correct URLs:

1. In **EggDash/appsettings.json** or **EggDash.Client/wwwroot/appsettings.json**, ensure the API URL is set correctly:

```json
{
  "ApiBaseUrl": "https://localhost:7117/"
}
```

2. In **UpdatePlayerFunction/local.settings.json**, ensure any API URLs are configured correctly:

```json
{
  "Values": {
    "ApiBaseUrl": "https://localhost:7117/"
  }
}
```

This ensures all components can communicate with each other during local development. 