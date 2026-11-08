# ServiceHub project guidelines

## Project scope and stack

- ServiceHub is an individual university semester project: keep features easy to medium in complexity and the implementation approachable.
- The web application is ASP.NET Core MVC on .NET 10, using C#, Razor views, HTML, CSS, JavaScript, and Bootstrap 5.
- MySQL with Entity Framework Core and ASP.NET Core Identity are part of the current phase. Keep Identity features approachable: cookie login for MVC, optional JWT for the API, Google sign-in when configured, password reset, and lockout.
- Do not add a package unless the current phase uses it. Check its .NET and EF Core compatibility before adding it.

## MVC structure

- `Controllers/` contains MVC controllers. Keep actions focused on HTTP concerns and delegate non-trivial business logic rather than growing controllers.
- `Models/` contains domain and view models. Use explicit view models for data passed to views; do not bind persistence entities directly from untrusted requests.
- `Views/<Controller>/` contains controller views; `Views/Shared/` contains shared layouts and partials.
- `wwwroot/css/`, `wwwroot/js/`, and `wwwroot/images/` contain public static assets. Do not put secrets or private uploads in `wwwroot`.
- Add `Data/` for the EF Core context and migrations when database integration begins. Add `Services/` only when a real business use case warrants a service; avoid generic repositories and unnecessary layers.

## C# and application design

- Use nullable reference types, meaningful names, small focused methods, and the existing framework conventions.
- Use constructor injection for required dependencies. Keep controllers and services testable without introducing abstractions that have no real use.
- Use asynchronous EF Core APIs for database I/O and pass `CancellationToken` through where supported. Do not use `async` when no asynchronous work exists.
- Validate user input at the application boundary and use antiforgery protection for state-changing MVC form posts.
- Follow least privilege and secure defaults. Never expose exception details to production users, and never enable EF sensitive-data logging in production.
- Use ASP.NET Core logging for actionable diagnostics; do not log passwords, connection strings, tokens, or unnecessary personal data.

## Configuration and dependencies

- Keep non-secret defaults in `appsettings.json` and environment-specific, non-secret overrides in `appsettings.Development.json` or `appsettings.Production.json`.
- Store local development secrets with .NET User Secrets (the project has a `UserSecretsId`). Supply production secrets through the deployment platform's secret store/environment variables, such as `ConnectionStrings__DefaultConnection`.
- Never commit credentials, private keys, tokens, or populated local settings. Keep local-only configuration out of source control.
- Before adding a MySQL provider, confirm its supported EF Core major version matches the EF Core version selected for this `net10.0` application. Do not infer compatibility from a package's target framework alone.
- Pin dependencies deliberately and avoid unused packages. The MySQL provider is `Microting.EntityFrameworkCore.MySql` aligned with EF Core 10.0.14. Do not swap providers without checking that major-version match.

## Middleware, errors, and UI

- Register application services in `Program.cs` through dependency injection and preserve the established MVC routing.
- Keep detailed developer diagnostics limited to Development. Use the generic error route and HTTPS/HSTS behavior in non-development environments.
- Reuse the shared layout and Bootstrap assets. Keep page-specific behavior in the appropriate view or static asset rather than embedding large scripts or styles in Razor markup.
- Make focused changes, preserve existing behavior, and run `dotnet build` after code or project configuration changes.
