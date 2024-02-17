using Microsoft.EntityFrameworkCore;
using Rinha_de_Backend_Q1_2024.Models;

namespace Rinha_de_Backend_Q1_2024.Services
{
    public class RinhanDbContext : DbContext
    {
        public RinhanDbContext()
        {
        }
        public RinhanDbContext(DbContextOptions<RinhanDbContext> options) : base(options)
        {
        }


        // DB Sets 
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

    }

}
