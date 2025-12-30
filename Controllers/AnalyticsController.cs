using Microsoft.AspNetCore.Mvc;
using BudgetTracker.Services;
using BudgetTracker.Models;
using System;
using System.Threading.Tasks;

namespace BudgetTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [HttpGet("monthly/{userId}")]
        public async Task<IActionResult> GetMonthlyAnalytics(
            Guid userId,
            [FromQuery] int? month = null,
            [FromQuery] int? year = null)
        {
            var targetMonth = month ?? DateTime.UtcNow.Month;
            var targetYear = year ?? DateTime.UtcNow.Year;

            var analytics = await _analyticsService.GetMonthlyAnalyticsAsync(userId, targetMonth, targetYear);
            return Ok(analytics);
        }

        [HttpGet("spending-trend/{userId}")]
        public async Task<IActionResult> GetSpendingTrend(Guid userId, [FromQuery] int months = 6)
        {
            var trend = await _analyticsService.GetSpendingTrendAsync(userId, months);
            return Ok(trend);
        }

        [HttpGet("category-trend/{userId}")]
        public async Task<IActionResult> GetCategoryTrend(
            Guid userId,
            [FromQuery] string category,
            [FromQuery] int months = 6)
        {
            var trend = await _analyticsService.GetCategoryTrendAsync(userId, category, months);
            return Ok(trend);
        }

        [HttpPost("regenerate/{userId}")]
        public async Task<IActionResult> RegenerateAnalytics(
            Guid userId,
            [FromQuery] int month,
            [FromQuery] int year)
        {
            var summary = await _analyticsService.GenerateAndCacheAnalyticsAsync(userId, month, year);
            return Ok(new { message = "Analytics regenerated successfully", summary });
        }
    }
}
