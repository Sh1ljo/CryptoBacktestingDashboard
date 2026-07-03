# Lab 5 — što je napravljeno, gdje se nalazi, što znati

Ova bilješka prati promjene napravljene za Lab 5 (predaja 12.6.). Organizirano je po
kriteriju ocjenjivanja iz `CryptoBacktestingDashboard/lab-5/Lab5.md`.

---

## 1. API CRUD za sve entitete (DTO) — 2 boda

### Lokacije
- `CryptoBacktestingDashboard/Controllers/Api/`
  - `CryptoPairApiController.cs` — `api/pairs` (postojao prije)
  - `BacktestStrategyApiController.cs` — `api/strategies` (postojao prije)
  - `IndicatorApiController.cs` — `api/indicators` (**novo**)
  - `BacktestSessionApiController.cs` — `api/sessions` (**novo**)
- `CryptoBacktestingDashboard/Models/DTO/`
  - `CryptoPairDTO.cs`, `BacktestStrategyDTO.cs` (postojeći)
  - `IndicatorDTO.cs` (**novo**)
  - `BacktestSessionDTO.cs` (**novo**, sadrži ugniježđene `Strategy` i `CryptoPair` DTO + `Profit`/`ROI`)

### Obrazac (isti za sva 4 controllera)
- `[Route("api/...")]`, `[ApiController]`, `[Authorize]` na klasi
- `[AllowAnonymous]` na `GET` (lista + `GET /{id}`), uz opcionalni `?q=` query parametar za pretragu
- `[Authorize(Roles = "Admin,User")]` na `POST`/`PUT`
- `[Authorize(Roles = "Admin")]` na `DELETE`
- Ručno mapiranje entitet → DTO (privatna `ToDTO` metoda)
- `POST` vraća `201 Created` preko `CreatedAtAction`
- `PUT` provjerava `id != dto.Id` → `400`, zatim `404` ako zapis ne postoji
- `DELETE` → `404` ako ne postoji, inače `200`

### Što znati
- `BacktestSessionDTO` ima ugniježđene `Strategy`/`CryptoPair` DTO-ove jer je
  `BacktestSessionRepository.GetItemsAsync()/GetItemAsync()` već radio `.Include()` na
  obje navigacije — DTO ih samo izlaže klijentu u "lijepom" obliku.
- Kod `POST /api/sessions`, `FinalBalance` se postavlja na `0` i `ExecutedAt` na
  `DateTime.Now` na serveru — klijent ne šalje te vrijednosti (sesija se "pokreće" tek
  kroz `/backtests/{id}/run`, API samo kreira definiciju).
- `IndicatorDTO.Name` ima `[Required]`; `CryptoPairDTO`/`BacktestStrategyDTO` koriste
  **implicitni required** koji ASP.NET Core automatski dodaje na ne-nullable `string`
  propertyje kada je `<Nullable>enable</Nullable>` uključen (`.csproj`).

---

## 2. Autentikacija (local accounts) + autorizacija — 1 bod

### Lokacije
- `CryptoBacktestingDashboard/Program.cs`
  - `app.UseAuthentication()` dodan **prije** `app.UseAuthorization()` (prije je
    potpuno nedostajao)
  - Seed rola `"Admin"` i `"User"` preko `RoleManager<IdentityRole>` pri startu
  - **Backfill** postojećih korisnika bez rola → automatski dobiju rolu `"User"`
    (rješava "AccessDenied" za račune kreirane prije nego su role postojale)
- Svi MVC controlleri imaju `[Authorize]` na klasi + `[AllowAnonymous]`/`[Authorize(Roles=...)]` po akciji:
  - `BacktestSessionController.cs`, `BacktestStrategyController.cs`,
    `CryptoPairController.cs`, `IndicatorController.cs`, `CandleDataController.cs`
- `Areas/Identity/Pages/Account/Register.cshtml.cs` — nakon uspješne registracije,
  korisnik se automatski dodaje u rolu `"User"` (`AddToRoleAsync`)
- `Areas/Identity/Pages/Account/ExternalLogin.cshtml.cs` — isto za korisnike koji se
  registriraju kroz Google login
- `Areas/Identity/Pages/Account/Login.cshtml` + `Login.cshtml.cs` (**novo**) —
  scaffoldana Login stranica, stilizirana istim dizajn-sustavom (`page-hero`,
  `form-card`, `form-shell`, `btn btn-primary`...) kao Register stranica, s linkovima
  na "Forgot your password?" i "Register as a new user"

