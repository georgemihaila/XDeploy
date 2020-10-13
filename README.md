# XDeploy

**Deploying applications on remote computers.**

XDeploy is a client–server application synchronization/deployment system. A central web server ("hub") stores application metadata and file blobs, while lightweight console clients run on the machines where applications live. Clients run in one of two complementary modes: a **Deployer**, which pushes a local application's files to the server, and an **Updater**, which pulls the files down onto a remote computer. Synchronization is delta-based (SHA-256 checksums), so only changed/new/removed files are transferred, and each application can define pre-deployment and post-deployment command-line actions (e.g., stop/start a service, restart a process) that run around a sync.

## Features

- **Server-side hub (ASP.NET Core web application)**
  - User accounts, roles, and login via ASP.NET Core Identity (scaffolded Razor Pages UI).
  - Register/edit/delete applications through the web dashboard; each app gets a unique ID.
  - Per-user API keys, generated from the dashboard; keys are stored as SHA-256 hashes.
  - Application settings per app: name, description, optional deployer IP restriction, optional encryption flag, and pre/post-deployment command lines.
  - REST API protected by HTTP **Basic authentication** (`base64(email:apiKey)`).
  - File storage in **MongoDB** (with SHA-256 checksum + size accounting); application metadata in **SQL Server** via Entity Framework Core (with migrations).
  - WebSocket endpoints for live status: update signals for updaters (`/api/ws`) and live dashboard status (`/ws/app-status`).
  - Authenticated `/config.json` endpoint that generates a ready-to-fill client configuration skeleton.

- **Deployer mode (client)**
  - Scans a local application folder, diffs it against the server's stored tree, uploads new/changed files (deduplicated by checksum), and reports removed files so the server can clean them up.
  - Locks the application on the server during upload (so updaters never fetch a half-updated state) and unlocks it afterwards, which notifies all connected updaters.
  - Runs a local HTTP signal server (default port `7745`, bound to `127.0.0.1`) so a sync can be triggered on demand, e.g. `http://127.0.0.1:7745?id=<appID>`.

- **Updater mode (client)**
  - Opens a WebSocket to the server per configured app and waits for "update available" signals.
  - Downloads changed/new files, removes locally deleted files, and runs the app's pre/post-deployment command-line actions around the update.

- **Shared core library** used by both client and server: REST API client, config model, SHA-256/Base64/AES-256 helpers, and disk I/O diffing utilities.
- **Parallel transfers** — up to 25 concurrent uploads/downloads per sync.

> **Note on encryption:** the codebase contains an `Encrypted` flag on applications, an `EncryptionKey` field in client config, and AES-256 file encrypt/decrypt helpers, but the file-encryption integration point (`FileEncrypter.EncryptFile` / `DecryptFile` in `XDeploy.Core`) currently throws `NotImplementedException`. In practice encryption is not yet wired into the transfer pipeline.

## Project structure

