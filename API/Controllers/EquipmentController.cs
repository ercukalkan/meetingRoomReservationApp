using Microsoft.AspNetCore.Mvc;
using Data.Context;
using Data.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Exceptions;
using Data.DTOs;
using System.ComponentModel.DataAnnotations;
using Data.Response;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EquipmentController(AppDbContext _context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ResponseSchema<IReadOnlyList<EquipmentDTO>>>> GetEquipments()
    {
        var equipments = await _context.Equipments
            .Select(e => new EquipmentDTO { Id = e.Id, Name = e.Name })
            .ToListAsync();
        return Ok(new ResponseSchema<IReadOnlyList<EquipmentDTO>>
        {
            Message = "Equipments retrieved successfully.",
            Success = true,
            Data = equipments
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseSchema<EquipmentDTO>>> GetEquipment(Guid id)
    {
        var equipment = await _context.Equipments.FindAsync(id)
            ?? throw new NotFoundException($"Equipment with ID {id} not found.");

        var equipmentDto = new EquipmentDTO
        {
            Id = equipment.Id,
            Name = equipment.Name
        };

        return Ok(new ResponseSchema<EquipmentDTO>
        {
            Message = "Equipment retrieved successfully.",
            Success = true,
            Data = equipmentDto
        });
    }

    [HttpPost]
    public async Task<ActionResult<ResponseSchema<EquipmentDTO>>> CreateEquipment(EquipmentDTO dto)
    {
        if (dto == null)
            throw new BadRequestException("Equipment data is required.");

        var equipment = new Equipment
        {
            Name = dto.Name!
        };

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(equipment);

        if (!Validator.TryValidateObject(equipment, validationContext, validationResults, true))
        {
            var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Validation error").ToList();
            throw new BadRequestException(string.Join("; ", errors));
        }

        _context.Equipments.Add(equipment);
        await _context.SaveChangesAsync();

        var equipmentDto = new EquipmentDTO
        {
            Id = equipment.Id,
            Name = equipment.Name
        };

        return CreatedAtAction(nameof(GetEquipment), new { id = equipment.Id }, new ResponseSchema<EquipmentDTO>
        {
            Message = "Equipment created successfully.",
            Success = true,
            Data = equipmentDto
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEquipment(Guid id, EquipmentDTO dto)
    {
        if (dto == null)
            throw new BadRequestException("Equipment data is required.");

        var updatedEquipment = await _context.Equipments.FindAsync(id)
            ?? throw new NotFoundException($"Equipment with ID {id} not found.");

        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(updatedEquipment);

        if (!Validator.TryValidateObject(updatedEquipment, validationContext, validationResults, true))
        {
            var errors = validationResults.Select(vr => vr.ErrorMessage ?? "Validation error").ToList();
            throw new BadRequestException(string.Join("; ", errors));
        }

        if (id != updatedEquipment.Id)
            throw new BadRequestException("ID in URL does not match ID in body.");

        var equipment = await _context.Equipments.FindAsync(id)
            ?? throw new NotFoundException($"Equipment with ID {id} not found.");

        equipment.Name = updatedEquipment.Name;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEquipment(Guid id)
    {
        var equipment = await _context.Equipments.FindAsync(id)
            ?? throw new NotFoundException($"Equipment with ID {id} not found.");

        _context.Equipments.Remove(equipment);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}