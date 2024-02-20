using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Rinha_de_Backend_Q1_2024.Models;
using Rinha_de_Backend_Q1_2024.Services;


namespace Rinha_de_Backend_Q1_2024
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // General variables and options for the builder
            var builder = WebApplication.CreateSlimBuilder(args);
            var connectionString = builder.Configuration.GetValue<string>("DBSettings:ConnectionString");

            // Register the services that will handle the required logic
            builder.Services.AddSingleton<ICustomerService, CustomerService>();
            builder.Services.AddSingleton<ITransactionService, TransactionService>();
            builder.Services.AddSingleton<IStatementService, StatementService>();
            builder.Services.AddSingleton<NpgsqlConnection>(_ => new NpgsqlConnection(connectionString));

            // Build the application
            var app = builder.Build();

            // Open SQL Connection (Pool is automatically managed by Npgsql)
            var connection = app.Services.GetRequiredService<NpgsqlConnection>();
            connection.OpenAsync().Wait();

            // Create the services
            var customerService = app.Services.GetRequiredService<ICustomerService>();
            var transactionService = app.Services.GetRequiredService<ITransactionService>();
            var statementService = app.Services.GetRequiredService<IStatementService>();


            /*****************************************************
                                ROUTES & HANDLERS
            ******************************************************/

            /********* CUSTOMERS *********/

            // ROUTE - Get all customers
            app.MapGet("/clientes", async () =>
            {
                // Call the GetAllCustomersAsync method from the service
                var customers = await customerService.GetAllCustomersAsync();

                // Return OK with the list of customers (or NoContent if empty)
                return customers.Any() ? Results.Ok(customers) : Results.NoContent();
            }).Produces<Customer[]>();

            // ROUTE - Get a customer by ID
            app.MapGet("/clientes/{id}", async (int id) =>
            {
                // Call the GetCustomerByIdAsync method from the service
                var existingCustomer = await customerService.GetCustomerByIdAsync(id);

                // Check if the customer exists
                if (existingCustomer != null)
                {
                    // Return OK with the existing customer
                    return Results.Ok(existingCustomer);
                }

                // Return NotFound if the customer does not exist
                return Results.NotFound();
            }).Produces<Customer>().WithName("GetCustomerById");

            // ROUTE - Create a new customer.
            app.MapPost("/clientes", async (Customer newCustomer) =>
            {
                // Call the HandleCustomerCreationAsync method from the service
                return await customerService.HandleCustomerCreationAsync(newCustomer);
            }).Produces<Customer>();

            /********* TRANSACTIONS *********/

            // ROUTE - Create new transaction for a customer
            app.MapPost("/clientes/{id}/transacoes", async (int id, [FromBody] TransactionInputModel transactionInput) =>
            {
                // Call the HandleTransactionAsync method from the service
                return await transactionService.HandleTransactionAsync(id, transactionInput);
            }).Produces<TransactionResponseModel>();

            /********* STATEMENTS *********/

            // ROUTE - Create a new statement for a given customer
            app.MapGet("/clientes/{id}/extrato", async (int id) =>
            {
                // Call the GetStatementForCustomerAsync method from the service
                var customerStatement = await statementService.GetStatementForCustomerAsync(id);

                // Return Ok with the statement (or NotFound if customer not found)
                return customerStatement != null ? Results.Ok(customerStatement) : Results.NotFound();
            }).Produces<Statement>();


            /*****************************************************
                          RUNS THE APPLICATION
             *****************************************************/
            app.Run();

        } // End-Main
    } // End-Program
} // End-Namespace
