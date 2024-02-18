using System.Text.Json.Serialization;

namespace Rinha_de_Backend_Q1_2024.Models
{
    // Class defining a model for the bank entire statement.
    public class Statement
    {
        [JsonPropertyName("saldo")]
        public Balance? Balance { get; set; }

        [JsonPropertyName("ultimas_transacoes")]
        public List<Transaction> BankStatement { get; set; } = new List<Transaction>();
    }

    // Class defining the balance details on a bank statement.
    public class Balance
    {
        [JsonPropertyName("total")]
        public int? Total { get; set; }

        [JsonPropertyName("data_extrato")]
        public DateTime DateTime { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("limite")]
        public int? Limit { get; set; }
    }
}


