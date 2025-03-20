using Microsoft.EntityFrameworkCore;
using PayMeChat.V1.Backend.Entities;

namespace PayMeChat_V._1.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<UsuarioPayMeChat> UsuarioPayMeChat { get; set; }  
    }
}
