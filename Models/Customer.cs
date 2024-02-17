using System.Text.Json.Serialization;

namespace Rinha_de_Backend_Q1_2024.Models
{
    // Input passed by the user to create a new customer.
    public class CustomerInputModel
    {
        // JSON Payload on POST.
        public int id { get; set; }
        public int limite { get; set; }
        public int saldo { get; set; }
    }

    // Class defining a model for customers.
    public class Customer
    {
        // Properties
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("limite")]
        public int Limit { get; set; }

        [JsonPropertyName("saldo")]
        public int Balance { get; set; }
    }
}
