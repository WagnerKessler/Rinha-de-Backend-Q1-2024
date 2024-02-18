using Microsoft.AspNetCore.Mvc;
using Npgsql;
using NpgsqlTypes;
using Rinha_de_Backend_Q1_2024.Models;
using System.Data;

namespace Rinha_de_Backend_Q1_2024.Controllers
{
    [ApiController]
    [Route("clientes/{id}/transacoes")]
    public class TransactionController : ControllerBase
    {
        private readonly IDbConnection _connection;

        public TransactionController(IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("DBSettings:ConnectionString");
            _connection = new NpgsqlConnection(connectionString);
            ((NpgsqlConnection)_connection).OpenAsync().Wait(); // Cast to NpgsqlConnection and open asynchronously
        }

        [HttpPost]
        public async Task<ActionResult<TransactionResponseModel>> PostTransaction(int id, [FromBody] TransactionInputModel transactionInput)
        {
            var existingCustomer = await GetCustomerByIdAsync(id);

            if (existingCustomer == null)
            {
                return NotFound();
            }

            if (!IsValidTransactionInput(transactionInput))
            {
                return BadRequest("Invalid request payload");
            }

            if (transactionInput.Type == 'd' && (existingCustomer.Balance - transactionInput.Amount) < -existingCustomer.Limit)
            {
                // Reject debit transaction if it exceeds the limit
                return UnprocessableEntity("Debit transaction would exceed limit");
            }

            if (transactionInput.Type == 'c')
            {
                existingCustomer.Balance += transactionInput.Amount;
            }
            else if (transactionInput.Type == 'd')
            {
                if ((existingCustomer.Balance - transactionInput.Amount) < -existingCustomer.Limit)
                {
                    // Reject debit transaction if it generates inconsistency
                    return UnprocessableEntity("Debit transaction would exceed limit");
                }
                existingCustomer.Balance -= transactionInput.Amount;
            }

            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    var updateCustomerCommandText = "UPDATE public.\"Customers\" SET \"Balance\" = @Balance WHERE \"Id\" = @Id";
                    var updateCustomerParameters = new
                    {
                        Balance = existingCustomer.Balance,
                        Id = existingCustomer.Id
                    };

                    using (var updateCommand = new NpgsqlCommand(updateCustomerCommandText, (NpgsqlConnection)_connection))
                    {
                        updateCommand.Parameters.AddWithValue("@Balance", existingCustomer.Balance);
                        updateCommand.Parameters.AddWithValue("@Id", existingCustomer.Id);

                        await updateCommand.ExecuteNonQueryAsync();
                    }

                    var insertTransactionCommandText = "INSERT INTO public.\"Transactions\" (\"CustomerId\", \"Amount\", \"Type\", \"Description\", \"DateTime\") VALUES (@CustomerId, @Amount, @Type, @Description, @DateTime)";
                    var insertTransactionParameters = new
                    {
                        CustomerId = existingCustomer.Id,
                        Amount = transactionInput.Amount,
                        Type = transactionInput.Type,
                        Description = transactionInput.Description,
                        DateTime = DateTime.UtcNow
                    };

                    using (var insertCommand = new NpgsqlCommand(insertTransactionCommandText, (NpgsqlConnection)_connection))
                    {
                        insertCommand.Parameters.AddWithValue("@CustomerId", existingCustomer.Id);
                        insertCommand.Parameters.AddWithValue("@Amount", transactionInput.Amount);
                        insertCommand.Parameters.AddWithValue("@Type", transactionInput.Type);
                        insertCommand.Parameters.AddWithValue("@Description", transactionInput.Description!);
                        insertCommand.Parameters.AddWithValue("@DateTime", DateTime.UtcNow);

                        await insertCommand.ExecuteNonQueryAsync();
                    }

                    transaction.Commit();

                    return Ok(new TransactionResponseModel
                    {
                        Limit = existingCustomer.Limit,
                        Balance = existingCustomer.Balance
                    });
                }
                catch
                {
                    transaction.Rollback();
                    return StatusCode(500, "Failed to process transaction");
                }
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

        private bool IsValidTransactionInput(TransactionInputModel transactionInput)
        {
            return transactionInput.Amount > 0 &&
                   !string.IsNullOrEmpty(transactionInput.Description) &&
                   transactionInput.Description.Length >= 1 &&
                   transactionInput.Description.Length <= 10 &&
                   (transactionInput.Type == 'c' || transactionInput.Type == 'd');
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