```
XDeploy/
├── .github/workflows/dotnetcore.yml   # GitHub Actions CI (dotnet build, Release)
├── XDeploy.sln                        # Solution (4 projects)
├── README.md
├── XDeploy.Core/                      # Shared class library (net5.0)
│   ├── ApplicationInfo.cs             # App descriptor used by the client
│   ├── XDeployAPI.cs                  # REST client for the server API
│   ├── StartupConfig.cs               # Client config model + ApplicationMode enum (Deployer|Updater)
│   ├── ProxyConfiguration.cs          # Optional HTTP proxy for API/WebSocket calls
│   ├── Cryptography/Cryptography.cs   # SHA-256, Base64, AES-256 file encryption helpers
│   ├── Exceptions/StartupException.cs
│   ├── Extensions/ListExtensions.cs   # ChunkBy helper
│   └── IO/
│       ├── FileInfo.cs                # Filename + SHA-256 + last modified
│       ├── IODifference.cs            # Diff entry (file/directory, add/remove/update)
│       ├── Extensions/IOExtensions.cs # Diff computation between two file trees
│       └── FileManagement/
│           ├── DiskFileManager.cs     # Local disk file enumeration/read/write/cleanup
│           └── Encryption/            # FileEncrypter (NotImplementedException) + enum
├── XDeploy.Deployer/                  # Client console app (assembly name: xdd)
│   ├── Program.cs                     # Entry point; takes a JSON config file as arg
│   ├── deployer.json                  # Example config (Deployer mode)
│   ├── updater.json                   # Example config (Updater mode)
│   └── Infrastructure/
│       ├── Abstractions/              # UpdateManager, ApplicationSynchronizerBase, interfaces
│       ├── Builders/UpdateManagerBuilder.cs  # Builds + validates client from config
│       ├── CommandLine/CommandLine.cs # Wraps cmd.exe for pre/post-deploy actions
│       ├── Deployment/                # ApplicationDeployer (upload), SyncSignalServer (NHttp HTTP listener)
│       ├── Update/                    # ApplicationUpdater (download), WebSocketsSignalNotifier
│       └── SynchronizationResult.cs
├── XDeploy.Server/                    # ASP.NET Core web app (hub) — net5.0
│   ├── Program.cs                     # Host builder (Kestrel + IIS integration in release)
│   ├── Startup.cs                     # DI, EF Core (SQL Server), Identity, MongoDB, MVC/Razor/WebSockets
│   ├── appsettings.json               # Connection strings + file storage settings
│   ├── appsettings.Development.json
│   ├── Controllers/
│   │   ├── HomeController.cs          # Dashboard, create/edit/delete apps, generate API key, /config.json
│   │   ├── APIController.cs           # REST API for the client (see API below)
│   │   ├── APIValidationBase.cs       # Basic-auth + ownership + optional IP validation
│   │   ├── APIWebSocketsController.cs              # /api/ws — update signals to updaters
│   │   ├── ApplicationStatusWebSocketsController.cs # /ws/app-status — live dashboard status
│   │   └── AdminController.cs         # Admin-role placeholder
│   ├── Data/AppIdentityDbContext.cs   # Identity DbContext (SQL Server)
│   ├── Infrastructure/Data/
│   │   ├── Application.cs             # App entity (ID, owner, name, IP restriction, encrypted, actions…)
│   │   ├── APIKey.cs                  # Per-user API key (hash stored)
│   │   ├── ApplicationDbContext.cs
│   │   ├── Binders/                   # Model binder that resolves an app ID to an Application entity
│   │   ├── Deployment/                # DeploymentJob / ExpectedFile entities
│   │   └── MongoDb/                   # MongoDbFileManager + ApplicationFile (blob storage)
│   ├── Migrations/                    # EF Core migrations (app db + identity db)
│   ├── Models/                        # EditAppViewModel, IndexViewModel, ErrorViewModel
│   ├── Views/                         # Razor views (dashboard, app editor, layout)
│   ├── Areas/Identity/                # Scaffolded ASP.NET Core Identity UI
│   ├── wwwroot/                       # Bootstrap, jQuery, Semantic UI, site assets
│   ├── WebsocketsIOC.cs               # Static in-process event hub for WebSocket broadcasts
│   ├── IApplication.cs
│   ├── GlobalSuppressions.cs
│   └── .config/dotnet-tools.json      # dotnet-ef tool manifest
└── XDeploy.Tests/                     # Manual test/scratch console app (not a unit-test suite)
```

## Tech stack

| Component | Technology |
|-----------|-----------|
| Language | C# |
| Target framework | .NET 5.0 (`net5.0`) — all four projects |
| Server | ASP.NET Core (MVC + Razor Pages + WebSockets), ASP.NET Core Identity + Identity UI |
| ORM / database | Entity Framework Core 5.0 (SQL Server provider); SQL Server for metadata/identity |
| File blob storage | MongoDB (MongoDB.Driver 2.11.2, GridFS/Bson) |
| JSON | Newtonsoft.Json 12.0.3 (Core) |
| HTTP server (client signal server) | NHttp 0.1.9 |
| WebSocket client | websocket-sharp-with-proxy-support 1.9.1 |
| Other packages | `Microsoft.Extensions.Logging.Debug`, `Microsoft.EntityFrameworkCore.Tools/Design`, `System.Configuration.ConfigurationManager` (5.0.0-rc.1) |
| EF tool | `dotnet-ef` 3.0.0 (`.config/dotnet-tools.json`) |
| Build tooling | `dotnet` CLI (`.sln`), Visual Studio 2019-era solution format |
| CI | GitHub Actions (`.github/workflows/dotnetcore.yml`), self-hosted runner, .NET SDK `5.0.100-preview.6.20265.1`, `dotnet build --configuration Release` |

Package versions in the server csproj reference .NET 5 **Release Candidate** builds (e.g., `5.0.0-rc.1.20451.17`), and the CI pins a preview .NET 5 SDK — the project is an early-.NET-5-era codebase.

## Build & run

### Prerequisites
- .NET 5 SDK (the project targets `net5.0`; CI used `5.0.100-preview.6.20265.1`).
- For the server at runtime: a **SQL Server** instance (metadata/identity) and a **MongoDB** instance (file blobs).

### Build the solution
```bash
dotnet build XDeploy.sln --configuration Release
```

