using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rinha_de_Backend_Q1_2024.Models;
using Rinha_de_Backend_Q1_2024.Services;

namespace Rinha_de_Backend_Q1_2024.Controllers
{

    [ApiController]
    [Route("clientes")]
    public class CustomerController : ControllerBase
    {

        // SET DB CONTEXT FOR CONTROLLER
        private readonly RinhanDbContext _dbContext;
        public CustomerController(RinhanDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // GET ALL CUSTOMERS
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            List<Customer>? Customers = await _dbContext.Customers.ToListAsync<Customer>();

            if (Customers == null || Customers.Count == 0)
            {
                return NoContent();
            }

            return Customers;
        }

        // GET CUSTOMER BY ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            Customer? ExistingCustomer = await _dbContext.Customers.FindAsync(id);

            if (ExistingCustomer == null)
            {
                return NotFound();
            }

            return ExistingCustomer;
        }

        // CREATE CUSTOMER
        [HttpPost]
        public async Task<ActionResult<Customer>> CreateCustomer([FromBody] CustomerInputModel customerInput)
        {
            if (customerInput == null)
            {
                return BadRequest("Invalid input");
            }

            Customer newCustomer = new Customer
            {
                Id = customerInput.id,
                Limit = customerInput.limite,
                Balance = customerInput.saldo
            };

            // Save the new customer to the database
            _dbContext.Customers.Add(newCustomer);
            await _dbContext.SaveChangesAsync();

            // Return the created object
            return CreatedAtAction(nameof(GetCustomer), new { id = newCustomer.Id }, newCustomer);
        }

    }

}
