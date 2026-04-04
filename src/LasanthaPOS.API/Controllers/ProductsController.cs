using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LasanthaPOS.API.Data;
using LasanthaPOS.API.Models;

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
        var csv = "ItemCode,Name,CategoryId,SupplierId,BuyingPrice,SellingPrice,Quantity,ReorderThreshold,WarrantyMonths\r\n"
                + "ITEM001,Sample Product,1,1,1000.00,1500.00,10,5,12\r\n";
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "inventory_template.csv");
    }

    /// <summary>
    /// Bulk-import products from a CSV file.
    /// Expected columns: ItemCode,Name,CategoryId,SupplierId,BuyingPrice,SellingPrice,Quantity,ReorderThreshold,WarrantyMonths
    /// </summary>
    [HttpPost("import-csv")]
    public async Task<IActionResult> ImportCsv(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var errors = new List<string>();
        var imported = 0;

        using var reader = new System.IO.StreamReader(file.OpenReadStream());
        var header = await reader.ReadLineAsync(); // skip header row
        int row = 1;

        while (!reader.EndOfStream)
        {
            row++;
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = line.Split(',');
            if (cols.Length < 8) { errors.Add($"Row {row}: not enough columns."); continue; }

            if (!decimal.TryParse(cols[4].Trim(), out var buy)) { errors.Add($"Row {row}: invalid BuyingPrice."); continue; }
            if (!decimal.TryParse(cols[5].Trim(), out var sell)) { errors.Add($"Row {row}: invalid SellingPrice."); continue; }
            if (!int.TryParse(cols[6].Trim(), out var qty)) { errors.Add($"Row {row}: invalid Quantity."); continue; }
            if (!int.TryParse(cols[3].Trim(), out var supplierId)) { errors.Add($"Row {row}: invalid SupplierId."); continue; }
            if (!int.TryParse(cols[2].Trim(), out var categoryId)) { errors.Add($"Row {row}: invalid CategoryId."); continue; }
            int? warranty = cols.Length > 8 && int.TryParse(cols[8].Trim(), out var w) ? w : null;
            int reorder = cols.Length > 7 && int.TryParse(cols[7].Trim(), out var r) ? r : 5;

            var itemCode = cols[0].Trim();
            var existing = await _db.Products.FirstOrDefaultAsync(p => p.ItemCode == itemCode);
            if (existing is not null)
            {
                // Update quantity only if already exists
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
                    PurchaseDate = DateTime.UtcNow
                });
            }
            imported++;
        }

        await _db.SaveChangesAsync();
        return Ok(new { imported, errors });
    }
}
