using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LasanthaPOS.API.Data;
using LasanthaPOS.API.Models;
using System.Globalization;
using System.Text;

namespace LasanthaPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Select(p => new
            {
                p.Id, p.ItemCode, p.Name,
                Category = p.Category.Name,
                p.CategoryId,
                Supplier = p.Supplier.Name,
                p.SupplierId,
                p.BuyingPrice, p.SellingPrice,
                TotalCost = p.BuyingPrice * p.Quantity,
                ProfitMargin = p.BuyingPrice == 0 ? 0 : Math.Round((p.SellingPrice - p.BuyingPrice) / p.BuyingPrice * 100, 2),
                p.Quantity, p.ReorderThreshold,
                p.PurchaseDate, p.ExpirationDate, p.WarrantyMonths
            })
            .ToListAsync();
        return Ok(products);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock()
    {
        var products = await _db.Products
            .Where(p => p.Quantity <= p.ReorderThreshold)
            .Include(p => p.Category)
            .ToListAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var p = await _db.Products.Include(p => p.Category).Include(p => p.Supplier).FirstOrDefaultAsync(p => p.Id == id);
        return p is null ? NotFound() : Ok(p);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        var products = await _db.Products
            .Where(p => p.Name.Contains(q) || p.ItemCode.Contains(q))
            .Include(p => p.Category)
            .ToListAsync();
        return Ok(products);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Product product)
    {
        if (id != product.Id) return BadRequest();
        _db.Entry(product).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Returns a CSV template with headers for bulk import.
    /// </summary>
    [HttpGet("csv-template")]
    public IActionResult CsvTemplate()
    {
        var csv = new StringBuilder();
        csv.AppendLine("ItemCode,Name,CategoryId,SupplierId,BuyingPrice,SellingPrice,Quantity,ReorderThreshold,WarrantyMonths,PurchaseDate,ExpirationDate");
        // Sample rows — remove these before importing real data
        csv.AppendLine("ITEM001,Samsung 65\" 4K Smart TV,1,1,85000.00,110000.00,5,2,24,2026-01-15,");
        csv.AppendLine("ITEM002,Sony Wireless Headphones,1,2,8500.00,12500.00,20,5,12,2026-02-01,");
        csv.AppendLine("ITEM003,AA Alkaline Batteries (Pack of 4),2,3,250.00,450.00,100,20,,2026-03-10,2027-03-10");
        csv.AppendLine("ITEM004,HDMI Cable 2m,2,1,600.00,950.00,50,10,,2026-01-20,");
        csv.AppendLine("ITEM005,Philips LED Bulb 9W,3,4,180.00,320.00,200,30,,2026-04-01,2028-04-01");
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "inventory_template.csv");
    }

    /// <summary>
    /// Exports all current inventory data as a CSV file.
    /// </summary>
    [HttpGet("export-csv")]
    public async Task<IActionResult> ExportCsv()
    {
        var products = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .OrderBy(p => p.ItemCode)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("ItemCode,Name,Category,Supplier,BuyingPrice,SellingPrice,ProfitMargin%," +
                      "Quantity,ReorderThreshold,TotalCost,PurchaseDate,ExpirationDate,WarrantyMonths");

        foreach (var p in products)
        {
            var exp = p.ExpirationDate.HasValue ? p.ExpirationDate.Value.ToString("yyyy-MM-dd") : "";
            var margin = p.BuyingPrice == 0 ? 0 :
                Math.Round((p.SellingPrice - p.BuyingPrice) / p.BuyingPrice * 100, 2);
            sb.AppendLine(
                $"{CsvEscape(p.ItemCode)},{CsvEscape(p.Name)}," +
                $"{CsvEscape(p.Category?.Name ?? "")},{CsvEscape(p.Supplier?.Name ?? "")}," +
                $"{p.BuyingPrice:F2},{p.SellingPrice:F2},{margin:F2}," +
                $"{p.Quantity},{p.ReorderThreshold},{p.TotalCost:F2}," +
                $"{p.PurchaseDate:yyyy-MM-dd},{exp},{p.WarrantyMonths}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"inventory_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    private static string CsvEscape(string s) =>
        s.Contains(',') || s.Contains('"') || s.Contains('\n')
            ? $"\"{s.Replace("\"", "\"\"")}\""
            : s;

    /// <summary>
    /// Bulk-import products from a CSV file.
    /// Expected columns: ItemCode,Name,CategoryId,SupplierId,BuyingPrice,SellingPrice,Quantity,ReorderThreshold,WarrantyMonths,PurchaseDate,ExpirationDate
    /// </summary>
    [HttpPost("import-csv")]
    public async Task<IActionResult> ImportCsv(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var errors = new List<string>();
        var imported = 0;

        // Pre-load valid IDs so we can validate per row without N+1 queries
        var validCategoryIds = await _db.Categories.Select(c => c.Id).ToHashSetAsync();
        var validSupplierIds  = await _db.Suppliers.Select(s => s.Id).ToHashSetAsync();

        using var reader = new System.IO.StreamReader(file.OpenReadStream());
        await reader.ReadLineAsync(); // skip header row
        int row = 1;

        while (!reader.EndOfStream)
        {
            row++;
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = line.Split(',');
            if (cols.Length < 7) { errors.Add($"Row {row}: not enough columns (expected at least 7)."); continue; }

            if (!int.TryParse(cols[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var categoryId))
                { errors.Add($"Row {row}: invalid CategoryId '{cols[2].Trim()}'."); continue; }
            if (!int.TryParse(cols[3].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var supplierId))
                { errors.Add($"Row {row}: invalid SupplierId '{cols[3].Trim()}'."); continue; }
            if (!decimal.TryParse(cols[4].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var buy))
                { errors.Add($"Row {row}: invalid BuyingPrice '{cols[4].Trim()}'."); continue; }
            if (!decimal.TryParse(cols[5].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var sell))
                { errors.Add($"Row {row}: invalid SellingPrice '{cols[5].Trim()}'."); continue; }
            if (!int.TryParse(cols[6].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var qty))
                { errors.Add($"Row {row}: invalid Quantity '{cols[6].Trim()}'."); continue; }

            if (!validCategoryIds.Contains(categoryId))
                { errors.Add($"Row {row}: CategoryId {categoryId} does not exist. Valid IDs: {string.Join(", ", validCategoryIds.Order())}."); continue; }
            if (!validSupplierIds.Contains(supplierId))
                { errors.Add($"Row {row}: SupplierId {supplierId} does not exist. Valid IDs: {string.Join(", ", validSupplierIds.Order())}."); continue; }

            int reorder = cols.Length > 7 && int.TryParse(cols[7].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : 5;
            int? warranty = cols.Length > 8 && int.TryParse(cols[8].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var w) ? w : null;
            DateTime purchaseDate = cols.Length > 9 && DateTime.TryParse(cols[9].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var pd) ? pd.ToUniversalTime() : DateTime.UtcNow;
            DateTime? expirationDate = cols.Length > 10 && DateTime.TryParse(cols[10].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var ed) ? ed.ToUniversalTime() : null;

            var itemCode = cols[0].Trim();
            var existing = await _db.Products.FirstOrDefaultAsync(p => p.ItemCode == itemCode);
            if (existing is not null)
            {
                existing.Quantity += qty;
                existing.BuyingPrice = buy;
                existing.SellingPrice = sell;
            }
            else
            {
                _db.Products.Add(new Product
                {
                    ItemCode = itemCode,
                    Name = cols[1].Trim(),
                    CategoryId = categoryId,
                    SupplierId = supplierId,
                    BuyingPrice = buy,
                    SellingPrice = sell,
                    Quantity = qty,
                    ReorderThreshold = reorder,
                    WarrantyMonths = warranty,
                    PurchaseDate = purchaseDate,
                    ExpirationDate = expirationDate
                });
            }
            imported++;
        }

        if (imported == 0 && errors.Count > 0)
            return BadRequest(new { imported, errors });

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { imported = 0, errors = new[] { $"Database save failed: {ex.InnerException?.Message ?? ex.Message}" } });
        }

        return Ok(new { imported, errors });
    }
}
