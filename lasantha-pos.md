# POS System with Inventory Management — Electronics Store

## Overview

A Point of Sale (POS) system integrated with an Inventory Management System for an Electronics Store. Built with a C# frontend, C# backend, and PostgreSQL database.

---

## Technology Stack

| Layer     | Technology                              | Deployment            |
|-----------|-----------------------------------------|-----------------------|
| Frontend  | C# WPF (.NET 8) Windows Desktop App    | Windows shortcut (.lnk) |
| Backend   | C# ASP.NET Core Web API (.NET 8)       | Docker container      |
| Database  | PostgreSQL 16                           | Docker container      |
| Container | Docker + Docker Compose                 | docker-compose.yml    |

### Architecture

```
[WPF Desktop App]  <--HTTP/REST-->  [.NET Web API : Docker]  <-->  [PostgreSQL : Docker]
     (Windows)                          (port 5000)                    (port 5432)
```

### Project Structure

```
Lasantha-POS/
├── docker-compose.yml          # PostgreSQL + API containers
├── LasanthaPOS.sln             # Visual Studio solution
├── src/
│   ├── LasanthaPOS.API/        # ASP.NET Core Web API (Dockerized)
│   │   ├── Dockerfile
│   │   ├── Controllers/
│   │   ├── Models/
│   │   ├── Data/               # EF Core DbContext + Migrations
│   │   └── Services/
│   └── LasanthaPOS.Desktop/   # WPF Windows Desktop Application
│       ├── Views/
│       ├── ViewModels/
│       └── Services/           # HTTP client wrappers
└── CreateShortcut.ps1          # Creates desktop shortcut
```

---

## Modules

### 1. Inventory Management

Manage stock of electronic goods with full traceability.

**Fields per inventory item:**

| Field            | Description                              |
|------------------|------------------------------------------|
| Item Code        | Unique identifier for the product        |
| Item Name        | Display name of the product              |
| Category         | e.g., Mobile, Laptop, Accessories        |
| Supplier         | Supplier name / supplier ID              |
| Buying Price     | Cost price per unit                      |
| Selling Price    | Retail price per unit                    |
| Profit Margin    | Calculated: `(Selling - Buying) / Buying * 100` |
| Quantity         | Current stock quantity                   |
| Total Cost       | `Buying Price × Quantity`                |
| Purchase Date    | Date of stock receipt                    |
| Expiration Date  | Applicable for items with shelf life     |

**Features:**
- Add, edit, delete inventory items (set buying date automatically to today)
- allow add products to invotory from CSV  as well also show export template of inventory
- Add Product categories Register Suppliers
- set expiration period  motnth and year instead of expire date
- Low stock alerts / reorder threshold
- Stock level adjustment (manual correction)
- Supplier management (name, contact, lead time)
- Purchase history per item
- Export inventory report (PDF / CSV)

---

### 2. Point of Sale (POS)

Fast checkout interface for cashiers.

**Features:**
- Search product by item code, name, or barcode scan
- Add items to cart / modify quantities
- Apply item-level or bill-level discounts (% or fixed amount)
- Calculate totals, tax, and change due
- Multiple payment methods: Cash, Card, Credit
- Print / email bill / receipt
- Void / return transactions
- Daily sales summary

---

### 3. Billing & Receipts

- Itemised bill with item name, quantity, unit price, discount, tax, total
- Store branding (logo, address, contact) on printed bill
- Receipt number / transaction ID
- Reprint last bill

---

### 4. Discount Management

- Percentage-based and fixed-amount discounts
- Item-level discount
- Bill-level discount
- Role-based discount authority (e.g., Manager can apply > 10% discount)
- Discount reason / authorization tracking

---

### 5. Loyalty Program

- Register customers with name, contact, and loyalty card / ID
- Earn points per purchase (configurable rate)
- Redeem points as discount on future purchases
- View loyalty point balance and transaction history
- Tier levels (e.g., Silver, Gold, Platinum) with tier-based benefits

---

### 6. Warranty Management

- Assign warranty period to products at point of sale
- Record warranty start date, end date, and terms
- Link warranty to customer and transaction
- Warranty lookup by item code or customer
- Track warranty claims and status (Open, In Repair, Resolved, Replaced)
- Warranty expiry notifications

---

## Non-Functional Requirements

- **Security:** Role-based access control (Admin, Manager, Cashier)
- **Performance:** Transaction processing under 2 seconds
- **Backup:** Automated daily database backup
- **Audit Log:** Track all create / update / delete actions with user and timestamp
- **Reporting:** Sales reports, inventory reports, profit/loss summary
- **Scalability:** Support multiple terminals / branches



