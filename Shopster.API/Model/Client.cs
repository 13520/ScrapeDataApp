using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shopster.API.Model
{
    [Table("Client")]
    public class Client
    {
        [Key]
        public int ID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string ZipCode { get; set; }
        public string Country { get; set; }
    }
}
