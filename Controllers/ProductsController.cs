using DatabaseConsumer.Data;
using DatabaseConsumer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace DatabaseConsumer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {

        private readonly ShopDBContext _context;

        public ProductsController(ShopDBContext context)
        {
            this._context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetAll()
        {
            Log.Information("Fetching all products from the database.");
            return Ok(await _context.Products.ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> Get(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                Log.Warning("Product with id {ProductID} not found.", id);
                return NotFound();
            }
            Log.Information("Retrieved product {@Product}", product);
            return Ok(product);
        }

        [HttpPost]
        public async Task<ActionResult<Product>> Create(Product newProduct)

        {
            _context.Products.Add(newProduct);
            await _context.SaveChangesAsync();

            Log.Information("Created product {@Product}", newProduct);
            return CreatedAtAction(nameof(Get), new { id = newProduct.Id }, newProduct);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Product updatedProduct)
        {
            if (id != updatedProduct.Id)
            {
                Log.Warning("New Product ID does not match existing product ID");
                return BadRequest("Product ID mismatch");
            }

            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null)
            {
                Log.Warning("Product with ID {ProductID} not found", id);
                return NotFound();
            }

            existingProduct.Description = updatedProduct.Description;
            existingProduct.Price = updatedProduct.Price;

            await _context.SaveChangesAsync();

            Log.Information("Updated product {@Product}", existingProduct);
            return Ok(existingProduct);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) {
                Log.Warning("Product with ID {ProductID} not found", id);
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            Log.Information("Deleted product {@Product}", product);
            return NoContent();
        }
    }
}
