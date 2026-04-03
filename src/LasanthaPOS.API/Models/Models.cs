namespace LasanthaPOS.API.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Supplier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product
{
    public int Id { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public decimal BuyingPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int Quantity { get; set; }
    public int ReorderThreshold { get; set; } = 5;
    public DateTime PurchaseDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int? WarrantyMonths { get; set; }

    public decimal TotalCost => BuyingPrice * Quantity;
    public decimal ProfitMargin => BuyingPrice == 0 ? 0 : Math.Round((SellingPrice - BuyingPrice) / BuyingPrice * 100, 2);
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string LoyaltyCardId { get; set; } = string.Empty;
    public int LoyaltyPoints { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<Warranty> Warranties { get; set; } = new List<Warranty>();
}

public class Sale
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string PaymentMethod { get; set; } = "Cash"; // Cash, Card, Credit
    public decimal AmountPaid { get; set; }
    public decimal Change { get; set; }
    public int PointsEarned { get; set; }
    public int PointsRedeemed { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}

public class SaleItem
{
    public int Id { get; set; }
    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string ItemCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public class Warranty
{
    public int Id { get; set; }
    public int SaleItemId { get; set; }
    public SaleItem SaleItem { get; set; } = null!;
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Terms { get; set; } = string.Empty;
    public string Status { get; set; } = "Active"; // Active, Claimed, Expired
    public string ClaimNotes { get; set; } = string.Empty;
}

public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Cashier"; // Admin, Manager, Cashier
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
