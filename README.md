# Loyalty Points API

A .NET 10 ASP.NET Core API that awards points idempotently using in-memory state.

## Requirements

.NET SDK 10.0 (tested with SDK 10.0.301, runtime 10.0.9).

## Run and reset

From the repository root on Windows:

```powershell
dotnet run --project src\LoyaltyPoints.Api
```

The service listens on the URLs printed by `dotnet run`. It starts with wallets `W100` (100 points) and `W200` (50 points). Because all state is in memory, stop and restart the process to reset balances and accepted awards to their initial values.

Run the automated tests with:

```powershell
dotnet test
```

## API

- `POST /awards` accepts `sourceId`, `externalTransactionId`, `walletId`, and `points`.
- `GET /wallets/{walletId}` returns the current wallet balance.

Identifiers must be 1–64 ASCII letters, digits, underscores, or hyphens. Points must be an integer from 1 to 1000. Errors are JSON objects with an `error` message.

When errors overlap, request shape, field types, formats, and ranges are validated first. For a valid award, an existing identity is checked before looking up a wallet: a matching identity returns the original award (`200`), a conflicting payload returns `409`, and only a new identity proceeds to wallet lookup (`404` if unknown). Unknown-wallet attempts do not consume their identity.

## Structure

- `LoyaltyPoints.Domain` contains wallet and award entities, identity, and domain exceptions.
- `LoyaltyPoints.Application` contains award/wallet use cases and the `IWalletStore` port.
- `LoyaltyPoints.Infrastructure` implements that port with the process-local store.
- `LoyaltyPoints.Api` contains HTTP controllers, request validation, response contracts, exception mapping, and application composition.

Each class and response/request contract is defined in its own file. Dependencies point inward for business behavior: Application depends on Domain, Infrastructure implements the Application port, and the API composes the implementations.

See `HANDOFF.md` for key decisions, checks executed, limitations, and time spent.
