using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LasanthaPOS.API.Data;
using LasanthaPOS.API.Models;

namespace LasanthaPOS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;
    public CustomersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _db.Customers.ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var c = await _db.Customers.FindAsync(id);
        return c is null ? NotFound() : Ok(c);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        var customers = await _db.Customers
            .Where(c => c.Name.Contains(q) || c.Phone.Contains(q) || c.LoyaltyCardId.Contains(q))
            .ToListAsync();
        return Ok(customers);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.LoyaltyCardId))
            customer.LoyaltyCardId = $"LC-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Customer customer)
    {
        if (id != customer.Id) return BadRequest();
        _db.Entry(customer).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id}/purchases")]
    public async Task<IActionResult> GetPurchases(int id)
    {
        var sales = await _db.Sales
            .Where(s => s.CustomerId == id)
            .Include(s => s.Items)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();
        return Ok(sales);
    }
}

[ApiController]
[Route("api/[controller]")]
public class WarrantiesController : ControllerBase
{
    private readonly AppDbContext _db;
    public WarrantiesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Warranties
            .Include(w => w.Customer)
            .Include(w => w.Product)
            .Select(w => new
            {
                w.Id,
                ProductName = w.Product.Name,
                CustomerName = w.Customer.Name,
                w.StartDate,
                w.EndDate,
                w.Status,
                w.ClaimNotes
            })
            .ToListAsync());

    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> ByCustomer(int customerId) =>
        Ok(await _db.Warranties
            .Where(w => w.CustomerId == customerId)
            .Include(w => w.Product)
            .Select(w => new
            {
                w.Id,
                ProductName = w.Product.Name,
                CustomerName = w.Customer.Name,
                w.StartDate,
                w.EndDate,
                w.Status,
                w.ClaimNotes
            })
            .ToListAsync());

    [HttpGet("product/{productId}")]
    public async Task<IActionResult> ByProduct(int productId) =>
        Ok(await _db.Warranties.Where(w => w.ProductId == productId).Include(w => w.Customer).ToListAsync());

    [HttpPut("{id}/claim")]
    public async Task<IActionResult> Claim(int id, [FromBody] string notes)
    {
        var w = await _db.Warranties.FindAsync(id);
        if (w is null) return NotFound();
        w.Status = "Claimed";
        w.ClaimNotes = notes;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    public CategoriesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _db.Categories.ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Category category)
    {
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return Ok(category);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _db.Categories.FindAsync(id);
        if (cat is null) return NotFound();
        _db.Categories.Remove(cat);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly AppDbContext _db;
    public SuppliersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _db.Suppliers.ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Supplier supplier)
    {
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();
        return Ok(supplier);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var sup = await _db.Suppliers.FindAsync(id);
        if (sup is null) return NotFound();
        _db.Suppliers.Remove(sup);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
