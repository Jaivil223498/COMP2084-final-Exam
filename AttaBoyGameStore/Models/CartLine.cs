using System.ComponentModel.DataAnnotations;

namespace AttaBoyGameStore.Models
{
    public class CartLine
    {
        public int Id { get; set; }
        public int Quantity { get; set; }

        [DisplayFormat(DataFormatString = "{0:c}")]
        public decimal Price { get; set; }

        public int ProductId { get; set; }

        public string CustomerId { get; set; } // Customer email, or GUID (aka. UUID)

        public Product? Product { get; set; }
    }
}
