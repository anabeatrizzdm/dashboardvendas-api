using DashboardVendas.Api.Data;
using DashboardVendas.Api.Dtos;
using DashboardVendas.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardVendas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VendasController : ControllerBase
{
    private readonly AppDbContext _context;

    public VendasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var vendas = await _context.Vendas
            .Include(v => v.Cliente)
            .Include(v => v.Itens)
                .ThenInclude(i => i.Produto)
            .OrderByDescending(v => v.DataVenda)
            .AsNoTracking()
            .ToListAsync();

        return Ok(vendas);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetById(int id)
    {
        var venda = await _context.Vendas
            .Include(v => v.Cliente)
            .Include(v => v.Itens)
                .ThenInclude(i => i.Produto)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id);

        if (venda == null)
            return NotFound(new { message = "Venda não encontrada." });

        return Ok(venda);
    }

    [HttpPost]
    public async Task<ActionResult> Create(CreateVendaDto dto)
    {
        var cliente = await _context.Clientes.FindAsync(dto.ClienteId);
        if (cliente == null)
            return BadRequest(new { message = "Cliente não encontrado." });

        if (dto.Itens == null || dto.Itens.Count == 0)
            return BadRequest(new { message = "A venda deve ter pelo menos um item." });

        var venda = new Venda
        {
            ClienteId = dto.ClienteId,
            DataVenda = DateTime.UtcNow,
            Itens = new List<ItemVenda>()
        };

        decimal total = 0;

        foreach (var itemDto in dto.Itens)
        {
            var produto = await _context.Produtos.FindAsync(itemDto.ProdutoId);

            if (produto == null)
                return BadRequest(new { message = $"Produto {itemDto.ProdutoId} não encontrado." });

            if (itemDto.Quantidade <= 0)
                return BadRequest(new { message = "Quantidade inválida." });

            if (produto.Estoque < itemDto.Quantidade)
                return BadRequest(new { message = $"Estoque insuficiente para o produto {produto.Nome}." });

            var subtotal = produto.Preco * itemDto.Quantidade;
            total += subtotal;

            produto.Estoque -= itemDto.Quantidade;

            venda.Itens.Add(new ItemVenda
            {
                ProdutoId = produto.Id,
                Quantidade = itemDto.Quantidade,
                PrecoUnitario = produto.Preco,
                Subtotal = subtotal
            });
        }

        venda.ValorTotal = total;

        _context.Vendas.Add(venda);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Venda cadastrada com sucesso.",
            vendaId = venda.Id
        });
    }
}