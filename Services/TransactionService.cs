using Npgsql;
using Rinha_de_Backend_Q1_2024.Models;

namespace Rinha_de_Backend_Q1_2024.Services
{

    public interface ITransactionService
    {
        Task<IResult> HandleTransactionAsync(int customerId, TransactionInputModel transactionInput);
    }

    public class TransactionService : ITransactionService
    {
        private readonly string _connectionString;
        private readonly ICustomerService _customerService;

        public TransactionService(string connectionString, ICustomerService customerService)
        {
            _connectionString = connectionString;
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        }

        // Validate transaction payload
        private static bool IsValidTransactionInput(TransactionInputModel transactionInput)
        {
            return transactionInput.Amount > 0 &&
                   !string.IsNullOrEmpty(transactionInput.Description) &&
                   transactionInput.Description.Length >= 1 &&
                   transactionInput.Description.Length <= 10 &&
                   (transactionInput.Type == 'c' || transactionInput.Type == 'd');
        }

        // Handle transactions for a given customer
        public async Task<IResult> HandleTransactionAsync(int customerId, TransactionInputModel transactionInput)
        {
            if (!IsValidTransactionInput(transactionInput))
            {
                return Results.UnprocessableEntity("Invalid request payload");
            }

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var existingCustomer = await _customerService.LockCustomerAndGetByIdAsync(customerId, connection, transaction);
            if (existingCustomer == null)
            {
                return Results.NotFound();
            }

            if (transactionInput.Type == 'c')
            {
                existingCustomer.Balance += transactionInput.Amount;
            }
            else if (transactionInput.Type == 'd')
            {
                if ((existingCustomer.Balance - transactionInput.Amount) < -existingCustomer.Limit)
                {
                    return Results.UnprocessableEntity("Debit transaction would exceed limit");
                }
                else { existingCustomer.Balance -= transactionInput.Amount; }
            }

            var tableName = "Transactions_" + existingCustomer.Id;
            var updateAndInsertCommandText = $"UPDATE public.\"Customers\" SET \"Balance\" = @Balance WHERE \"Id\" = @Id;" +
                                             $"INSERT INTO public.\"{tableName}\" ( \"Amount\", \"Type\", \"Description\") VALUES (@Amount, @Type, @Description);";

            try
            {

                using (var updateAndInsertCommand = new NpgsqlCommand(updateAndInsertCommandText, connection))
                {
                    updateAndInsertCommand.Parameters.AddWithValue("@Balance", existingCustomer.Balance);
                    updateAndInsertCommand.Parameters.AddWithValue("@Id", existingCustomer.Id);
                    updateAndInsertCommand.Parameters.AddWithValue("@Amount", transactionInput.Amount);
                    updateAndInsertCommand.Parameters.AddWithValue("@Type", transactionInput.Type);
                    updateAndInsertCommand.Parameters.AddWithValue("@Description", transactionInput.Description!);
                    await updateAndInsertCommand.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return Results.Ok(new TransactionResponseModel
                {
                    Limit = existingCustomer.Limit,
                    Balance = existingCustomer.Balance
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                return Results.StatusCode(500);
            }
        }
    }
}