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

## Third increment
- `/Accounts`: balances by date from opening balances, income, actual expense payments and transfers. These are recorded balances, not bank-reconciled balances. Negative recorded balances are displayed, not silently blocked, so missing entries can be investigated.
- `/Accounts/Pay/{expenseId}`: partial/full payment of approved expenses, protected against overpayment and duplicate submission; `/Accounts/Voucher/{id}` provides a printable payment voucher.
- `/Accounts/Transfer`: records completed transfers between different accounts without creating income or expense. Transfer history and payment history are paginated.
- Account postings participate in row-version concurrency; opening terms cannot be changed after income, payments or transfers exist.
- `/Inventory`: products, units, reorder thresholds, stock receipts/issues and movement history. Unit changes are blocked after movements exist. Stock cannot become negative; backdating before the latest stock movement is blocked. Corrections can be entered as a compensating movement with a reason.
- `/Assets`: asset/ornament register with unique code, description, material, weight, estimated value, donor, location, custodian and active status.
- `/Committees`: committee terms, editing and person/position assignments. Duplicate assignment of the same person to a committee is blocked.
- `/Documents`: searchable document register, upload and authenticated attachment download. PDF/PNG/JPEG signatures and 10 MB size limit are checked. Files are stored in SQL Server, not a public web directory; signature validation is not malware scanning.
- InventoryManager and DocumentManager can reach their modules from the dashboard without receiving financial dashboard data.
- Bengali navigation and forms are included; migration `TempleOperations` has been applied to the separate preview database.

### Third increment validation
- Build passed with zero warnings/errors.
- 36 domain/validation checks and 17 SQL integration assertions passed. Database checks include duplicate payments/transfers, concurrent payment/stock rollback and account-balance arithmetic.
- 51 live HTTP checks passed across the existing and new workflows, including overpayment/over-issue rejection, document download protection and antiforgery enforcement.
- Desktop browser screenshots verified the account balances, inventory register and stock-entry form. Printed-page and mobile layout acceptance are still pending.
- Run `python tests/http_operations_smoke.py` with `TEMPLE_TEST_PASSWORD` configured to exercise the full preview workflow; it retains clearly named test records in the preview database.

## Remaining increments
- Bank reconciliation and financial correction/reversal workflows.
- Prasad sales beyond generic income entry.
- Membership billing recurrence once the client defines its schedule and renewal rules.
- Bengali Identity screens, user/role administration, visual/print acceptance, operational deployment and backup validation.

Expense approval authorizes the expense; the new payment workflow separately records settlement and affects account balances.
The existing startup seeder contains a fixed administrator password; replace it with securely configured bootstrap credentials before deployment.

## Employee and payroll increment
- `/Employees`: Bengali employee register; administrators can create/edit name, position, contact, joining date, active status and monthly salary.
- `/Employees/Generate/{id}`: one payroll per employee/calendar month, with salary snapshot, allowance and deduction. Partial-month adjustments are entered explicitly as deductions/allowances.
- Payroll creates one linked expense atomically. The selected expense category determines approval; the existing expense/payment workflow handles settlement. Recording payroll does not itself move cash.
- `/Employees/Payroll`: monthly salary history, approved/pending/paid status, paid amount and payment links. Later salary edits preserve historical figures.
- Unique employee/month and expense indexes prevent duplicate payroll. Employee row-version checks reject stale salary forms and competing updates. Joining date is fixed after payroll exists.
- Migration `PayrollWorkflow` preserves the earlier `cashmodule` migration and replaces its ordinary binary concurrency columns with SQL Server rowversion columns for employees/payroll.
- Validation: build passed (four existing lowercase migration-class warnings); 36 domain checks and 26 SQL integration assertions passed, including nine payroll assertions. Five live payroll form checks passed against `DoyamoyeeMondirPreview`.
- The general HTTP suite passed once during this increment; a repeat exposed its existing first-person selection assumption when preview records accumulate. The payroll HTTP test runs independently and passed. Visual/mobile acceptance remains outstanding.
- Payroll migration applied to the separate preview database. Preview process stopped after testing to avoid locking build DLLs.
- Run `python tests/http_payroll_smoke.py` with `TEMPLE_TEST_PASSWORD` set while preview is running. This leaves named test records in preview.
- Rejected/cancelled payroll correction is still part of the pending financial correction workflow; do not create a second general expense to bypass the monthly payroll restriction.
