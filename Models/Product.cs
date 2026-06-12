using System.ComponentModel.DataAnnotations;

namespace BakeryOrderSystem.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public bool IsAvailable { get; set; }
        public string Category { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        public Category? CategoryObj { get; set; }
    }
}