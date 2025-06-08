namespace DatabaseConsumer.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
