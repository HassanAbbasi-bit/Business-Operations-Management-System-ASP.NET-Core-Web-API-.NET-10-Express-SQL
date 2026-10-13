# Technology Stack and Decisions

| Area | Decision | Notes |
|---|---|---|
| Language | C# | Primary backend language |
| Target framework | `net10.0` | Local SDK 10.0.401 confirmed |
| Web | ASP.NET Core Web API | REST API, controllers initially |
| Database | Microsoft SQL Server | Local instance still needs connection verification |
| ORM | Entity Framework Core | Use package versions compatible with EF Core 10 |
| Authentication | JWT bearer | Secure key management and explicit validation settings required |
| Authorization | Role-based/policy-based | Admin, Manager, Employee; enforce server-side |
| Validation | FluentValidation | Use compatible version and clear validation responses |
| API docs | OpenAPI/Swagger | Verify .NET 10 template and package behavior before adding packages |
| Logging | Serilog | Structured logs; never log secrets |
| Unit tests | xUnit | Domain/application behavior |
| Integration tests | xUnit + ASP.NET Core test host as needed | Exercise real HTTP/API pipeline |
| Source control | Git/GitHub | Small, meaningful commits |

## Package/version policy

Do not hardcode unverified package versions into this document. Before installing:
1. Check target framework and project files.
2. Choose mutually compatible stable versions for .NET 10.
3. Use `dotnet list package` to inspect installed packages.
4. Record meaningful package decisions here or in a decision log.
5. Avoid prerelease packages unless explicitly justified.

## Configuration and secrets

- Keep non-secret defaults in `appsettings.json`.
- Keep local credentials and JWT signing secrets out of source control.
- Use .NET user-secrets for local development secrets.
- Use environment variables or the deployment platform's secret store in deployment.
- Commit an `appsettings.example.json` only if it contains placeholders, never real credentials.

## Frontend decision

Deferred. After the backend is stable, choose React if the goal includes demonstrating a separate modern frontend stack; choose Blazor if the goal is to remain primarily in the .NET ecosystem. Do not build both initially.
