# LasanthaPOS — Project Status for AI Agents

> Last updated: 2026-04-05
> Status legend: ✅ Done · 🔶 Partial · ❌ Not started

---

## Tech Stack

| Layer    | Technology                                      | Details                          |
|----------|-------------------------------------------------|----------------------------------|
| Frontend | C# WPF (.NET 10) — Windows Desktop App         | `src/LasanthaPOS.Desktop/`       |
| Backend  | C# ASP.NET Core Web API (.NET 10)              | `src/LasanthaPOS.API/`           |
| Database | PostgreSQL 16                                   | Docker container, port 5432      |
| Runtime  | Docker Compose                                  | `docker-compose.yml`             |

API runs on **port 5100** (mapped from container port 8080).  
Desktop connects to `http://localhost:5100`.

---

## Architecture

```
[WPF Desktop App] <--HTTP/REST--> [ASP.NET Core API : Docker :5100] <--> [PostgreSQL :5432 : Docker]
```

---

## Module Implementation Status

### 1. Inventory Management
| Feature                                          | Status |
|--------------------------------------------------|--------|
| Add / edit / delete products                     | ✅     |
| Auto-set purchase date to today on add           | ✅     |
| Search products by name / item code              | ✅     |
| Low-stock alerts / reorder threshold filter      | ✅     |
| Category management (add / delete)               | ✅     |
| Supplier management (name, phone, email)         | ✅     |
| CSV import of products                           | ✅     |
| Export CSV template for import                   | ✅     |
| Export inventory report (PDF / CSV)              | ✅     |
| Expiration: month+year input instead of date     | ❌     |
| Purchase history per item                        | ❌     |
| Stock adjustment (manual correction)             | ❌     |

### 2. Point of Sale (POS)
| Feature                                          | Status |
|--------------------------------------------------|--------|
| Product search (name / item code)                | ✅     |
| Add items to cart, modify quantities             | ✅     |
| Item-level discount (fixed amount)               | ✅     |
| Bill-level discount                              | ✅     |
| Percentage-based discounts                       | ❌     |
| Calculate totals, tax, and change due            | ✅     |
| Payment methods: Cash, Card, Credit              | ✅     |
| Print receipt (WPF print dialog)                 | ✅     |
| Reprint last bill                                | ✅     |
| Email bill / receipt                             | ❌     |
| Void / return transactions                       | ❌     |
| Daily sales summary on POS screen               | ❌     |
| Barcode scan input                               | ❌     |

### 3. Billing & Receipts
| Feature                                          | Status |
|--------------------------------------------------|--------|
| Itemised receipt (name, qty, price, discount)    | ✅     |
| Transaction / receipt number (RCP-YYYYMMDD-xxxx) | ✅     |
| Reprint last bill                                | ✅     |
| Store branding on printed bill                   | 🔶 basic |
| Receipt via email                                | ❌     |

### 4. Discount Management
| Feature                                          | Status |
|--------------------------------------------------|--------|
| Fixed-amount item-level discount in POS          | ✅     |
| Fixed-amount bill-level discount in POS          | ✅     |
| Percentage-based discounts                       | ❌     |
| Role-based discount authority                    | ❌     |
| Discount reason / authorization tracking         | ❌     |
| Dedicated discount configuration UI             | ❌     |

### 5. Loyalty Program
| Feature                                          | Status |
|--------------------------------------------------|--------|
| Register customers with loyalty card ID          | ✅     |
| Earn points per purchase (1 per 100 currency)    | ✅     |
| Redeem points as discount                        | ✅     |
| View loyalty balance (customer list)             | ✅     |
| Loyalty transaction history                      | ❌     |
| Tier levels (Silver / Gold / Platinum)           | ❌     |
| Tier-based configurable benefits                 | ❌     |
| Configurable points rate                         | ❌     |

### 6. Warranty Management
| Feature                                          | Status |
|--------------------------------------------------|--------|
| Auto-create warranty on sale (if product has it) | ✅     |
| Warranty lookup by customer                      | ✅     |
| Warranty lookup by product                       | ✅     |
| View all warranties in desktop UI                | ✅     |
| File a claim (status → Claimed + notes)          | ✅     |
| Claim status tracking (Open/In Repair/Resolved)  | 🔶 basic |
| Warranty expiry notifications                    | ❌     |
| Warranty expiry report                           | ❌     |