### Pravila pristupa (pattern ponovljen za sve entitete)
| Akcija | Pristup |
| --- | --- |
| `Index`, `Details`, `Search` | Javno (`[AllowAnonymous]`) |
| `Create`, `Edit`, `Run` (backtest) | `Admin` ili `User` |
| `Delete` | Samo `Admin` |

### Što znati
- **Redoslijed `UseAuthentication()` → `UseAuthorization()` je kritičan.** Bez
  prvog, `HttpContext.User` nikad nije ispravno popunjen iz cookie-a pa
  `[Authorize]` ne radi kako treba.
- **Role su zapisane u auth cookie-u u trenutku prijave.** Ako se korisniku
  doda/promijeni rola u bazi dok je već prijavljen, mora se **odjaviti i ponovno
  prijaviti** da promjena postane vidljiva (cookie se mora regenerirati).
- Backfill u `Program.cs` rješava postojeće korisnike samo **jednom, pri sljedećem
  pokretanju aplikacije** — nakon toga svi novi korisnici dobivaju rolu odmah kroz
  Register/ExternalLogin.
- `CandleDataController` nema `Delete` akciju, pa ni `[Authorize(Roles="Admin")]`
  delete-pravilo za njega ne postoji — to je očekivano (akcija ne postoji).

---

## 3. Upload datoteka (Dropzone) — 1 bod

### Lokacije (postojeće iz ranijeg rada, nije mijenjano u ovoj sesiji)
- Model `Attachment` (`Models/Crypto/Attachment.cs`)
- `BacktestStrategyController.cs` — `UploadAttachment`, `DeleteAttachment`,
  `GetAttachments` akcije
- Strategy Edit view — Dropzone forma + `_AttachmentList` partial

### Što znati
- Upload je vezan uz **konkretnu strategiju** (`StrategyId`) i dostupan samo na Edit
  formi (strategija mora već imati ID).
- `UploadAttachment` je `[Authorize(Roles = "Admin,User")]`, `DeleteAttachment` isto;
  `GetAttachments` je `[AllowAnonymous]` (AJAX popis datoteka).
- Datoteke se spremaju na disk (`wwwroot/uploads/...`), metapodaci (naziv, putanja,
  veličina, content-type) u tablicu `Attachments`.

---

## 4. Google OAuth (3rd party login) — 1 bod

### Lokacije
- `Program.cs` — `AddAuthentication().AddGoogle(...)`, čita `ClientId`/`ClientSecret`
  iz konfiguracije, s `"dummy-client-id"/"dummy-client-secret"` fallbackom ako su
  praznе (npr. u testovima)
- `appsettings.json` — `Authentication:Google:ClientId/ClientSecret` su sada **prazni
  stringovi** (tajne su maknute iz repozitorija)
- User secrets (lokalno, izvan repozitorija) — sadrže stvarni `ClientId`/`ClientSecret`
  (`UserSecretsId` u `.csproj`: `212acf84-f382-456f-b380-274242496c4d`)

### Što znati
- ⚠️ **Stari Google secret je i dalje u git historiji** (commit `9a64f7d` i raniji,
  vrijednost je počinjala s `GOCSPX-...`). Brisanje iz `appsettings.json` ga ne briše
  iz historije — preporuka je **rotirati ključ u Google Cloud Console** prije nego
  repozitorij postane javan/predan.
- Da provjeriš lokalnu konfiguraciju: `dotnet user-secrets list --project
  CryptoBacktestingDashboard`.
- "Login with Google" gumb se prikazuje na Login i Register stranicama automatski
  ako su `ExternalLogins` dostupni (`_signInManager.GetExternalAuthenticationSchemesAsync()`).

---

## 5. Integracijski testovi za API — 2 boda

### Lokacije
- `CryptoBacktestingDashboard.Tests/`
  - `TestAuthHandler.cs` (**novo**) — custom auth scheme za testove
  - `CryptoPairApiControllerTests.cs` — proširen (bilo 2 testa, sada puni CRUD)
  - `BacktestStrategyApiControllerTests.cs` (**novo**)
  - `IndicatorApiControllerTests.cs` (**novo**)
  - `BacktestSessionApiControllerTests.cs` (**novo**)
  - `UnitTest1.cs` — **obrisan** (prazan placeholder)

Ukupno **42 testa**, sva prolaze (`dotnet test`).

