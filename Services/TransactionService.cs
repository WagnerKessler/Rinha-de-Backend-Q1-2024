using Npgsql;
using Rinha_de_Backend_Q1_2024.Models;

namespace Rinha_de_Backend_Q1_2024.Services
{

    public interface ITransactionService
    {
        Task<IResult> HandleTransactionAsync(int customerId, TransactionInputModel transactionInput);
    }

    public class TransactionService(NpgsqlConnection connection, ICustomerService customerService) : ITransactionService
    {
        private readonly NpgsqlConnection _connection = connection;
        private readonly ICustomerService _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));

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
            var existingCustomer = await _customerService.GetCustomerByIdAsync(customerId);

            if (existingCustomer == null)
            {
                return Results.NotFound();
            }

            if (!IsValidTransactionInput(transactionInput))
            {
                return Results.BadRequest("Invalid request payload");
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

            using var transaction = _connection.BeginTransaction();
            try
            {
                var updateCustomerCommandText = "UPDATE public.\"Customers\" SET \"Balance\" = @Balance WHERE \"Id\" = @Id";
                var updateCustomerParameters = new
                {
                    Balance = existingCustomer.Balance,
                    Id = existingCustomer.Id
                };

                using var updateCommand = new NpgsqlCommand(updateCustomerCommandText, _connection);
                updateCommand.Parameters.AddWithValue("@Balance", existingCustomer.Balance);
                updateCommand.Parameters.AddWithValue("@Id", existingCustomer.Id);

                await updateCommand.ExecuteNonQueryAsync();

                var insertTransactionCommandText = "INSERT INTO public.\"Transactions\" (\"CustomerId\", \"Amount\", \"Type\", \"Description\", \"DateTime\") VALUES (@CustomerId, @Amount, @Type, @Description, @DateTime)";
                var insertTransactionParameters = new
                {
                    CustomerId = existingCustomer.Id,
                    Amount = transactionInput.Amount,
                    Type = transactionInput.Type,
                    Description = transactionInput.Description!,
                    DateTime = DateTime.UtcNow
                };

                using var insertCommand = new NpgsqlCommand(insertTransactionCommandText, _connection);
                insertCommand.Parameters.AddWithValue("@CustomerId", existingCustomer.Id);
                insertCommand.Parameters.AddWithValue("@Amount", transactionInput.Amount);
                insertCommand.Parameters.AddWithValue("@Type", transactionInput.Type);
                insertCommand.Parameters.AddWithValue("@Description", transactionInput.Description!);
                insertCommand.Parameters.AddWithValue("@DateTime", DateTime.UtcNow);

                await insertCommand.ExecuteNonQueryAsync();

                transaction.Commit();

                return Results.Ok(new TransactionResponseModel
                {
                    Limit = existingCustomer.Limit,
                    Balance = existingCustomer.Balance
                });
            }
            catch
            {
                transaction.Rollback();
                return Results.StatusCode(500);
            }
        }

    }
}