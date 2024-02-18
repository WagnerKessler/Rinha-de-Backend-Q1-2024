using System.Text.Json.Serialization;

namespace Rinha_de_Backend_Q1_2024.Models
{
    // Class defining a model for customers.
    public class Customer
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("limite")]
        public int Limit { get; set; }

        [JsonPropertyName("saldo")]
        public int Balance { get; set; }
    }
}
