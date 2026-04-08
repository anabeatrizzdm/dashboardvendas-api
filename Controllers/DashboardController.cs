using DashboardVendas.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardVendas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult> Get()
    {
        var totalVendas = await _context.Vendas.SumAsync(v => (decimal?)v.ValorTotal) ?? 0;
        var quantidadeVendas = await _context.Vendas.CountAsync();
        var totalClientes = await _context.Clientes.CountAsync();
        var totalProdutos = await _context.Produtos.CountAsync();

        return Ok(new
        {
            totalVendas,
            quantidadeVendas,
            totalClientes,
            totalProdutos
        });
    }
}