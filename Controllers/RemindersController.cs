using Microsoft.AspNetCore.Mvc;
using BudgetTracker.Services;
using BudgetTracker.Models;
using System;
using System.Threading.Tasks;

namespace BudgetTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RemindersController : ControllerBase
    {
        private readonly IReminderService _reminderService;

        public RemindersController(IReminderService reminderService)
        {
            _reminderService = reminderService;
        }

        [HttpGet("upcoming/{userId}")]
        public async Task<IActionResult> GetUpcomingReminders(Guid userId, [FromQuery] int days = 7)
        {
            var reminders = await _reminderService.GetUpcomingRemindersAsync(userId, days);
            return Ok(reminders);
        }

        [HttpPost("{userId}")]
        public async Task<IActionResult> CreateReminder(Guid userId, [FromBody] Reminder reminder)
        {
            await _reminderService.CreateReminderAsync(userId, reminder);
            return Ok(new { message = "Reminder created successfully" });
        }

        [HttpPost("auto-generate/{userId}")]
        public async Task<IActionResult> AutoGenerateReminders(Guid userId)
        {
            await _reminderService.AutoGenerateRemindersAsync(userId);
            return Ok(new { message = "Reminders auto-generated successfully" });
        }
    }
}
