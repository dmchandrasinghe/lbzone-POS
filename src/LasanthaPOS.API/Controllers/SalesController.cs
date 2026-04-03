using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LasanthaPOS.API.Data;
using LasanthaPOS.API.Models;

namespace LasanthaPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly AppDbContext _db;
    public SalesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var query = _db.Sales.Include(s => s.Items).Include(s => s.Customer).AsQueryable();
        if (from.HasValue) query = query.Where(s => s.SaleDate >= from.Value);
        if (to.HasValue) query = query.Where(s => s.SaleDate <= to.Value);
        return Ok(await query.OrderByDescending(s => s.SaleDate).ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var sale = await _db.Sales
            .Include(s => s.Items).ThenInclude(i => i.Product)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == id);
        return sale is null ? NotFound() : Ok(sale);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSale([FromBody] Sale sale)
    {
        // Generate receipt number
        sale.ReceiptNumber = $"RCP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
        sale.SaleDate = DateTime.UtcNow;

        // Deduct stock and snapshot names
        foreach (var item in sale.Items)
        {
            var product = await _db.Products.FindAsync(item.ProductId);
            if (product is null) return BadRequest($"Product {item.ProductId} not found.");
            if (product.Quantity < item.Quantity)
                return BadRequest($"Insufficient stock for {product.Name}. Available: {product.Quantity}");

            product.Quantity -= item.Quantity;
            item.ItemCode = product.ItemCode;
            item.ProductName = product.Name;
            item.LineTotal = (item.UnitPrice * item.Quantity) - item.DiscountAmount;

            // Create warranty if product has warranty months
            if (product.WarrantyMonths.HasValue && product.WarrantyMonths > 0 && sale.CustomerId.HasValue)
            {
                _db.Warranties.Add(new Warranty
                {
                    SaleItemId = 0, // will be set after save
                    CustomerId = sale.CustomerId.Value,
                    ProductId = item.ProductId,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(product.WarrantyMonths.Value),
                    Terms = $"{product.WarrantyMonths} month manufacturer warranty",
                    Status = "Active"
                });
            }
        }

        // Loyalty points: 1 point per 100 units of currency spent
        if (sale.CustomerId.HasValue)
        {
            var customer = await _db.Customers.FindAsync(sale.CustomerId.Value);
            if (customer is not null)
            {
                var earned = (int)(sale.Total / 100);
                customer.LoyaltyPoints += earned - sale.PointsRedeemed;
                sale.PointsEarned = earned;
            }
        }

        _db.Sales.Add(sale);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale);
    }

    [HttpGet("daily-summary")]
    public async Task<IActionResult> DailySummary([FromQuery] DateTime? date)
    {
        var day = date ?? DateTime.UtcNow.Date;
        var sales = await _db.Sales
            .Where(s => s.SaleDate.Date == day.Date)
            .ToListAsync();
        return Ok(new
        {
            Date = day,
            TotalSales = sales.Count,
            TotalRevenue = sales.Sum(s => s.Total),
            TotalDiscount = sales.Sum(s => s.DiscountAmount),
            CashSales = sales.Count(s => s.PaymentMethod == "Cash"),
            CardSales = sales.Count(s => s.PaymentMethod == "Card")
        });
    }
}
