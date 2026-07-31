# Timekeeping Middleware (refactored)

VPN-aware architecture for offsite ZKTeco biometric attendance → central HRIS.

## Architecture

```
Offsite PC                          VPN                         HQ
┌─────────────────────────────┐                 ┌──────────────────────────────┐
│ Timekeeping.Agent           │                 │ Timekeeping.IngestApi        │
│  (Windows Service, x86)     │   HTTPS/HTTP    │  (IIS / Kestrel on HQ LAN)   │
│                             │ ──────────────► │                              │
│  ZKTeco device (zkemkeeper) │                 │  API key auth                │
│  → SQLite buffer            │                 │  → HRIS SQL Server           │
│  → sync w/ VPN backoff      │                 │     timekeeping_LogRecord    │
│  → loopback /status         │                 └──────────────────────────────┘
│                             │
│ Timekeeping.Agent.Tray      │
│  (optional status UI)       │
└─────────────────────────────┘
```

### Why this is better than the old WinForms + direct SQL design

| Concern | Old | New |
|--------|-----|-----|
| Offsite credentials | SQL connection string on every PC | API key only; SQL stays at HQ |
| VPN drops | Timer sync, weak retry | SQLite queue + exponential backoff |
| Unmapped employees | Whole batch failed | Per-punch Accepted / Duplicate / Unmapped / Error |
| Reliability | UI process must stay open | Windows Service |
| Device watermark | PC clock (`DateTime.Now`) | Max punch timestamp from device |
| Ops visibility | Custom dashboard form | Loopback status + tray |

VPN note: the agent treats “API unreachable” as “VPN/network down” and backs off (5s → 300s). Punches keep collecting locally until the tunnel returns.

## Projects

| Project | Role | Where it runs |
|---------|------|----------------|
| `Timekeeping.Contracts` | Shared DTOs | Both |
| `Timekeeping.IngestApi` | Punch ingest + health + server time | HQ (behind VPN) |
| `Timekeeping.Agent` | Device poll + buffer + sync | Each offsite PC |
| `Timekeeping.Agent.Tray` | Status tray (optional) | Offsite PC |
| Legacy WinForms (`offSiteTimekeeping_NET8`) | Previous middleware | Keep until cutover |

## Quick start (dev)

### 1. HQ ingest API

Edit `src/Timekeeping.IngestApi/appsettings.json`:

- `ConnectionStrings:Hris` → SQL login with rights to read JointID / PMS and insert `timekeeping_LogRecord`
- `Ingest:ApiKey` → long random secret (same value on agents)

```powershell
dotnet run --project src/Timekeeping.IngestApi
```

### 2. Offsite agent

Edit `src/Timekeeping.Agent/appsettings.json`:

- Device IP / port / CommKey
- `Ingest:BaseUrl` → VPN-reachable URL of the ingest API
- Same `Ingest:ApiKey`

Register zkemkeeper first (`SDK/Register_SDK.bat` as admin), then:

```powershell
dotnet run --project src/Timekeeping.Agent
```

### 3. Tray (optional)

```powershell
dotnet run --project src/Timekeeping.Agent.Tray
```

## Production install (offsite agent as Windows Service)

```powershell
# From an elevated prompt, after publishing x86:
dotnet publish src/Timekeeping.Agent -c Release -r win-x86 --self-contained false -o C:\Timekeeping\Agent

sc.exe create "PVAOTimekeepingAgent" binPath= "C:\Timekeeping\Agent\Timekeeping.Agent.exe" start= auto
sc.exe description "PVAOTimekeepingAgent" "Polls ZKTeco device and syncs punches to HQ over VPN"
sc.exe start "PVAOTimekeepingAgent"
```

Publish the tray separately if operators need a UI. The tray only talks to `http://127.0.0.1:17890/status` on the local machine.

## Production install (HQ API)

Host `Timekeeping.IngestApi` on an internal IIS site or as a Windows Service / container **only reachable on the VPN/LAN**. Do not expose it to the public internet.

Prefer HTTPS with an internal cert. Keep SQL credentials only on the HQ host (user secrets / environment variables).

## Cutover from legacy middleware

1. Deploy Ingest API at HQ and verify `/health`.
2. Install Agent on one pilot site; confirm punches appear in `timekeeping_LogRecord`.
3. Stop/disable the old WinForms middleware on that PC.
4. Roll out remaining sites.
5. Retire `offSiteTimekeeping_NET8` when all sites are converted.

Local SQLite path for the new agent:

`%ProgramData%\TimekeepingAgent\punches.db`

After fixing an unmapped biometric ID in HRIS, requeue locally:

```powershell
Invoke-RestMethod -Method POST -Uri http://127.0.0.1:17890/requeue-unmapped
```

## Security checklist

- [ ] Strong unique `Ingest:ApiKey` (not the sample value)
- [ ] SQL account used by the API is least-privilege
- [ ] Ingest API bound to VPN/internal interface only
- [ ] Agent status URL stays on `127.0.0.1`
- [ ] Rotate API keys when a site PC is decommissioned