### Run the server (hub)
```bash
dotnet run --project XDeploy.Server
```
- Default dev URL: `https://localhost:5001` (also `http://localhost:5000`).
- On first run, apply the EF Core migrations against your SQL Server (e.g., `dotnet ef database update` — requires the `dotnet-ef` tool, see `XDeploy.Server/.config/dotnet-tools.json`).
- The server will not work until `appsettings.json` is configured (see Configuration below).

### Run the client (deployer/updater)
The client is a console app (assembly name `xdd`) that requires a JSON configuration file as its argument:
```bash
dotnet run --project XDeploy.Deployer -- deployer.json     # Deployer mode
dotnet run --project XDeploy.Deployer -- updater.json     # Updater mode
```
The included `deployer.json` / `updater.json` are examples with placeholder credentials/endpoints; copy and adapt them for your setup.

### Tests
`XDeploy.Tests` is a small **manual console program** (not a unit-test framework), so `dotnet test` is not applicable to this repo as-is. It was used for ad-hoc local verification.

## Usage notes / configuration

### Client configuration (`deployer.json` / `updater.json`)
The client reads a `StartupConfig` JSON file (path passed as the first command-line argument). Recognized fields:

| Field | Description |
|-------|-------------|
| `Mode` | `"Deployer"` or `"Updater"` — select the sync direction. |
| `Endpoint` | Base URL of the XDeploy server (e.g., `https://localhost:44353`). |
| `Email` | The registered user's email (used with the API key for Basic auth). |
| `APIKey` | The user's API key (from the dashboard). |
| `SyncServerPort` | Port for the local sync-signal HTTP server (Deployer mode only; example uses `7745`). |
| `Apps` | Array of applications: `ID` (app ID from the server), `Location` (absolute local path of the app), optional `EncryptionKey`. |
| `Verbose` | Flag for detailed logging — **note:** the field is parsed but not actually consumed anywhere in the current code. |
| `Proxy` | Optional `{ "Address": "...", "credentials": ... }` HTTP proxy used for API and WebSocket connections. |

The server can also generate a config skeleton via the authenticated `/config.json` endpoint.

### Server configuration (`XDeploy.Server/appsettings.json`)
- `ConnectionStrings:DefaultConnection` — SQL Server connection string (metadata + Identity). The committed value points to an internal `10.2.0.8` address and **must be changed** for your environment.
- `ConnectionStrings:MongoDbConnection` — MongoDB connection string for file storage (committed example targets internal `10.2.0.173`).
- `MongoDbFileStorageDbName` — database name used for stored files (default `FileStorage`).
- `MaxProjectSizeBytes` — per-app storage quota shown in the dashboard (default `52428800` = 50 MB).
- The server project also defines a `UserSecretsId` (`aspnet-XDeploy.Server-9FAB833C-...`), and the repo `.gitignore` lists `appsettings.json`, so connection strings are clearly intended to be kept out of version control.

### REST API (consumed by the clients)
All endpoints under `/api` require an `Authorization: Basic base64(email:apiKey)` header; several also verify the caller owns the app, and optionally that the request comes from the app's restricted deployer IP.

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/ValidateCredentials` | POST | Validate a user's credentials |
| `/api/ListApps` | GET | List the authenticated user's applications |
| `/api/App?id=` | GET | Get app details (encrypted flag, pre/post-deploy actions) |
| `/api/RemoteFiles?id=` | GET | Get the stored file tree of an app (name + SHA-256) |
| `/api/HasFile?id=` | GET | Check whether the server already has a file (`Content-Location`, `X-SHA256` headers) |
| `/api/UploadFile?id=` | POST | Upload file bytes (`Content-Location`, `X-SHA256` headers) |
| `/api/DownloadFile?id=` | GET | Download a stored file (`Content-Location` header) |
| `/api/Cleanup?id=` | POST | Delete files removed locally (JSON body of `IODifference`s) |
| `/api/LockApplication?id=` | POST | Lock an app (terminates update jobs during upload) |
| `/api/UnlockApplication?id=` | POST | Unlock an app + notify updaters + update size/last-update |
| `/api/ws` | GET (WebSocket) | Updater listens for "update available" signals |
| `/ws/app-status` | GET (WebSocket) | Dashboard live status (locked changes, size progress) |

### Web dashboard
After registering/login, a user can generate an API key, register applications, configure them (IP restriction, encryption flag, pre/post-deploy command lines), and download a client config skeleton. The dashboard uses Semantic UI and shows live quota/status via WebSocket.

## License

**No license file was found in the repository** (no `LICENSE`, `LICENSE.md`, or `COPYING` in the root). The only license files present are the bundled third-party libraries under `XDeploy.Server/wwwroot/...` (Bootstrap, jQuery, jQuery Validation). Absent an explicit license, no usage terms are granted by the repository itself — treat the code as all-rights-reserved until the author adds a license.
