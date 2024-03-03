using System.Text.Json.Serialization;

namespace Rinha_de_Backend_Q1_2024.Models
{
    // Input passed by the user to create a new transaction.
    public class TransactionInputModel
    {
        [JsonPropertyName("valor")]
        public int Amount { get; set; }
        [JsonPropertyName("tipo")]
        public char Type { get; set; }
        [JsonPropertyName("descricao")]
        public string? Description { get; set; }
    }

    // Class defining a model for the response of a transaction.
    public class TransactionResponseModel
    {
        [JsonPropertyName("limite")]
        public int Limit { get; set; }

        [JsonPropertyName("saldo")]
        public int Balance { get; set; }
    }

    // Class defining a model for transactions
    public class Transaction
    {
        [JsonPropertyName("valor")]
        public int? Amount { get; set; }

        [JsonPropertyName("tipo")]
        public char? Type { get; set; }

        [JsonPropertyName("descricao")]
        public string? Description { get; set; }

        [JsonPropertyName("realizada_em")]
        public DateTime? DateTime { get; set; }
    }
}
