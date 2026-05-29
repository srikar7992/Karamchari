# Production Configuration Audit

## Files Found

- `src/Backend/Karamchari.Api/appsettings.json`
- `src/Backend/Karamchari.Api/appsettings.Development.json`

Files not found:

- `appsettings.Local.json`
- `appsettings.Staging.json`
- `appsettings.Production.json`

## Findings

| Configuration | Result | Evidence |
|---|---|---|
| Production JWT placeholder protection | PASS | `Program.cs` refuses non-Development/Local startup if `Jwt:Secret` is missing, placeholder, or under 32 bytes. |
| Committed JWT placeholder | ACCEPTABLE WITH GUARD | `appsettings.json` contains `REPLACE_VIA_ENV...change_me...`; guarded in production. |
| Development SQL password | DEV RISK | `appsettings.Development.json` contains `Password=Karamchari@123`. |
| RabbitMQ guest credentials | DEV RISK | `amqp://guest:guest@localhost:5672`. |
| AllowedHosts | REVIEW | `AllowedHosts: "*"`. |
| Trusted gateway fingerprint | DEV ONLY | Development config sets `local-dev-gateway`; production value empty in base config. |
| DocumentIntelligence secrets | PASS/EMPTY | Development `Endpoint` and `ApiKey` are empty. |
| Missing environment-specific production config | FAIL | No `appsettings.Production.json` or staging config found. |

## Verdict

Configuration is guarded against the worst production JWT placeholder failure, but production configuration is not fully certifiable because staging/production config files and secret wiring contracts are absent.
