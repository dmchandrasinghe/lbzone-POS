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
}
