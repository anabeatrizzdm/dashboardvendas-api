namespace DashboardVendas.Api.Dtos;

public class CreateVendaDto
{
    public int ClienteId { get; set; }
    public List<CreateItemVendaDto> Itens { get; set; } = new();
}

public class CreateItemVendaDto
{
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
}