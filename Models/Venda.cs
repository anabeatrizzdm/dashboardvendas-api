namespace DashboardVendas.Api.Models;

public class Venda
{
    public int Id { get; set; }
    public DateTime DataVenda { get; set; } = DateTime.UtcNow;
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public decimal ValorTotal { get; set; }

    public List<ItemVenda> Itens { get; set; } = new();
}