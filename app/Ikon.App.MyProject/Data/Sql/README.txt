LungFirst / COPD CDS — test database (PostgreSQL)

Spec: lungfirst-ai-flagging-spec.pdf (hard gates, P1–P5, bands, comorbidity modifier).

1) Local Docker (recommended for development)
   From repo root (Ikon.App.MyProject):
     docker compose up -d
   Wait until healthy, then set environment variable (PowerShell example):
     $env:COPD_PG_CONNECTION = "Host=localhost;Port=5432;Username=copd;Password=copd_test;Database=copd_cds"
     ikon app run
   Init scripts 01_schema.sql and 02_seed.sql run automatically on first container start.

2) Manual apply (existing Postgres)
   psql -U your_user -d your_db -f 01_schema.sql
   psql -U your_user -d your_db -f 02_seed.sql
   Set COPD_PG_CONNECTION to your connection string.

3) Ikon-managed Postgres
   Uncomment Databases = ["copd:postgres"] in ikon-config.toml, run `ikon app config`, deploy.
   Unset COPD_PG_CONNECTION so the app uses the allocated connection from the host.

If neither COPD_PG_CONNECTION nor an Ikon "copd" database is available, the app uses bundled JSON demo data.
