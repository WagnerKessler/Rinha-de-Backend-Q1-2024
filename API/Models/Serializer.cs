using System.Text.Json.Serialization;

namespace Rinha_de_Backend_Q1_2024.Models
{
    [JsonSerializable(typeof(Customer))]
    [JsonSerializable(typeof(List<Customer>))]
    [JsonSerializable(typeof(Statement))]
    [JsonSerializable(typeof(Balance))]
    [JsonSerializable(typeof(TransactionInputModel))]
    [JsonSerializable(typeof(List<Customer>))]
    [JsonSerializable(typeof(TransactionResponseModel))]
    [JsonSerializable(typeof(Transaction))]
    [JsonSerializable(typeof(List<Transaction>))]
    public partial class SerializerContext : JsonSerializerContext { }
}
