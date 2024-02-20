using Npgsql;
using NpgsqlTypes;
using Rinha_de_Backend_Q1_2024.Models;

namespace Rinha_de_Backend_Q1_2024.Services
{
    public interface IStatementService
    {
        Task<Statement?> GetStatementForCustomerAsync(int customerId);
    }

    public class StatementService(NpgsqlConnection connection, ICustomerService customerService) : IStatementService
    {
        private readonly NpgsqlConnection _connection = connection;
        private readonly ICustomerService _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));

        // Retrieve the statement for a given customer
        public async Task<Statement?> GetStatementForCustomerAsync(int customerId)
        {
            var existingCustomer = await _customerService.GetCustomerByIdAsync(customerId);

            if (existingCustomer == null)
            {
                return null;
            }

            var statement = new Statement();

            var commandText = "SELECT \"Amount\", \"Type\", \"Description\", \"DateTime\" FROM public.\"Transactions\" WHERE \"CustomerId\" = @CustomerId ORDER BY \"DateTime\" DESC LIMIT 10";
            var parameters = new NpgsqlParameter("@CustomerId", NpgsqlDbType.Integer) { Value = existingCustomer.Id };

            using (var command = new NpgsqlCommand(commandText, _connection))
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

            var balance = new Balance
            {
                Total = existingCustomer.Balance,
                Limit = existingCustomer.Limit
            };

            statement.Balance = balance;

            return statement;
        }

    }
}
