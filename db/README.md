# Database

The Invoice Portal Admin demo database, as version-controlled SQL. `db-init` applies these files
in filename order when it finds no bacpac mounted, which is the default. Nothing here touches
Azure or any Monster Energy system.

| File | What it is |
|------|------------|
| `001_schema.sql` | Table definitions, defaults, indexes and foreign keys |
| `002_seed_demo_data.sql` | Synthetic demo rows |

## What's in the schema

The nine tables the admin UI scaffolds, plus the seven more they reach through a foreign key -
sixteen in all. The production database has around eighty; the rest are not used by this POC and
are deliberately absent.

```
Bottlers  BrandSegmentCategories  Companies  Countries  Currencies  Invoices  Payers  Programs  SalesCenters
AspNetUsers  CancellationCategories  CommercialManagers  FundingElements  ProgramTypes  SamplesPurposes  Vendors
```

`001_schema.sql` was generated from the local SQL Server 2022 Express container's catalog views
and then committed. It is a faithful copy of those sixteen tables: 175 columns, 111 indexes,
24 foreign keys and 60 default constraints, verified to match object-for-object.

## What's in the demo data

Entirely fictional - invented for this repo, not sampled or anonymised out of anything real.
Every company, distributor, person and email address below is made up, and `example.com`
addresses are used throughout.

| | | | |
|---|---:|---|---:|
| Companies | 2 | Bottlers | 40 |
| Countries | 2 | Payers | 90 |
| Currencies | 2 | Sales centers | 200 |
| Brand segment categories | 3 | Programs | 400 |
| Users | 24 | Invoices | 4,000 |

The volumes are chosen to exercise the UI: server-side paging, sorting and filtering on the
invoice grid, populated dropdowns on every lookup screen, and enough spread for the AI query
pages to return interesting aggregates. Invoices cover 17 of the 21 `InvoiceStatus` values,
100 are soft-deleted so the "Show deleted" toggle has something to show, and 195 are `Samples`
rather than `Promotional`.

Rows are internally consistent: an invoice's sales center belongs to its payer, that payer to its
bottler, and its program to that same bottler.

Dates are relative to the moment the script runs, so a freshly built database always looks
current. Everything else is deterministic - ids, names and amounts come from an arithmetic hash
of the row number, so two runs produce the same database.

## Applying by hand

```bash
sqlcmd -S localhost,1433 -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "CREATE DATABASE InvoicePortal"
sqlcmd -S localhost,1433 -U sa -P "$MSSQL_SA_PASSWORD" -C -d InvoicePortal -i db/001_schema.sql
sqlcmd -S localhost,1433 -U sa -P "$MSSQL_SA_PASSWORD" -C -d InvoicePortal -i db/002_seed_demo_data.sql
```

`002_seed_demo_data.sql` exits without doing anything if `Bottlers` already has rows, so it is
safe to re-run.

## Changing the schema

These files are the source of truth for the demo database. After editing them, rebuild from
scratch and re-scaffold the EF model so the two stay in step:

```bash
docker compose down -v && docker compose up --build
```

The EF entities under `InvoicePortal.Admin/Data/` are scaffold output and are never hand-edited;
see the repository README for the scaffold command.

## Working from a real bacpac instead

If you have an actual export, `docker-compose.bacpac.yml` switches `db-init` over to it:

```bash
docker compose -f docker-compose.yml -f docker-compose.bacpac.yml up --build
```

Bacpacs are git-ignored (`*.bacpac`) and must stay that way - they contain production data and
this repository is public.
