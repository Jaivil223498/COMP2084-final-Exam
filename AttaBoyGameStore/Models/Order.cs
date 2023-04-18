using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AttaBoyGameStore.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public bool InProgress { get; set; }

        [Required]
        [DisplayName("Order Date")]
        public DateTime OrderDate { get; set; }

        [Required]
        [DisplayFormat(DataFormatString = "{0:c}")]
        public decimal Total { get; set; }

        [Required]
        [DisplayName("First Name")]
        [MaxLength(100)]
        public String FirstName { get; set; }

        [Required]
        [DisplayName("Last Name")]
        [MaxLength(100)]
        public String LastName { get; set; }

        [Required]
        public String Address { get; set; }

        [Required]
        [MaxLength(100)]
        public String City { get; set; }

        [Required]
        [MaxLength(15)]
        public String Province { get; set; }

        [Required]
        [DisplayName("Postal Code")]
        [MaxLength(7)]
        public String PostalCode { get; set; }

        [Required]
        [Phone]
        public String Phone { get; set; }

        [Required]
        [DisplayName("Customer")]
        public String CustomerId { get; set; }

        public List<OrderLine>? OrderLines { get; set; }
    }
}
