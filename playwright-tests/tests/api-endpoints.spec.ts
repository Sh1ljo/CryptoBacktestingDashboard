import { test, expect, type Page } from '@playwright/test';

/**
 * End-to-end API scenario for the CryptoBacktestingDashboard REST endpoints.
 *
 * A single 10-step scenario (`test.step`) that:
 *   - registers a fresh user through the real Identity UI (grants the "User" role
 *     and drops an auth cookie into the browser context),
 *   - then exercises every CRUD API controller using `page.request`, which shares
 *     that cookie, so the authorized POST/PUT calls succeed,
 *   - and finally proves role enforcement (a "User" cannot DELETE — that is Admin-only).
 *
 * Endpoints covered:
 *   GET/POST/PUT/DELETE /api/pairs        (list, by-id, create, update, forbidden-delete)
 *   GET/POST            /api/indicators
 *   GET/POST            /api/strategies
 *   GET/POST            /api/sessions
 *   GET                 /api/search
 *
 * (Optimization and AI-chat endpoints are intentionally out of scope: they require
 *  seeded candle data and an external DeepSeek API key respectively.)
 */

// A unique run id keeps the registered account / created rows collision-free
// across repeated runs against the same LocalDB database.
const RUN = Date.now().toString();

const USER = {
  email: `pw_${RUN}@example.com`,
  password: 'Passw0rd!',
  oib: RUN.slice(-11), // OIB: exactly 11 digits
  jmbg: RUN.padStart(13, '0').slice(-13), // JMBG: exactly 13 digits
};

async function registerAndLogin(page: Page): Promise<void> {
  await page.goto('/Identity/Account/Register');
  await page.locator('#Input_Email').fill(USER.email);
  await page.locator('#Input_OIB').fill(USER.oib);
  await page.locator('#Input_JMBG').fill(USER.jmbg);
  await page.locator('#Input_Password').fill(USER.password);
  await page.locator('#Input_ConfirmPassword').fill(USER.password);

  await Promise.all([
    page.waitForURL('/'), // Register redirects to home on success
    page.locator('#registerSubmit').click(),
  ]);
}

