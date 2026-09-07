# Development status

## Confirmed requirements
- Bengali UI; Gregorian dates.
- Configurable membership types and amounts.
- Configurable service charges and temple/priest/staff allocations.
- Enter each transaction once.
- Expense categories determine whether admin approval is required.
- General devotee records; no public portal.

## First increment
- Admin settings: `/Settings` (membership, service, expense categories).
- Service shares currently configured as fixed monetary amounts, validated against the total.
- Expense submission: `/Expenses/Create`; list and admin review: `/Expenses`.
- Pending/rejected expenses excluded from the displayed approved total.
- Approval requirement captured on submission; later category changes do not change existing expenses.
- Anti-forgery validation, role restrictions, row-version concurrency and duplicate submission key.
- Database migration included; application startup already applies migrations in the existing project.

## Validation
Run `dotnet build DoyamoyeeMondir.slnx` and `dotnet run --project tests/DoyamoyeeMondir.Checks`.
The checks cover approval transitions and monetary validation; they do not replace SQL Server integration or browser acceptance tests.

First increment verification: application build passed with zero warnings/errors; all 12 workflow/validation checks passed; SQL migration script generation passed. Migration has not been applied to SQL Server and browser acceptance testing is still outstanding.

## Second increment
- `/People`: Bengali devotee/contact records, editing, search, paging and collection history.
- `/Memberships`: enrollment, agreed amount/type snapshot, optional date range, partial collection and outstanding dues.
- Enrollment is one configured charge for the entered term. Automatic recurring billing is not assumed; create a new enrollment for a new term.
- `/Income`: donations/general income, membership installments and service collections in one register, with date filtering and paging.
- `/Income/Services`: configured services and fees. Service collections snapshot the fee and all three monetary shares.
- `/Income/Receipt/{id}`: Bengali printable receipt with stable database-generated number and historical payer/category/account names.
- Admin settings now include income categories and cash/bank accounts, with opening balance/date.
- `/Reports`: date-range income and approved/pending expense summaries, including priest/staff allocations within gross collections.
- Dashboard reads database totals instead of sample figures.
- Membership balance and receipt save atomically; row versions reject competing collections and a unique submission key prevents duplicate receipts.
- Actual MVC form checks exposed and fixed the previously required empty row-version field on new settings/person forms.

### Second increment validation
- Application build: zero warnings/errors.
- 23 domain/validation checks passed.
- SQL integration checks passed against a fresh temporary database: migrations, concurrent collection rollback, duplicate POST, outdated service price and account opening date.
- 22 live HTTP checks passed: authentication, rendered Bengali pages, settings creation, devotee creation, enrollment, collection, receipt and report.
- Visual browser inspection could not run because the browser automation runtime failed to start. HTTP checks do not verify visual layout or printed page layout.
- Migrations were applied to the separate `DoyamoyeeMondirPreview` database on the local default SQL instance. The connection configured in appsettings.json was not changed.
- Local preview: `http://127.0.0.1:5209`; contains clearly named test records.

Run SQL checks with `dotnet run --project tests/DoyamoyeeMondir.Checks -- --sql`. `TEMPLE_TEST_SQL` may override the server connection; the test always substitutes its own uniquely named database and removes only that database.
Run live HTTP checks with `TEMPLE_TEST_PASSWORD` set, using `python tests/http_smoke.py` against the preview. These checks intentionally leave sample records in that preview.

## Remaining increments
- Expense settlement, account transfers, reconciled balances and correction/reversal workflows.
- Inventory, assets/ornaments, committee and document registers.
- Prasad sales beyond generic income entry; employee/salary workflows.
- Membership billing recurrence once the client defines its schedule and renewal rules.
- Bengali Identity screens, user/role administration, visual/print acceptance, operational deployment and backup validation.

Expense approval currently records authorization of the expense, not bank/cash settlement. No payment ledger or account balance is implemented yet.
The existing startup seeder contains a fixed administrator password; replace it with securely configured bootstrap credentials before deployment.
