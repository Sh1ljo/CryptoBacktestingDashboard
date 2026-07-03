# Playwright API Tests

A single **10-step Playwright scenario** that exercises every REST API endpoint of
the CryptoBacktestingDashboard app.

The scenario registers a real user through the Identity UI (which grants the `User`
role and sets an auth cookie), then reuses that cookie via `page.request` to call the
JSON APIs — so the authorized create/update calls actually succeed, and the final step
proves that `DELETE` is correctly rejected for a non-admin.

## Endpoints covered

| Step | Endpoint | What it checks |
|------|----------|----------------|
| 1 | `POST /Identity/Account/Register` | Register + login (auth cookie) |
| 2 | `GET /api/pairs` | List pairs |
| 3 | `POST /api/pairs` | Create pair (201) |
| 4 | `GET /api/pairs/{id}` | Read pair by id |
| 5 | `PUT /api/pairs/{id}` | Update pair |
| 6 | `POST` + `GET /api/indicators` | Create + list indicator |
| 7 | `POST` + `GET /api/strategies` | Create + list strategy |
| 8 | `POST` + `GET /api/sessions` | Create + list session |
| 9 | `GET /api/search` | Global search finds the new data |
| 10 | `DELETE /api/pairs/{id}` | Forbidden (403) for `User` role |

## Prerequisites

- Node.js 18+ (tested on 22)
- The .NET 8 SDK and SQL Server LocalDB (the app auto-migrates on startup)

## Setup

```bash
cd playwright-tests
npm install
npx playwright install chromium
```

## Run

```bash
npm test              # headless
npm run test:headed   # watch the browser
npm run report        # open the HTML report afterwards
```

Playwright starts the ASP.NET Core app itself (via the `webServer` block in
`playwright.config.ts`, using `dotnet run --launch-profile http` on
`http://localhost:5080`). If you already have the app running it will reuse it.

Override the target with `BASE_URL=http://localhost:1234 npm test`.
