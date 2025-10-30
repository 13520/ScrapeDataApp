using Microsoft.EntityFrameworkCore;
using Shopster.API.Model;

namespace Shopster.API
{
    public class AppDBContext(DbContextOptions options):DbContext(options)
    {
        public DbSet<Client> Clients { get; set; }
    }
}