### 7. Reporting
| Feature                                          | Status |
|--------------------------------------------------|--------|
| Daily sales summary (count, revenue, discounts)  | ✅     |
| Cash vs card breakdown in daily report           | ✅     |
| Export report as PDF                             | ❌     |
| Export report as CSV                             | ❌     |
| Inventory / stock report                         | ❌     |
| Profit / loss summary report                     | ❌     |
| Sales report by date range                       | ❌     |

### 8. User Management & Security
| Feature                                          | Status |
|--------------------------------------------------|--------|
| User model with Role (Admin, Manager, Cashier)   | ✅     |
| Login with BCrypt password verification          | ✅     |
| Role stored in session after login               | ✅     |
| JWT / token-based authentication                 | ❌     |
| Role enforcement middleware on API endpoints     | ❌     |
| User management UI (add/edit/deactivate users)   | ❌     |

### 9. Non-Functional / Infrastructure
| Feature                                          | Status |
|--------------------------------------------------|--------|
| Dockerised API + PostgreSQL via Compose          | ✅     |
| Desktop shortcut creation script                 | ✅     |
| EF Core migrations (auto-applied on API startup) | ✅     |
| Versioned SQL migration runner (`db/migrate.ps1`)       | ✅     |
| SQL migration files (`db/migrations/V###__*.sql`)       | ✅     |
| deploy.bat — auto-install deps, docker, DB migrate, build, launch | ✅ |
| build-frontend.bat — desktop-only build (PS 5/6/7 safe) | ✅ |
| Audit log (user + timestamp on CUD operations)  | ❌     |
| Automated daily database backup                  | ❌     |
| Multi-terminal / multi-branch support            | ❌     |
| Performance: transaction < 2 seconds             | 🔶 untested |

---

## Key File Map

| File / Folder                                                   | Purpose                                       |
|-----------------------------------------------------------------|-----------------------------------------------|
| `src/LasanthaPOS.API/Models/Models.cs`                         | All domain models (Product, Sale, Customer…)  |
| `src/LasanthaPOS.API/Data/AppDbContext.cs`                     | EF Core DbContext + DbSets                    |
| `src/LasanthaPOS.API/Controllers/ProductsController.cs`        | Inventory CRUD + search + low-stock           |
| `src/LasanthaPOS.API/Controllers/SalesController.cs`           | POS transactions, daily summary               |
| `src/LasanthaPOS.API/Controllers/AuthController.cs`            | Login endpoint                                |
| `src/LasanthaPOS.API/Controllers/CustomerAndOtherControllers.cs` | Customers, Warranties, Categories, Suppliers |
| `src/LasanthaPOS.Desktop/Services/ApiService.cs`               | HTTP client wrapper for all API calls         |
| `src/LasanthaPOS.Desktop/Views/InventoryPage.xaml(.cs)`        | Inventory management UI                       |
| `src/LasanthaPOS.Desktop/Views/PosPage.xaml(.cs)`              | POS / checkout UI                             |
| `src/LasanthaPOS.Desktop/Views/CustomersPage.xaml(.cs)`        | Customer & loyalty UI                         |
| `src/LasanthaPOS.Desktop/Views/WarrantyPage.xaml(.cs)`         | Warranty tracking UI                          |
| `src/LasanthaPOS.Desktop/Views/ReportPage.xaml(.cs)`           | Daily report UI                               |
| `src/LasanthaPOS.Desktop/Views/CategoriesSuppliersDialog.xaml` | Category & supplier management dialog         |
| `docker-compose.yml`                                           | PostgreSQL + API container definitions        |
| `db/migrate.ps1`                                               | Versioned SQL migration runner (PS 5.1/6/7)   |
| `db/migrations/V001__initial_schema.sql`                       | Initial full schema — idempotent, all 8 tables + indexes |
| `deploy.bat`                                                   | Full deploy: deps → docker → DB migrate → build → launch |
| `build-frontend.bat`                                           | Desktop-only build (no Docker/backend steps)  |
| `lasantha-pos.md`                                              | Full requirements document                    |

---

## Domain Model Summary

```
User            — Id, Username, PasswordHash, FullName, Role, IsActive
Category        — Id, Name
Supplier        — Id, Name, ContactPhone, Email
Product         — Id, ItemCode, Name, CategoryId, SupplierId, BuyingPrice, SellingPrice,
                  Quantity, ReorderThreshold, PurchaseDate, ExpirationDate, WarrantyMonths
Customer        — Id, Name, Phone, Email, LoyaltyCardId, LoyaltyPoints, RegisteredAt
Sale            — Id, ReceiptNumber, CustomerId, SaleDate, SubTotal, DiscountAmount,
                  TaxAmount, Total, PaymentMethod, AmountPaid, Change, PointsEarned,
                  PointsRedeemed, CreatedBy
SaleItem        — Id, SaleId, ProductId, ItemCode, ProductName, Quantity, UnitPrice,
                  DiscountAmount, LineTotal
Warranty        — Id, SaleItemId, CustomerId, ProductId, StartDate, EndDate, Terms,
                  Status, ClaimNotes
```

