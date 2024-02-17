using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Rinha_de_Backend_Q1_2024.Models
{
    // Input passed by the user to create a new transaction.
    public class TransactionInputModel
    {
        // JSON Payload on POST.
        [JsonPropertyName("valor")]
        public int Amount { get; set; }
        [JsonPropertyName("tipo")]
        public char Type { get; set; }
        [JsonPropertyName("descricao")]
        [MaxLength(10)]
        public string? Description { get; set; }
    }

    // Class defining a model for the response of a transaction.
    public class TransactionResponseModel
    {
        // Properties
        // JSON Property names added for the POST return.
        [JsonPropertyName("limite")]
        public int Limit { get; set; }

        [JsonPropertyName("saldo")]
        public int Balance { get; set; }
    }

    // Class defining a model for transactions
    public class Transaction
    {
        // Properties
        [JsonIgnore]
        public int Id { get; set; }

        [JsonIgnore]
        public int CustomerId { get; set; }

        [JsonPropertyName("valor")]
        public int Amount { get; set; }

        [JsonPropertyName("tipo")]
        public char Type { get; set; }

        [JsonPropertyName("descricao")]
        [MaxLength(10)]
        public string? Description { get; set; }

        [JsonPropertyName("realizada_em")]
        public DateTime? DateTime { get; set; }
    }


}
