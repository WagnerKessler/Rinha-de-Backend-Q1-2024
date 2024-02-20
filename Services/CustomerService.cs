using Npgsql;
using NpgsqlTypes;
using Rinha_de_Backend_Q1_2024.Models;

namespace Rinha_de_Backend_Q1_2024.Services
{
    public interface ICustomerService
    {
        Task<Customer?> GetCustomerByIdAsync(int id);
        Task<List<Customer>> GetAllCustomersAsync();
        Task<IResult> HandleCustomerCreationAsync(Customer newCustomer);
    }

    public class CustomerService(NpgsqlConnection connection) : ICustomerService
    {
        private readonly NpgsqlConnection _connection = connection;

        // Retrieve a particular customer based on their ID
        public async Task<Customer?> GetCustomerByIdAsync(int id)
        {
            var commandText = "SELECT * FROM public.\"Customers\" WHERE \"Id\" = @Id";
            var parameters = new NpgsqlParameter("@Id", NpgsqlDbType.Integer) { Value = id };

            using (var command = new NpgsqlCommand(commandText, _connection))
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

        // Retrieve all customers
        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            var commandText = "SELECT * FROM public.\"Customers\"";
            var customers = new List<Customer>();

            using (var command = new NpgsqlCommand(commandText, _connection))
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

        // Create a new customer
        public async Task<IResult> HandleCustomerCreationAsync(Customer newCustomer)
        {
            if (newCustomer == null)
            {
                return Results.BadRequest("Invalid input");
            }

            var commandText = "INSERT INTO public.\"Customers\" (\"Id\", \"Limit\", \"Balance\") VALUES (@Id, @Limit, @Balance) RETURNING *";

            using (var command = new NpgsqlCommand(commandText, _connection))
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
