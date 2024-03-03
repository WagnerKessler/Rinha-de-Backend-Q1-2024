using Npgsql;
using Rinha_de_Backend_Q1_2024.Models;

namespace Rinha_de_Backend_Q1_2024.Services
{
    public interface IStatementService
    {
        Task<Statement?> GetStatementForCustomerAsync(int customerId);
    }

    public class StatementService : IStatementService
    {
        private readonly string _connectionString;
        private readonly ICustomerService _customerService;

        public StatementService(string connectionString, ICustomerService customerService)
        {
            _connectionString = connectionString;
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        }

        public async Task<Statement?> GetStatementForCustomerAsync(int customerId)
        {
            var existingCustomer = await _customerService.GetCustomerByIdAsync(customerId);

            if (existingCustomer == null)
            {
                return null;
            }

            var statement = new Statement { BankStatement = new List<Transaction>() };
            var tableName = "Transactions_" + existingCustomer.Id;
            var commandText = $"SELECT \"Amount\", \"Type\", \"Description\", \"DateTime\" FROM public.\"{tableName}\" ORDER BY \"Id\" DESC LIMIT 10";


            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var command = new NpgsqlCommand(commandText, connection))
            {
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