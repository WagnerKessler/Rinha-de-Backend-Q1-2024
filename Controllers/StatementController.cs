using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;
using Rinha_de_Backend_Q1_2024.Models;
using System.Data;

namespace Rinha_de_Backend_Q1_2024.Controllers
{
    [ApiController]
    [Route("clientes/{id}/extrato")]
    public class StatementController : ControllerBase
    {
        private readonly IDbConnection _connection;

        public StatementController(IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("DBSettings:ConnectionString");
            _connection = new NpgsqlConnection(connectionString);
            ((NpgsqlConnection)_connection).OpenAsync().Wait();
        }

        [HttpGet]
        public async Task<ActionResult<Statement>> GetStatement(int id)
        {
            try
            {
                var existingCustomer = await GetCustomerByIdAsync(id);

                if (existingCustomer == null)
                {
                    return NotFound();
                }

                var statement = new Statement();
                var balance = new Balance();

                var commandText = "SELECT \"Amount\", \"Type\", \"Description\", \"DateTime\" FROM public.\"Transactions\" WHERE \"CustomerId\" = @CustomerId ORDER BY \"DateTime\" DESC LIMIT 10";
                var parameters = new NpgsqlParameter("@CustomerId", NpgsqlDbType.Integer) { Value = existingCustomer.Id };

                using (var command = new NpgsqlCommand(commandText, (NpgsqlConnection)_connection))
                {
                    command.Parameters.Add(parameters);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var transaction = new Transaction
                            {
                                Amount = reader.GetInt32(0),
                                Type = reader.GetChar(1),
                                Description = reader.GetString(2),
                                DateTime = reader.GetDateTime(3)
                            };

                            statement.BankStatement ??= new List<Transaction>();
                            statement.BankStatement.Add(transaction);
                        }
                    }
                }

                balance.Total = existingCustomer.Balance;
                balance.Limit = existingCustomer.Limit;
                statement.Balance = balance;

                return Ok(statement);
            }
            catch
            {
                return StatusCode(500, "Failed to retrieve statement");
            }
        }

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
