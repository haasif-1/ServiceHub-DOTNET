# ServiceHub

University semester project: an ASP.NET Core MVC app on .NET 10 for finding and offering local services. This phase adds ASP.NET Core Identity with cookie login, JWT API tokens, Google sign-in, password reset, and account lockout.

## Run locally

```bash
dotnet tool restore
dotnet restore
dotnet build
dotnet test
dotnet run --project ServiceHub.Web
```

Development currently uses an in-memory store so the site starts without MySQL. Accounts reset when the process stops.

### MySQL and initial Admin account

1. Create the `servicehub_db` database in MySQL/phpMyAdmin.
2. Store the connection string and initial Admin credentials in User Secrets (do not commit them):

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=servicehub_db;User=your_mysql_user;Password=your_mysql_password;" --project ServiceHub.Web
dotnet user-secrets set "UseInMemoryDatabase" "false" --project ServiceHub.Web
dotnet user-secrets set "InitialAdmin:Email" "admin@example.com" --project ServiceHub.Web
dotnet user-secrets set "InitialAdmin:Password" "replace-with-a-strong-password" --project ServiceHub.Web
```

The Admin password must be at least 8 characters and contain uppercase, lowercase, and numeric characters.

3. Apply the initial migration with `dotnet ef database update`, then run the app. In Development it applies any pending EF Core migrations and idempotently seeds the `Admin`, `Customer`, and `ServiceProvider` roles plus the configured initial Admin account. If either Admin setting is missing, the Admin account is not created.

The Admin email and password are read only from secure configuration. Public registration permits `Customer` and `ServiceProvider` accounts only.

### JWT API

```bash
dotnet user-secrets set "Jwt:Key" "replace-with-a-long-random-development-key" --project ServiceHub.Web
```

Then `POST /api/auth/token` with `{ "email": "...", "password": "..." }`. Send the returned token as `Authorization: Bearer ...` to `GET /api/auth/me`.

### Google sign-in

Create an OAuth client in Google Cloud. Authorized redirect URI:

`https://localhost:<port>/signin-google`

```bash
dotnet user-secrets set "Authentication:Google:ClientId" "your-client-id" --project ServiceHub.Web
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-client-secret" --project ServiceHub.Web
```

The Google button appears on the login page only after both values are set.

## Identity behaviour

- Register and login use cookie authentication and anti-forgery tokens.
- Accounts lock for 15 minutes after 5 failed password attempts.
- Password reset emails are written to the application log in this phase. Use the logged link to open `/Account/ResetPassword`.
- New users choose Customer or ServiceProvider. Admin is not publicly selectable; the initial Admin account is created only when both secure configuration values are present.

