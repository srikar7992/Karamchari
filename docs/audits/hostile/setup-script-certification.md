# Setup Script Certification

Scripts audited:

- `setup-local.sh`
- `setup-local.ps1`

## Verification Executed

```bash
./setup-local.sh --fresh --no-run
./setup-local.sh
```

## Results

| Responsibility | Result | Evidence |
|---|---|---|
| Validate Docker | PASS outside sandbox | Docker CLI/daemon verified. |
| Validate .NET SDK | PASS | `.NET SDK: 10.0.300`. |
| Required ports | PARTIAL | Script reports occupied ports as warnings, not hard failures. Existing services can mask conflicts. |
| Certificates | PARTIAL | API health over HTTPS succeeded during script; script does not explicitly validate ASP.NET dev cert state. |
| Environment variables | PARTIAL | Script exports connection strings, but does not validate all required app configuration. |
| Containers | PASS | SQL, Redis, RabbitMQ, OTel, Seq, Prometheus, Grafana, Mailpit, Azurite started. |
| Database connectivity | PASS | SQL accepted connections. |
| Redis/RabbitMQ/OTel/Seq/Prometheus/Grafana | PARTIAL | Containers start; only SQL is deeply validated in script. |
| Restore | PASS | `dotnet restore` succeeded. |
| Build | PASS | `dotnet build` succeeded. |
| Provision | PASS | `--provision-dev-tenants` returned exit 0. |
| Migrate | PASS by provisioning side effect | Verified identity tables, tenant schemas, RLS policies. |
| Seed | PASS | Seed applied. |
| Verify | PASS for DB/RLS/signing key | Script checks identity table count, tenant schema count, RLS policy count, signing key count. |
| Generate report | PASS | `docs/hostile-audit/setup-report.txt` produced. |

## Hostile Finding

`setup-local.sh` returned success after health reached HTTP 200, but the API process was not reachable after the script exited in this execution harness. The script was patched to start the built DLL with `nohup`, absolute content root, explicit URLs, and `/dev/null` stdin, but the process still could not be observed after command exit here.

Because the user-facing requirement is "new developer executes setup-local and nothing else", this is not fully certified from this environment.

## Verdict

Setup script: Conditionally certified for bootstrap/provisioning. Not certified for persistent runtime after command return in this harness.