---

## API Endpoint Summary

| Method | Route                            | Controller       |
|--------|----------------------------------|------------------|
| POST   | /api/auth/login                  | Auth             |
| GET    | /api/products                    | Products         |
| GET    | /api/products/{id}               | Products         |
| GET    | /api/products/search?q=          | Products         |
| GET    | /api/products/low-stock          | Products         |
| POST   | /api/products                    | Products         |
| PUT    | /api/products/{id}               | Products         |
| DELETE | /api/products/{id}               | Products         |
| GET    | /api/sales                       | Sales            |
| GET    | /api/sales/{id}                  | Sales            |
| POST   | /api/sales                       | Sales            |
| GET    | /api/sales/daily-summary?date=   | Sales            |
| GET    | /api/customers                   | Customers        |
| GET    | /api/customers/{id}              | Customers        |
| GET    | /api/customers/search?q=         | Customers        |
| POST   | /api/customers                   | Customers        |
| PUT    | /api/customers/{id}              | Customers        |
| GET    | /api/customers/{id}/purchases    | Customers        |
| GET    | /api/warranties                  | Warranties       |
| GET    | /api/warranties/customer/{id}    | Warranties       |
| GET    | /api/warranties/product/{id}     | Warranties       |
| PUT    | /api/warranties/{id}/claim       | Warranties       |
| GET    | /api/categories                  | Categories       |
| POST   | /api/categories                  | Categories       |
| DELETE | /api/categories/{id}             | Categories       |
| GET    | /api/suppliers                   | Suppliers        |
| POST   | /api/suppliers                   | Suppliers        |
| DELETE | /api/suppliers/{id}              | Suppliers        |

---

## Database Migration Architecture

Two complementary migration mechanisms coexist — both are applied on every deploy:

| Mechanism | Runner | Tracking table | When it runs |
|---|---|---|---|
| **SQL scripts** (`V###__*.sql`) | `db/migrate.ps1` (PowerShell) | `db_migrations` | deploy.bat STEP 4a — after PostgreSQL is ready, before API starts |
| **EF Core C# migrations** | `db.Database.Migrate()` in `Program.cs` | `__EFMigrationsHistory` | API container startup |

`V001__initial_schema.sql` also inserts its ID into `__EFMigrationsHistory` so EF Core skips it, preventing double-application.

### Migration file naming convention
```
db/migrations/
  V001__initial_schema.sql          ← applied → skipped on re-run
  V002__add_discount_table.sql      ← pending → applied in order
  V003__seed_reference_data.sql     ← pending → applied in order
```

### How the runner works (`db/migrate.ps1`)
- Reads `V*.sql` files in lexicographic order
- Checks `db_migrations` table for already-applied versions — skips them
- Wraps each new migration in `BEGIN … COMMIT` with `\set ON_ERROR_STOP 1`
- On failure: transaction rolls back, script exits 1, deploy.bat logs error and continues
- Uses `docker cp` + `docker exec psql` — compatible with PS 5.1 / 6 / 7

### Adding a new migration
1. Create `db/migrations/V002__your_description.sql`
2. Write idempotent SQL (`IF NOT EXISTS`, `ON CONFLICT DO NOTHING`)
3. Run `deploy.bat` — only the new script will execute

---

## Known Gaps / Priority Next Steps

1. **API authentication** — No JWT middleware; all endpoints are currently unprotected. Add `[Authorize]` + JWT bearer in `Program.cs`.
2. **Percentage discounts** — POS only supports fixed-amount discounts. Add `%` toggle to POS and models.
3. **Expiration input** — Requirements say month+year, but model stores `ExpirationDate` (full DateTime). Adjust `ProductDialog` to show month/year pickers and calculate the date internally.
4. **Audit logging** — No audit trail. Add a `AuditLog` table + EF Core interceptor or middleware.
5. **Report export** — Daily report has no PDF/CSV export. Consider `QuestPDF` (PDF) or `CsvHelper` (CSV).
6. **Loyalty tiers** — No tier logic. Would need a `LoyaltyTier` config table and threshold rules.
7. **User management UI** — Admin needs a screen to create/edit/deactivate users.
8. **Void/return transactions** — No refund/void flow exists in API or UI.
