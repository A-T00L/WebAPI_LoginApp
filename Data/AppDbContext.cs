using Microsoft.EntityFrameworkCore;
using WebAPI_LoginApp.Models;

namespace WebAPI_LoginApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        //Creating DbSet for the tables required in the Database
        public DbSet<User> Users { get; set; }
    }
}
