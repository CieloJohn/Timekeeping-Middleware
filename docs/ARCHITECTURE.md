# Architecture — VPN-aware biometric sync

## Data flow

1. **Device poll (every ~10s)**  
   `ZkDeviceService` (STA thread + zkemkeeper COM) connects to the ZKTeco device, reads general logs, and inserts only punches newer than the stored watermark into SQLite.

2. **Local durability**  
   `%ProgramData%\TimekeepingAgent\punches.db` holds punches with states: `Pending`, `Synced`, `Unmapped`, `Failed`.

3. **VPN sync (every ~30s when healthy)**  
   `SyncWorker` calls `GET /health`. If unreachable, it exponential-backs off (VPN flap). If healthy, it POSTs a batch to `/api/punches/batch`.

4. **HQ ingest**  
   `Timekeeping.IngestApi` authenticates with `X-Api-Key`, resolves `biometric_id → emp_id → employee_id`, dedupes, and inserts into `[HRIS].[dbo].[timekeeping_LogRecord]`. Per-punch results are returned so one bad enroll does not block the batch.

5. **Operator visibility**  
   Agent hosts loopback `GET /status`. Tray app polls it; no VPN required for local monitoring.

## VPN failure modes

| Failure | Behavior |
|---------|----------|
| VPN down / API timeout | Punches stay Pending; backoff 5s→300s; device polling continues |
| SQL down at HQ | `/health` degraded or batch errors; agent backs off |
| Employee not mapped | Punch marked Unmapped (not retried forever as Pending); fix JointID then requeue manually if needed |
| Device offline | Agent reconnects on next poll; queue unchanged |

## Idempotency

Duplicates are treated as success (`Synced`) so a VPN drop mid-response does not create double punches on retry. Dedup key uses `logtime + device_sn` (legacy-compatible) plus employee/action refinement where available.

## Why not direct SQL over VPN?

SQL over VPN works on a trusted network, but:

- Every site PC would hold a DB login
- Schema/auth changes require redeploying every agent
- Harder to audit and rate-limit
- Firewall rules are coarser (SQL port vs one HTTPS service)

The ingest API keeps SQL at HQ while agents only need the VPN path to that one HTTP endpoint.
