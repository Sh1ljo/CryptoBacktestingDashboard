# Lab 5 - Implementacija

Ovaj dokument opisuje sve tehničke korake napravljene unutar rješenja `CryptoBacktestingDashboard` kako bi se ostvarili zahtjevi vježbe Lab 5. Zbog prirode aplikacije (kripto strategije i parovi, umjesto kvizova), koncepti su mapirani gdje imaju najviše smisla (npr. uploadi se vežu uz trading strategiju). 

## 1. Identity, Auth i OAuth (Google)
- **Proširenje korisnika:** Kreiran je `AppUser.cs` unutar modela koji nasljeđuje `IdentityUser` te sadrži obavezna polja `OIB` (11 znakova, string, reg-ex za brojeve) i `JMBG` (13 znakova, string, reg-ex za brojeve) točno prema naputku.
- **Konfiguracija baze:** `ApplicationDbContext` je izmjenjen da nasljeđuje `IdentityDbContext<AppUser>`. Napravljena je i pushana EF migracija `AddAttachments` i UI nadogradnje (`Register.cshtml` / `ExternalLogin.cshtml`).
- **Google Login:** Dodan je novi Authentication provider u `Program.cs` na način da čita `ClientId` i `ClientSecret` iz app sekreta, ali pruža safe-fallback ako oni izostanu kako aplikacija ne bi "pucala".
- **Autorizacije i UI:** Postojeći i API controlleri obogaćeni su `[Authorize]` logikom i `AllowAnonymous` dekoracijama. Scaffoldani su Identity ekrani (`Register`, `ExternalLogin`) i dodana HTML markup polja za OIB i JMBG sa server-side validacijom.

## 2. API i DTO Klase
- **DTO strukture:** Izrađeni su DTO predlošci `CryptoPairDTO` i `BacktestStrategyDTO`.
- **API Controlleri:** Dodani su u novu rutu `Controllers/Api/*`. Kreirani su `CryptoPairApiController` i `BacktestStrategyApiController`. Obje klase podržavaju kompletni CRUD (`GET`/`POST`/`PUT`/`DELETE`), query search funkcije (filtracija preko url parametara), model-state validaciju i DTO vraćanje. Na kontrolere je vezana `[Authorize]` blokada gdje se anonimnim korisnicima dopušta tek čitanje listi.

## 3. Dropzone Asinkrone Datoteke (Upload)
- **Model Attachment:** Dokumenti (Manualovi i slike) vežu se uz `BacktestStrategy` klasu formirajući `Attachment` model s podacima za FilePath, Size, Name i MimeType. 
- **Upload sekcija:** Oblikovana kao grid layout integriran unutar `Views/BacktestStrategy/Edit.cshtml`. Dropzone script je dodan u `Scripts` block i vezan uz novokreirane metode.
- **BacktestStrategyController metode:**
    - `UploadAttachment` omogućuje asinkrono slanje s auto-generiranim GUID-om filea. 
    - `GetAttachments` hvata `_AttachmentList.cshtml` pre-rendani kod za AJAX ubacivanje u DOM.
    - `DeleteAttachment` rukuje brisanjem zapisa iz baze i iz foldera aplikacije. 

## 4. Integracijski Testovi
- Kreiran je poseban .NET 8 xUnit testni projekt naziva `CryptoBacktestingDashboard.Tests`.
- U projektu je dodana referenca na bazni repozitorij gdje se koristi `WebApplicationFactory<Program>` sa svrhom bootanja aplikacijskih procesa.
- Database konfiguracija i DbContext rutiran je na isključivo lokalni `.UseInMemoryDatabase("InMemoryDbForTesting")`.
- Testovi potvrđuju prolaženje uspješnih scenarija te ruše autorizacijske pozive (npr. ne-autentificiran `POST` dobiva presretnuti 302 Redirect ili error status).

## Kako testirati?
- **Identity Setup:** Stranica ima registracijski link, pri samoj validaciji aplikacija ne dopušta faličan OIB format i neće kreirati zapis ako uvjeti nisu sravnjani.
- **Dropzone:** Prebacite se pod `Strategies` -> `Edit` -> Povući dokument u sivkasti dropzone blok na dnu UI grida. Datoteka se pojavljuje nakon uploada i ima ikonu za "Obriši" s AJAX callbackom.
- **API pozivi:** Otići primjerice na `https://localhost:XXXX/api/pairs` (vratiti će se JSON s DTO odgovorom). Ako pokušate poslati POST na istu rutu s postman-a dobiti ćete Redirect k `Identity/Login` ruti zbog falše Autorizacije.
- **Testovi:** U terminalu unutar `CryptoBacktestingDashboard.Tests` putanje izvršiti naredbu `dotnet test`. Svi testovi trebaju se zasijati zeleno u rasponu par sekundi.