using Npgsql;
using Rinha_de_Backend_Q1_2024.Models;

namespace Rinha_de_Backend_Q1_2024.Services
{
    public interface ICustomerService
    {
        Task<Customer?> GetCustomerByIdAsync(int id);
        Task<Customer?> LockCustomerAndGetByIdAsync(int id, NpgsqlConnection connection, NpgsqlTransaction transaction);
        Task<List<Customer>> GetAllCustomersAsync();
        Task<IResult> HandleCustomerCreationAsync(Customer newCustomer);
    }

    public class CustomerService : ICustomerService
    {
        private readonly string _connectionString;

        public CustomerService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            var commandText = "SELECT \"Id\", \"Limit\", \"Balance\" FROM public.\"Customers\" WHERE \"Id\" = @Id";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var command = new NpgsqlCommand(commandText, connection))
            {
                command.Parameters.AddWithValue("@Id", id);

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

        public async Task<Customer?> LockCustomerAndGetByIdAsync(int id, NpgsqlConnection connection, NpgsqlTransaction transaction)
        {
            var commandText = "SELECT \"Id\", \"Limit\", \"Balance\" FROM public.\"Customers\" WHERE \"Id\" = @Id FOR UPDATE";

            using (var command = new NpgsqlCommand(commandText, connection, transaction))
            {
                command.Parameters.AddWithValue("@Id", id);

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

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var commandText = "SELECT \"Id\", \"Limit\", \"Balance\" FROM public.\"Customers\"";
            var customers = new List<Customer>();

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var command = new NpgsqlCommand(commandText, connection))
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
            return customers;
        }

        public async Task<IResult> HandleCustomerCreationAsync(Customer newCustomer)
        {
            if (newCustomer == null)
            {
                return Results.BadRequest("Invalid input");
            }

            var commandText = "INSERT INTO public.\"Customers\" (\"Id\", \"Limit\", \"Balance\") VALUES (@Id, @Limit, @Balance) RETURNING *";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var command = new NpgsqlCommand(commandText, connection))
            {
                command.Parameters.AddWithValue("@Id", newCustomer.Id);
                command.Parameters.AddWithValue("@Limit", newCustomer.Limit);
                command.Parameters.AddWithValue("@Balance", newCustomer.Balance);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var createdCustomer = new Customer
                        {
                            Id = reader.GetInt32(0),
                            Limit = reader.GetInt32(1),
                            Balance = reader.GetInt32(2)
                        };

                        return Results.Created($"/clientes/{createdCustomer.Id}", createdCustomer);
                    }
                }
            }

            return Results.BadRequest("Failed to create customer");
        }
    }
}
