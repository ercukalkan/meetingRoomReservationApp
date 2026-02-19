using Microsoft.AspNetCore.Mvc;
using Data.Context;
using Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EquipmentController(AppDbContext _context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Equipment>>> GetEquipments()
    {
        var equipments = await _context.Equipments.ToListAsync();
        return Ok(equipments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Equipment>> GetEquipment(Guid id)
    {
        var equipment = await _context.Equipments.FindAsync(id);

        if (equipment == null)
            return NotFound();

        return Ok(equipment);
    }

    [HttpPost]
    public async Task<ActionResult<Equipment>> CreateEquipment(Equipment equipment)
    {
        _context.Equipments.Add(equipment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEquipment), new { id = equipment.Id }, equipment);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEquipment(Guid id, Equipment updatedEquipment)
    {
        if (id != updatedEquipment.Id)
            return BadRequest();

        var equipment = await _context.Equipments.FindAsync(id);
        if (equipment == null)
            return NotFound();

        equipment.Name = updatedEquipment.Name;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEquipment(Guid id)
    {
        var equipment = await _context.Equipments.FindAsync(id);

        if (equipment == null)
            return NotFound();

        _context.Equipments.Remove(equipment);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}