### Pattern po controlleru
Svaki test file pokriva (kao u Lab5 checklisti "Što minimalno testirati"):
- `GET` svih zapisa (anonimno) → `200`
- `GET /{id}` postojeći → `200` + ispravan DTO
- `GET /{id}` nepostojeći → `404`
- `POST` bez autorizacije → `401`
- `POST` s neispravnim modelom (npr. bez `Name`) → `400`
- `POST` s odgovarajućom rolom → `201 Created`
- `PUT` postojeći zapis → `200` + ažurirani podaci
- `PUT` nepostojeći zapis → `404`
- `DELETE` s pogrešnom rolom (`User` umjesto `Admin`) → `403`
- `DELETE` s `Admin` rolom → `200`, zapis nestaje
- `DELETE` nepostojeći zapis → `404`

### `TestAuthHandler` — kako radi
- Definiran u `CryptoBacktestingDashboard.Tests/TestAuthHandler.cs`
- Svaka test-klasa u `WithWebHostBuilder` registrira ga kao **default auth scheme**:
  ```csharp
  services.AddAuthentication(TestAuthHandler.SchemeName)
      .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
  ```
- Ako request **nema** header `X-Test-Role`, handler vraća `AuthenticateResult.NoResult()`
  → korisnik je anoniman → `[Authorize]` vraća `401` (default `HandleChallengeAsync`).
- Ako request **ima** `X-Test-Role: Admin` (ili `User`), handler kreira
  `ClaimsPrincipal` s tom rolom → `[Authorize(Roles=...)]` prolazi ili vraća `403`
  ako rola ne odgovara.
- Helper `AuthorizedClient(role)` u svakom test-fileu samo doda taj header na
  `HttpClient`.

### Što znati
- Svaka test-klasa koristi **svoju InMemory bazu** (drugačiji naziv —
  `"CryptoPairApiTests"`, `"IndicatorApiTests"`, itd.) da se testovi ne mješaju.
- Razlog zašto je stari `Post_ShouldReturnUnauthorized_WhenAnonymous` test promijenio
  očekivani status iz `Redirect` (302, cookie-redirect na login) u `Unauthorized`
  (401): nakon dodavanja `TestAuthHandler`-a kao default scheme, anonimni zahtjev
  dobiva `401` umjesto preusmjeravanja na login stranicu — ispravnije za API.
- `dotnet build`/`dotnet test` mogu pucati s `MSB3027`/`MSB3021` ako je app pokrenut
  (`dotnet run`) jer drži `.exe` zaključan — to **nije compile error**, samo treba
  zaustaviti pokrenutu instancu prije builda.

---

## Brzi popis novih/izmijenjenih datoteka u ovoj sesiji

```
Program.cs                                           — auth middleware, role seed + backfill
appsettings.json                                     — Google secrets maknuti
CryptoBacktestingDashboard.csproj                    — UserSecretsId

Controllers/BacktestSessionController.cs             — [Authorize]/[AllowAnonymous]/role rules
Controllers/BacktestStrategyController.cs            — isto
Controllers/CryptoPairController.cs                  — isto
Controllers/IndicatorController.cs                   — isto
Controllers/CandleDataController.cs                  — isto

Controllers/Api/IndicatorApiController.cs            — NOVO
Controllers/Api/BacktestSessionApiController.cs      — NOVO
Models/DTO/IndicatorDTO.cs                           — NOVO
Models/DTO/BacktestSessionDTO.cs                     — NOVO

Areas/Identity/Pages/Account/Register.cshtml.cs      — AddToRoleAsync(user, "User")
Areas/Identity/Pages/Account/ExternalLogin.cshtml.cs — AddToRoleAsync(user, "User")
Areas/Identity/Pages/Account/Login.cshtml            — NOVO (stilizirana Login forma)
Areas/Identity/Pages/Account/Login.cshtml.cs         — NOVO (standardni Identity flow)

CryptoBacktestingDashboard.Tests/TestAuthHandler.cs                 — NOVO
CryptoBacktestingDashboard.Tests/CryptoPairApiControllerTests.cs    — proširen
CryptoBacktestingDashboard.Tests/BacktestStrategyApiControllerTests.cs — NOVO
CryptoBacktestingDashboard.Tests/IndicatorApiControllerTests.cs        — NOVO
CryptoBacktestingDashboard.Tests/BacktestSessionApiControllerTests.cs  — NOVO
CryptoBacktestingDashboard.Tests/UnitTest1.cs                       — OBRISAN
```

## Prije predaje — checklist
- [ ] Rotirati Google OAuth ključ (curio u git historiji)
- [ ] `dotnet build` i `dotnet test` prolaze (zaustaviti `dotnet run` prije builda)
- [ ] Odjaviti se i ponovno prijaviti nakon prvog pokretanja s novim kodom (role
      backfill + cookie refresh)
- [ ] Provjeriti da Edit/Delete na strategijama/sesijama radi za prijavljenog
      korisnika (rola `User` ili `Admin`)
