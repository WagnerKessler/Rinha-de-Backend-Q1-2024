using Microsoft.EntityFrameworkCore;
using Rinha_de_Backend_Q1_2024.Services;

namespace Rinha_de_Backend_Q1_2024
{
    public class Program
    {
        public static void Main(string[] args)
        {

            // Variables
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetValue<string>("DBSettings:ConnectionString");

            // Services
            builder.Services.AddControllers();
            builder.Services.AddDbContextPool<RinhanDbContext>(options => options.UseNpgsql(connectionString), poolSize: 100); // Use PostgreSQL to process transactions and statements.

            // Build
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.MapControllers();
            app.Run();

        }
    }
}
