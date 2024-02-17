using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rinha_de_Backend_Q1_2024.Models;
using Rinha_de_Backend_Q1_2024.Services;

namespace Rinha_de_Backend_Q1_2024.Controllers
{
    [ApiController]
    [Route("clientes/{id}/extrato")]
    public class StatementController : ControllerBase
    {


        // SET DB CONTEXT FOR CONTROLLER
        private readonly RinhanDbContext _dbContext;
        public StatementController(RinhanDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<ActionResult<Statement>> GetStatement(int id)
        {

            Customer? ExistingCustomer = await _dbContext.Customers.FindAsync(id);

            if (ExistingCustomer == null)
            {
                return NotFound();
            }

            var Statement = new Statement();
            var Balance = new Balance();

            // Get the transactions from the database.
            Statement.BankStatement = await _dbContext.Transactions
                .Where(t => t.CustomerId == ExistingCustomer.Id)
                .OrderByDescending(t => t.DateTime)
                .Take(10)
                .ToListAsync();

            Balance.Total = ExistingCustomer.Balance;
            Balance.Limit = ExistingCustomer.Limit;
            Statement.Balance = Balance;

            return Ok(Statement);

        }
    }
}
