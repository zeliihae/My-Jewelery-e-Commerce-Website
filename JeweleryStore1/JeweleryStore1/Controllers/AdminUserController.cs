using JeweleryStore1.Data;
using JeweleryStore1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace JeweleryStore1.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly JewDbContext _context;

        public UsersController(JewDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/Users
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Select(u => new {
                    u.UserId,
                    u.UserName,
                    u.UserEmail,
                    u.UserPhone,
                    u.UserStatus,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = users });
        }

        // DELETE: api/admin/Users/{id}
  
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // PUT: api/admin/Users/{id}/status
        
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] int newStatus)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            
            user.UserStatus = (byte)newStatus;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = user.UserStatus });
        }
    }
}