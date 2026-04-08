using DashboardVendas.Api.Data;
using DashboardVendas.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DashboardVendas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ClientesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Cliente>>> GetAll()
    {
        return Ok(await _context.Clientes.ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Cliente>> GetById(int id)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null) return NotFound(new { message = "Cliente não encontrado." });
        return Ok(cliente);
    }

    [HttpPost]
    public async Task<ActionResult> Create(Cliente cliente)
    {
        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = cliente.Id }, cliente);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> Update(int id, Cliente clienteAtualizado)
    {
        var cliente = await _context.Clientes.FindAsync(id);
        if (cliente == null) return NotFound(new { message = "Cliente não encontrado." });

        cliente.Nome = clienteAtualizado.Nome;
        cliente.Email = clienteAtualizado.Email;
        cliente.Telefone = clienteAtualizado.Telefone;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var cliente = await _context.Clientes
            .Include(c => c.Vendas)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cliente == null) return NotFound(new { message = "Cliente não encontrado." });
        if (cliente.Vendas.Any()) return BadRequest(new { message = "Cliente possui vendas cadastradas e não pode ser excluído." });

        _context.Clientes.Remove(cliente);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}