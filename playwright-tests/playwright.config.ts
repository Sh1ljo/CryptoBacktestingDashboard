import { defineConfig, devices } from '@playwright/test';

// The .NET app runs on http://localhost:5080 under the "http" launch profile
// (Development environment => no HTTPS redirect, Swagger enabled).
// Override with BASE_URL if you host it elsewhere.
const BASE_URL = process.env.BASE_URL ?? 'http://localhost:5080';

export default defineConfig({
  testDir: './tests',
  // The scenario is sequential and stateful (each step builds on the last),
  // so keep a single worker and no parallelism.
  fullyParallel: false,
  workers: 1,
  timeout: 60_000,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: BASE_URL,
    trace: 'on-first-retry',
    ignoreHTTPSErrors: true,
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
  // Boots the ASP.NET Core app automatically before the tests run.
  // If you already have it running, Playwright reuses it (unless in CI).
  webServer: {
    command:
      'dotnet run --project ../CryptoBacktestingDashboard/CryptoBacktestingDashboard.csproj --launch-profile http',
    url: BASE_URL,
    reuseExistingServer: !process.env.CI,
    timeout: 180_000,
    stdout: 'pipe',
    stderr: 'pipe',
  },
});
