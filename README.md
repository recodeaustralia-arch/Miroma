# Partner Integration BFF

.NET 8 Backend-for-Frontend that accepts partner transactions, validates them, verifies the partner against a (deliberately flaky) verification API, and publishes accepted work to RabbitMQ for legacy consumers.

## Architecture

```
Partner  --API key-->  POST /api/v1/partner/transactions
                              |
                    FluentValidation (amount > 0, ISO 4217, required fields)
                              |
                    Resilient HTTP client (Polly retries)
                              v
                    GET /api/v1/mock/partners/{id}/verify   (same process; 30% TimeoutException)
                              |
                    ITransactionQueuePublisher
                              v
                    RabbitMQ queue: partner-transactions
```

Projects:

| Project | Responsibility |
| --- | --- |
| `PartnerIntegration.Api` | HTTP surface, API-key auth, Swagger, global exception handler |
| `PartnerIntegration.Application` | Use-case (`PartnerTransactionProcessor`), contracts, FluentValidation |
| `PartnerIntegration.Infrastructure` | Flaky mock verifier, HTTP client, Polly pipeline, RabbitMQ publisher |
| `PartnerIntegration.UnitTests` | Validation, retry behaviour, processor outcomes, API tests |

Dependencies flow inward: Api → Application ← Infrastructure. Queueing and HTTP are behind interfaces so the processor stays testable and does not throw unhandled exceptions to the caller.

### Resilience

The mock verification endpoint throws `TimeoutException` ~30% of the time (mapped to HTTP 504). The outbound client treats 5xx / 408 / 504 as `TimeoutException` and Polly retries up to 3 times with exponential backoff. If retries are exhausted the processor returns **503** instead of crashing the request.

Verified partners in the mock: `P-1001`, `P-1002`, `P-2000`.

### Security

`POST /api/v1/partner/transactions` uses an API-key scheme (`X-Api-Key`). That is a practical starting point for server-to-server partner traffic. A production rollout would typically add:

- Per-partner keys (or OAuth2 client credentials) stored hashed, not in config
- mTLS between partners and the BFF
- Replay protection using `transactionReference` idempotency
- Rate limiting and request signing

The mock verification endpoint is anonymous on purpose: it stands in for an external system.

## Prerequisites

- .NET 8 SDK
- Docker (for RabbitMQ / full stack)

## Run locally

Start the broker, then the API:

```bash
docker compose up rabbitmq -d
dotnet run --project src/PartnerIntegration.Api
```

Swagger: [http://localhost:5080/swagger](http://localhost:5080/swagger)

Default development key: `dev-partner-api-key`

```bash
curl -sS -X POST http://localhost:5080/api/v1/partner/transactions \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: dev-partner-api-key" \
  -d '{
    "partnerId": "P-1001",
    "transactionReference": "TXN-99823",
    "amount": 250.00,
    "currency": "USD",
    "timestamp": "2024-05-10T14:30:00Z"
  }'
```

Accepted transactions appear on the `partner-transactions` queue. RabbitMQ management UI: [http://localhost:15672](http://localhost:15672) (`guest` / `guest`).

Because verification is flaky, a valid payload may occasionally return **503** after retries; that is expected. Retry the request.

## Run with Docker Compose

```bash
docker compose up --build
```

API: [http://localhost:5080](http://localhost:5080)

## Tests

```bash
dotnet test PartnerIntegrationBff.sln
```

With coverage (coverlet):

```bash
dotnet test PartnerIntegrationBff.sln --collect:"XPlat Code Coverage"
```

Covered areas:

- Required fields, `amount > 0`, ISO 4217 currency
- Mock verifier timeout vs success paths
- Polly retry on `TimeoutException` / `HttpRequestException`, no retry on unexpected errors
- Processor: queue on success, skip queue when unverified, 503-style results when dependencies fail
- Endpoint: 400 / 401 / 202, and 504 from the mock timeout path