test('API scenario: full CRUD lifecycle across all endpoints (10 steps)', async ({ page }) => {
  // IDs captured as the scenario progresses.
  let pairId = 0;
  let indicatorId = 0;
  let strategyId = 0;
  let sessionId = 0;

  // ── Step 1 ─ Authenticate: register a brand-new user via the UI ───────────
  await test.step('1. Register a new user and obtain an authenticated session', async () => {
    await registerAndLogin(page);
    // The auth cookie now lives in the browser context; page.request reuses it.
    expect(page.url()).toContain('localhost');
  });

  // ── Step 2 ─ GET /api/pairs (anonymous read, list) ────────────────────────
  await test.step('2. GET /api/pairs returns the pair list', async () => {
    const res = await page.request.get('/api/pairs');
    expect(res.status()).toBe(200);
    const body = await res.json();
    expect(Array.isArray(body)).toBe(true);
  });

  // ── Step 3 ─ POST /api/pairs (create, authorized) ─────────────────────────
  await test.step('3. POST /api/pairs creates a crypto pair', async () => {
    const res = await page.request.post('/api/pairs', {
      data: {
        symbol: `PW${RUN.slice(-6)}/USD`,
        baseAsset: 'PW',
        quoteAsset: 'USD',
        currentPrice: 123.45,
      },
    });
    expect(res.status()).toBe(201);
    const created = await res.json();
    expect(created.id).toBeGreaterThan(0);
    pairId = created.id;
  });

  // ── Step 4 ─ GET /api/pairs/{id} (read by id) ─────────────────────────────
  await test.step('4. GET /api/pairs/{id} returns the created pair', async () => {
    const res = await page.request.get(`/api/pairs/${pairId}`);
    expect(res.status()).toBe(200);
    const dto = await res.json();
    expect(dto.id).toBe(pairId);
    expect(dto.baseAsset).toBe('PW');
  });

  // ── Step 5 ─ PUT /api/pairs/{id} (update, authorized) ─────────────────────
  await test.step('5. PUT /api/pairs/{id} updates the pair price', async () => {
    const res = await page.request.put(`/api/pairs/${pairId}`, {
      data: {
        id: pairId,
        symbol: `PW${RUN.slice(-6)}/USD`,
        baseAsset: 'PW',
        quoteAsset: 'USD',
        currentPrice: 999.99,
      },
    });
    expect(res.status()).toBe(200);
    const dto = await res.json();
    expect(dto.currentPrice).toBe(999.99);
  });

  // ── Step 6 ─ POST + GET /api/indicators ───────────────────────────────────
  await test.step('6. POST /api/indicators creates an indicator, GET lists it', async () => {
    const create = await page.request.post('/api/indicators', {
      data: {
        name: `RSI-${RUN.slice(-6)}`,
        type: 0, // IndicatorType.RSI
        period: 14,
        threshold: 70,
        description: 'Playwright test indicator',
      },
    });
    expect(create.status()).toBe(201);
    indicatorId = (await create.json()).id;
    expect(indicatorId).toBeGreaterThan(0);

    const list = await page.request.get('/api/indicators');
    expect(list.status()).toBe(200);
    const items = await list.json();
    expect(items.some((i: { id: number }) => i.id === indicatorId)).toBe(true);
  });

  // ── Step 7 ─ POST + GET /api/strategies ───────────────────────────────────
  await test.step('7. POST /api/strategies creates a strategy, GET lists it', async () => {
    const create = await page.request.post('/api/strategies', {
      data: {
        name: `Strategy-${RUN.slice(-6)}`,
        description: 'Playwright test strategy',
        isActive: true,
        stopLossPercent: 5,
        takeProfitPercent: 10,
      },
    });
    expect(create.status()).toBe(201);
    strategyId = (await create.json()).id;
    expect(strategyId).toBeGreaterThan(0);

    const list = await page.request.get('/api/strategies');
    expect(list.status()).toBe(200);
    const items = await list.json();
    expect(items.some((s: { id: number }) => s.id === strategyId)).toBe(true);
  });

  // ── Step 8 ─ POST + GET /api/sessions ─────────────────────────────────────
  await test.step('8. POST /api/sessions creates a session linking pair + strategy', async () => {
    const create = await page.request.post('/api/sessions', {
      data: {
        strategyId,
        cryptoPairId: pairId,
        startDate: '2024-01-01T00:00:00Z',
        endDate: '2024-03-01T00:00:00Z',
        initialBalance: 10000,
        isOptimized: false,
      },
    });
    expect(create.status()).toBe(201);
    const session = await create.json();
    sessionId = session.id;
    expect(sessionId).toBeGreaterThan(0);
    expect(session.strategyId).toBe(strategyId);
    expect(session.cryptoPairId).toBe(pairId);

    const list = await page.request.get('/api/sessions');
    expect(list.status()).toBe(200);
    const items = await list.json();
    expect(items.some((s: { id: number }) => s.id === sessionId)).toBe(true);
  });

  // ── Step 9 ─ GET /api/search (global search) ──────────────────────────────
  await test.step('9. GET /api/search finds the newly created data', async () => {
    const res = await page.request.get(`/api/search?q=Strategy-${RUN.slice(-6)}`);
    expect(res.status()).toBe(200);
    const result = await res.json();
    expect(result.totalCount).toBeGreaterThan(0);
    expect(
      result.results.some((r: { type: string }) => r.type === 'Strategy'),
    ).toBe(true);
  });

  // ── Step 10 ─ DELETE /api/pairs/{id} is Admin-only => access denied ───────
  await test.step('10. DELETE /api/pairs/{id} is forbidden for the User role', async () => {
    // [Authorize(Roles = "Admin")] rejects the "User" role. With Identity's cookie
    // auth that surfaces as a 302 redirect to the AccessDenied page (not a bare 403).
    const del = await page.request.delete(`/api/pairs/${pairId}`, { maxRedirects: 0 });
    expect(del.status()).toBe(302);
    expect(del.headers()['location']).toContain('/Identity/Account/AccessDenied');

    // ...and the pair is therefore still present (the delete did not happen).
    const stillThere = await page.request.get(`/api/pairs/${pairId}`);
    expect(stillThere.status()).toBe(200);
  });
});
