# Квітка

Blazor Web App for the Квітка planting-material and ornamental-plant catalogue. The public site shows company information, current price lists, and a sale page. An authenticated admin page manages the sale content and price lists stored in a JSON file.

## Projects

- `Kwytka/` — the .NET 10 Blazor Web App.
- `Kwytka.RichTextEditor/` — a Razor class library containing the Quill rich-text editor used by the admin page.

## Requirements

- .NET 10 SDK

## Run locally

Set the storage location and admin credentials before starting the app. Environment-variable names use double underscores for nested configuration.

```sh
export ConfigPath="config/settings.json"
export Admin__Login="admin"
export Admin__Password="change-me"
dotnet run --project Kwytka/Kwytka.csproj
```

The app starts using the URL printed by `dotnet run`. The JSON configuration file is created when changes are first saved in the admin area. Do not commit real credentials.

## Routes

| Route | Purpose |
| --- | --- |
| `/` | Home page |
| `/price` | List of available price lists |
| `/price/{slug}` | Individual price list with search |
| `/sale` | Sale page |
| `/contacts` | Contact information |
| `/admin` | Password-protected configuration editor |

## Configuration data

The configured JSON file contains:

- `priceLists`: ordered price lists with a unique `slug`, `title`, and HTML content.
- `isSaleEnabled`: sale-state flag.
- `salePageHtml`: HTML displayed on the sale page.

The admin page uses HTTP Basic authentication, configured through `Admin:Login` and `Admin:Password` (or the `Admin__Login` and `Admin__Password` environment variables).

## Commands

```sh
dotnet restore Kwytka.slnx
dotnet build Kwytka.slnx
dotnet test Kwytka.slnx
dotnet format Kwytka.slnx --verify-no-changes
```

There is currently no test project, so `dotnet test` is included for when tests are added.
