# Repository Guidelines

## Project Structure & Module Organization

`Kwytka.slnx` is the solution entry point and contains two .NET 10 projects:

- `Kwytka/` is the Blazor Web App. Startup, authentication, and service registration are in `Program.cs`. Razor UI is under `Components/`: public pages are in `Components/Pages/`, layouts are in `Components/Layout/`, and reusable components are in `Components/Shared/`. Static assets and component JavaScript are in `wwwroot/` and beside their Razor components, respectively.
- `Kwytka.RichTextEditor/` is a Razor class library that provides the Quill editor used by the admin page. Its browser asset is in `Kwytka.RichTextEditor/wwwroot/`.

Application data is stored as JSON through `Configuration/JsonConfigurationService`. `ConfigPath` must be configured; it selects the JSON file path, relative to the application content root when not absolute. `ConfigurationData` contains the sale-page HTML and ordered price lists. The `/admin` page edits this data and is protected by HTTP Basic authentication using `Admin:Login` and `Admin:Password` configuration values.

Public routes are `/`, `/price`, `/price/{slug}`, `/sale`, and `/contacts`; missing content is served through `/not-found`. Build outputs under `bin/` and `obj/` are generated and must not be committed.

## Build, Test, and Development Commands

- `dotnet restore Kwytka.slnx` restores project dependencies.
- `dotnet build Kwytka.slnx` compiles the full solution and reports analyzer errors.
- `dotnet run --project Kwytka/Kwytka.csproj` starts the app; use the URL printed in the terminal.
- `dotnet watch --project Kwytka/Kwytka.csproj` runs locally with hot reload.
- `dotnet format Kwytka.slnx --verify-no-changes` checks formatting against `.editorconfig`.
- `dotnet test Kwytka.slnx` runs all tests; no test project exists currently.

Both projects target `net10.0`, so install a compatible .NET SDK. Configure `ConfigPath`, `Admin:Login`, and `Admin:Password` through local configuration or environment variables before running the app; do not store credentials in source control.

## Coding Style & Naming Conventions

Follow the root `.editorconfig`: use spaces, four-space indentation for C# and two spaces for XML, JSON, and project files. Use file-scoped namespaces and top-level statements where appropriate. Types, namespaces, methods, properties, and Razor component files use `PascalCase`; interfaces start with `I`; parameters and locals use `camelCase`; private fields use `_camelCase`, and private static fields use `s_camelCase`. Keep component-specific CSS and JavaScript beside the component as `.razor.css` and `.razor.js`.

## Testing Guidelines

No test project or coverage threshold exists yet. Add automated tests in a separate project such as `Kwytka.Tests/`, reference the relevant application or library project, and name test files after the unit under test (for example, `JsonConfigurationServiceTests.cs`). Test method names should state behavior and outcome. Run `dotnet test Kwytka.slnx` before opening a pull request.

## Commit & Pull Request Guidelines

Use short, imperative commit subjects such as `Add catalog page`, keeping each commit focused. Pull requests should explain the user-visible change, list verification performed, link related issues, and include before/after screenshots for UI changes. Do not commit secrets; use environment variables or .NET user secrets for local-only configuration.
