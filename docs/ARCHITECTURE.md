# Architecture

## Solution structure

```text
BusinessOperations/
├── BusinessOperations.sln
├── docs/
├── src/
│   ├── BusinessOperations.API/
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Extensions/
│   │   ├── Contracts/              # HTTP-specific request/response models if useful
│   │   ├── Program.cs
│   │   └── appsettings*.json
│   ├── BusinessOperations.Application/
│   │   ├── DTOs/
│   │   ├── Interfaces/
│   │   ├── Services/
│   │   ├── Validators/
│   │   └── Mapping/                 # only if mapping complexity warrants it
│   ├── BusinessOperations.Domain/
│   │   ├── Entities/
│   │   ├── Enums/
│   │   ├── Exceptions/
│   │   └── Rules/                   # domain rules where they belong
│   └── BusinessOperations.Infrastructure/
│       ├── Data/
│       ├── Data/Configurations/
│       ├── Migrations/
│       ├── Repositories/            # only where a repository adds value
│       └── Services/
└── tests/
    ├── BusinessOperations.UnitTests/
    └── BusinessOperations.IntegrationTests/
```

Folders are guidelines, not a requirement to create empty directories in advance. Keep files discoverable and avoid abstraction for its own sake.

## Dependency rules

```text
API ───────────────> Application ───────────────> Domain
 │
 └─────────────────> Infrastructure ────────────> Application
                                    └───────────> Domain
```

- **Domain:** business entities, enums, and core invariants. No EF Core or ASP.NET dependencies.
- **Application:** use cases, DTOs, validation, and interfaces needed by use cases.
- **Infrastructure:** EF Core, SQL Server, persistence implementations, and integrations.
- **API:** HTTP controllers/endpoints, authentication setup, middleware, dependency registration, and OpenAPI.

Infrastructure implements interfaces defined inward. Application should not depend on Infrastructure. API is the composition root and can reference Infrastructure to register implementations.

## Request flow

1. HTTP request reaches API.
2. Authentication middleware validates the bearer token, if supplied.
3. Authorization checks endpoint/policy requirements.
4. Controller validates request shape and calls an Application use case/service.
5. Application enforces use-case/business rules and calls interfaces.
6. Infrastructure persists/loads data with EF Core.
7. API returns the appropriate HTTP status and response DTO.

## Design guidance

- Use asynchronous I/O and pass cancellation tokens for database operations where practical.
- Do not return EF Core entities directly from controllers.
- Validate input at the boundary and enforce important business rules in application/domain logic.
- Use database constraints for invariants that must remain true under concurrent requests.
- Keep controllers thin.
- Use transactions when one business operation must update multiple records atomically.
- Avoid logging passwords, JWTs, connection strings, or sensitive personal data.
- Use UTC for stored timestamps; document date-only business concepts such as leave dates separately.
- Use pagination for potentially large collections.
- Do not return stack traces to API clients.
- Do not claim an endpoint is secure based only on hiding it in the UI.

## Not in the initial scope

Microservices, Docker, Redis, message brokers, CQRS/MediatR, a generic repository framework, and a frontend are not initial requirements. Add them only after identifying a concrete need.
