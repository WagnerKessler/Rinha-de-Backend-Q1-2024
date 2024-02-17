using Microsoft.AspNetCore.Mvc;
using Rinha_de_Backend_Q1_2024.Models;
using Rinha_de_Backend_Q1_2024.Services;

namespace Rinha_de_Backend_Q1_2024.Controllers
{
    [ApiController]
    [Route("clientes/{id}/transacoes")]
    public class TransactionController : ControllerBase
    {

        // SET DB CONTEXT FOR CONTROLLER
        private readonly RinhanDbContext _dbContext;
        public TransactionController(RinhanDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpPost]
        public async Task<ActionResult<TransactionResponseModel>> PostTransaction(int id, [FromBody] TransactionInputModel transactionInput)
        {

            Customer? ExistingCustomer = await _dbContext.Customers.FindAsync(id);

            if (ExistingCustomer == null)
            {
                return NotFound();
            }

            if (transactionInput.Amount <= 0 || transactionInput.Description == null || transactionInput.Description.Length < 1 || transactionInput.Description.Length > 10 || (transactionInput.Type != 'c' && transactionInput.Type != 'd'))
            {
                return BadRequest("Invalid request payload");
            }

            if (transactionInput.Type == 'd' && (ExistingCustomer.Balance - transactionInput.Amount) < -ExistingCustomer.Limit)
            {
                // Reject debit transaction if it exceeds the limit
                return UnprocessableEntity("Debit transaction would exceed limit");
            }

            if (transactionInput.Type == 'c')
            {
                ExistingCustomer.Balance += transactionInput.Amount;

            }
            else if (transactionInput.Type == 'd')
            {
                if ((ExistingCustomer.Balance - transactionInput.Amount) < -ExistingCustomer.Limit)
                {
                    // Reject debit transaction if it generates inconsistency
                    return UnprocessableEntity("Debit transaction would exceed limit");
                }
                ExistingCustomer.Balance -= transactionInput.Amount;
            }

            // Save the transactions to the database.
            _dbContext.Customers.Update(ExistingCustomer);
            _dbContext.Transactions.Add(new Transaction
            {
                Amount = transactionInput.Amount,
                CustomerId = ExistingCustomer.Id,
                Description = transactionInput.Description,
                Type = transactionInput.Type,
                DateTime = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync();

            return Ok(new TransactionResponseModel
            {
                Limit = ExistingCustomer.Limit,
                Balance = ExistingCustomer.Balance
            });
        }
    }
}