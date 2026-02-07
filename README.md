# Embedded Integration - Takeoff & Estimator

This repository contains two sample services demonstrating a simple handoff of user-driven "takeoff" actions from a Takeoff app to an Estimator service.

## Projects
- `Contracts`: shared DTOs (`Quantity`, `Measurement`, `TakeoffAction`, etc.)
- `Takeoff`: interactive editor that records validated actions and can send them to Estimator
- `Estimator`: processes incoming Takeoff action lists and applies business semantics to a local state
- `Validation`: server-side business validation rules used by Takeoff

## Running locally
1. Open the solution in Visual Studio or use `dotnet run` from project folders.
2. Start the `Takeoff` project. It serves a client at `state-editor.html`.
3. Start the `Estimator` project. It serves a read-only client at `state-viewer.html`.

## Configuration
- Configure remote service URLs in `appsettings.json` or environment variables:
  - `Estimator:BaseUrl` in Takeoff to point to Estimator.
  - `Takeoff:BaseUrl` in Estimator to point to Takeoff.

## Important files
- `Takeoff/wwwroot/state-editor.html` — client UI for editing and recording takeoff actions
- `Estimator/wwwroot/state-viewer.html` — read-only viewer that shows Estimator state
- `Validation/TakeoffValidator.cs` — business rules enforced in Takeoff
- `Takeoff/Controllers/StateController.cs` — endpoints to manipulate state and send actions to Estimator
- `Estimator/Controllers/QuantitiesController.cs` — endpoint to receive and apply action lists
- `docs/takeoff-estimator-interaction.txt` — business-focused interaction description

## Notes
- This is a demo for local development. For production use add persistence, authentication, robust error handling, and observability.