using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;
using Rinha_de_Backend_Q1_2024.Models;
using System.Data;

namespace Rinha_de_Backend_Q1_2024.Controllers
{
    [ApiController]
    [Route("clientes")]
    public class CustomerController : ControllerBase, IDisposable
    {
        private readonly IDbConnection _connection;

        public CustomerController(IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("DBSettings:ConnectionString");
            _connection = new NpgsqlConnection(connectionString);
            ((NpgsqlConnection)_connection).OpenAsync().Wait();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            try
            {
                var commandText = "SELECT * FROM public.\"Customers\"";
                var customers = new List<Customer>();

                using (var command = new NpgsqlCommand(commandText, (NpgsqlConnection)_connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var customer = new Customer
                        {
                            Id = reader.GetInt32(0),
                            Limit = reader.GetInt32(1),
                            Balance = reader.GetInt32(2)
                        };

                        customers.Add(customer);
                    }
                }

                if (customers.Count == 0)
                {
                    return NoContent();
                }

                return customers;
            }
            catch
            {
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            try
            {
                var customer = await GetCustomerByIdAsync(id);

                if (customer == null)
                {
                    return NotFound();
                }

                return customer;
            }
            catch
            {
                return StatusCode(500, "Internal Server Error");
            }
        }
        [HttpPost]
        public async Task<ActionResult<Customer>> CreateCustomer([FromBody] Customer Customer)
        {
            try
            {
                if (Customer == null)
                {
                    return BadRequest("Invalid input");
                }

                var newCustomer = new Customer
                {
                    Id = Customer.Id,
                    Limit = Customer.Limit,
                    Balance = Customer.Balance
                };

                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        var commandText = "INSERT INTO public.\"Customers\" (\"Id\", \"Limit\", \"Balance\") VALUES (@Id, @Limit, @Balance)";

                        using (var command = new NpgsqlCommand(commandText, (NpgsqlConnection)_connection))
                        {
                            command.Parameters.AddWithValue("@Id", newCustomer.Id);
                            command.Parameters.AddWithValue("@Limit", newCustomer.Limit);
                            command.Parameters.AddWithValue("@Balance", newCustomer.Balance);

                            await command.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();

                        return CreatedAtAction(nameof(GetCustomer), new { id = newCustomer.Id }, newCustomer);
                    }
                    catch
                    {
                        transaction.Rollback();
                        return StatusCode(500, "Failed to create customer");
                    }
                }
            }
            catch
            {
                return StatusCode(500, "Internal Server Error");
            }
        }

        // Retrieve Customer by ID
        private async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            var commandText = "SELECT * FROM public.\"Customers\" WHERE \"Id\" = @Id";
            var parameters = new NpgsqlParameter("@Id", NpgsqlDbType.Integer) { Value = id };

            using (var command = new NpgsqlCommand(commandText, (NpgsqlConnection)_connection))
            {
                command.Parameters.Add(parameters);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new Customer
                        {
                            Id = reader.GetInt32(0),
                            Limit = reader.GetInt32(1),
                            Balance = reader.GetInt32(2)
                        };
                    }
                }
            }

            return null;
        }

        // Dispose method to ensure proper cleanup
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_connection is NpgsqlConnection npgsqlConnection)
                {
                    npgsqlConnection.Dispose();
                }
            }
        }
    }
}