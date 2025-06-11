using DatabaseConsumer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Serilog;

namespace DatabaseConsumer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsAPIController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public ProductsAPIController(IConfiguration configuration)
        {
            _configuration = configuration;
            var connection = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connection))
            {
                throw new ArgumentException("Connection string 'DefaultConnection' is not configured.");
            }
            _connectionString = connection;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetAll()
        {
            Log.Information("Fetching all products from the database.");

            var products = new List<Product>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT Id, Description, Price
                    FROM Products 
                    ORDER BY Id 
                    OFFSET @Offset ROWS 
                    FETCH NEXT @PageSize ROWS ONLY;";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Offset", 0);
                    command.Parameters.AddWithValue("@PageSize", 10);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var product = new Product
                            {
                                Id = reader.GetInt32(0),
                                Description = reader.GetString(1),
                                Price = reader.GetDecimal(2)
                            };
                            products.Add(product);
                        }
                    }
                }
            }
            return products;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> Get(int id)
        {
            const string query = "SELECT * FROM Products WHERE Id = @id";
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var product = new Product
                {
                    Id = reader.GetInt32(0),
                    Description = reader.GetString(1),
                    Price = reader.GetDecimal(2)
                };

                Log.Information("Retrieved product {@Product}", product);
                return Ok(product);
            }
            Log.Warning("Product with id {ProductID} not found.", id);
            return NotFound();
        }

        [HttpPost]
        public async Task<ActionResult<Product>> Create(Product newProduct)
        {
            int newId;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                INSERT INTO Products (Description, Price)
                VALUES (@description, @price);
                SELECT CAST(SCOPE_IDENTITY() AS int);";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@description", newProduct.Description);
            command.Parameters.AddWithValue("@price", newProduct.Price);

            newId = (int)await command.ExecuteScalarAsync();
            newProduct.Id = newId;

            Log.Information("Created product {@Product}", newProduct);
            return CreatedAtAction(nameof(Get), new { id = newId }, newProduct);

        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Product updatedProduct)
        {
            if (id != updatedProduct.Id)
            {
                Log.Warning("Mismatched ID in PUT: URL {UrlID} and Body {BodyID}", id, updatedProduct.Id);
                return BadRequest();
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = @"
                UPDATE Products
                SET Description = @description, Price = @price
                WHERE Id = @id";
            
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@description", updatedProduct.Description);
            command.Parameters.AddWithValue("@price", updatedProduct.Price);
            command.Parameters.AddWithValue("@id", id);

            int rowsAffected = await command.ExecuteNonQueryAsync();
            if (rowsAffected == 0)
            {
                Log.Warning("Attempted to update non-existent product with ID {Id}", id);
                return NotFound();
            }

            Log.Information("Updated product {@Product}", updatedProduct);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            const string query = "DELETE FROM Products WHERE Id = @id";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);

            int rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected == 0)
            {
                Log.Warning("Attempted to delete non-existent product with ID {Id}", id);
                return NotFound();
            }

            Log.Information("Deleted product with id {ID}", id);
            return NoContent();
        }
    }
}
