namespace LasanthaPOS.Desktop.Models;

public class Product
{
    public int Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Supplier { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public decimal BuyingPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal TotalCost { get; set; }
    public decimal ProfitMargin { get; set; }
    public int Quantity { get; set; }
    public int ReorderThreshold { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int? WarrantyMonths { get; set; }
    public override string ToString() => $"[{ItemCode}] {Name} - ${SellingPrice:F2} (Qty: {Quantity})";
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string LoyaltyCardId { get; set; } = string.Empty;
    public int LoyaltyPoints { get; set; }
    public override string ToString() => $"{Name} ({Phone}) — Points: {LoyaltyPoints}";
}

public class CartItem
{
    public int ProductId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal => (UnitPrice * Quantity) - DiscountAmount;
    public DateTime PurchaseDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public override string ToString() => $"{ItemCode} | {ProductName} | Qty:{Quantity} | ${UnitPrice:F2} | Disc:${DiscountAmount:F2} | Total:${LineTotal:F2}";
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public override string ToString() => Name;
}

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public override string ToString() => Name;
}

public class Warranty
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ClaimNotes { get; set; } = string.Empty;
}

public class DailySummary
{
    public DateTime Date { get; set; }
    public int TotalSales { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscount { get; set; }
    public int CashSales { get; set; }
    public int CardSales { get; set; }
}
