using Microsoft.EntityFrameworkCore;
using TyBudget_backend.Data;
using TyBudget_backend.Models.DTOs;
using TyBudget_backend.Models.Entities;
using System.Text.Json;

namespace TyBudget_backend.Services
{
    public interface IAnalyticsService
    {
        Task<MonthlyAnalyticsDto> GetMonthlyAnalyticsAsync(Guid userId, int month, int year);
        Task<AnalyticsSummary?> GenerateAndCacheAnalyticsAsync(Guid userId, int month, int year);
        Task<Dictionary<string, decimal>> GetSpendingTrendAsync(Guid userId, int months);
        Task<Dictionary<string, decimal>> GetCategoryTrendAsync(Guid userId, string category, int months);
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly TyBudget_backendDbContext _context;

        public AnalyticsService(TyBudget_backendDbContext context)
        {
            _context = context;
        }

        public async Task<MonthlyAnalyticsDto> GetMonthlyAnalyticsAsync(Guid userId, int month, int year)
        {
            // Check if we have cached analytics
            var cached = await _context.AnalyticsSummaries
                .FirstOrDefaultAsync(a => a.UserId == userId && a.Month == month && a.Year == year);

            if (cached != null && (DateTime.UtcNow - cached.GeneratedAt).TotalHours < 1)
            {
                // Use cached data if less than 1 hour old
                return await BuildAnalyticsDtoFromCache(userId, cached);
            }

            // Generate fresh analytics
            return await GenerateFreshAnalytics(userId, month, year);
        }

        private async Task<MonthlyAnalyticsDto> GenerateFreshAnalytics(Guid userId, int month, int year)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Get all transactions for the month
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId &&
                            t.TransactionDate >= startDate &&
                            t.TransactionDate <= endDate)
                .ToListAsync();

            var income = transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var expenses = transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            var netSavings = income - expenses;
            var savingsRate = income > 0 ? (netSavings / income) * 100 : 0;

            // Group expenses by category
            var expensesByCategory = transactions
                .Where(t => t.Type == TransactionType.Expense)
                .GroupBy(t => t.Category)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

            // Group income by category (source)
            var incomeBySource = transactions
                .Where(t => t.Type == TransactionType.Income)
                .GroupBy(t => t.Category)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

            // Top expenses
            var topExpenses = transactions
                .Where(t => t.Type == TransactionType.Expense)
                .OrderByDescending(t => t.Amount)
                .Take(10)
                .Select(t => new TopExpenseDto
                {
                    Description = t.Description,
                    Category = t.Category,
                    Amount = t.Amount,
                    Date = t.TransactionDate
                })
                .ToList();

            // Budget summary
            var budgets = await _context.Budgets
                .Where(b => b.UserId == userId && b.Month == month && b.Year == year)
                .ToListAsync();

            var budgetSummary = new BudgetSummaryDto
            {
                TotalBudgeted = budgets.Sum(b => b.Amount),
                TotalSpent = budgets.Sum(b => b.SpentAmount),
                Remaining = budgets.Sum(b => b.RemainingAmount),
                BudgetItems = budgets.Select(b => new BudgetItemDto
                {
                    Category = b.Category,
                    Budgeted = b.Amount,
                    Spent = b.SpentAmount,
                    Percentage = b.ProgressPercentage
                }).ToList()
            };

            // Goal progress
            var goals = await _context.Goals
                .Where(g => g.user_id == userId && g.Status == GoalStatus.Active)
                .ToListAsync();

            var goalProgress = new GoalProgressDto
            {
                TotalGoals = goals.Count,
                ActiveGoals = goals.Count(g => g.Status == GoalStatus.Active),
                CompletedGoals = await _context.Goals
                    .CountAsync(g => g.user_id == userId && g.Status == GoalStatus.Completed),
                TotalTargetAmount = goals.Sum(g => g.TargetAmount),
                TotalCurrentAmount = goals.Sum(g => g.CurrentAmount),
                Goals = goals.Select(g => new GoalItemDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    TargetAmount = g.TargetAmount,
                    CurrentAmount = g.CurrentAmount,
                    ProgressPercentage = g.ProgressPercentage,
                    TargetDate = g.TargetDate,
                    DaysRemaining = g.TargetDate.HasValue
                        ? (int)(g.TargetDate.Value - DateTime.UtcNow).TotalDays
                        : 0
                }).ToList()
            };

            var daysInMonth = DateTime.DaysInMonth(year, month);
            var averageDailySpending = expenses / daysInMonth;

            // Cache the analytics
            await CacheAnalytics(userId, month, year, income, expenses, netSavings,
                savingsRate, averageDailySpending, expensesByCategory);

            return new MonthlyAnalyticsDto
            {
                Month = month,
                Year = year,
                TotalIncome = income,
                TotalExpenses = expenses,
                NetSavings = netSavings,
                SavingsRate = savingsRate,
                AverageDailySpending = averageDailySpending,
                ExpensesByCategory = expensesByCategory,
                IncomeBySource = incomeBySource,
                TopExpenses = topExpenses,
                BudgetSummary = budgetSummary,
                GoalProgress = goalProgress
            };
        }

        private async Task<MonthlyAnalyticsDto> BuildAnalyticsDtoFromCache(Guid userId, AnalyticsSummary cached)
        {
            var expensesByCategory = JsonSerializer.Deserialize<Dictionary<string, decimal>>(
                cached.ExpensesByCategoryJson ?? "{}");

            // Still need to fetch some real-time data
            var budgets = await _context.Budgets
                .Where(b => b.UserId == userId && b.Month == cached.Month && b.Year == cached.Year)
                .ToListAsync();

            var goals = await _context.Goals
                .Where(g => g.user_id == userId && g.Status == GoalStatus.Active)
                .ToListAsync();

            return new MonthlyAnalyticsDto
            {
                Month = cached.Month,
                Year = cached.Year,
                TotalIncome = cached.TotalIncome,
                TotalExpenses = cached.TotalExpenses,
                NetSavings = cached.NetSavings,
                SavingsRate = cached.SavingsRate,
                AverageDailySpending = cached.AverageDailySpending,
                ExpensesByCategory = expensesByCategory ?? new Dictionary<string, decimal>(),
                IncomeBySource = new Dictionary<string, decimal>(),
                TopExpenses = new List<TopExpenseDto>(),
                BudgetSummary = new BudgetSummaryDto
                {
                    TotalBudgeted = budgets.Sum(b => b.Amount),
                    TotalSpent = budgets.Sum(b => b.SpentAmount),
                    Remaining = budgets.Sum(b => b.RemainingAmount),
                    BudgetItems = budgets.Select(b => new BudgetItemDto
                    {
                        Category = b.Category,
                        Budgeted = b.Amount,
                        Spent = b.SpentAmount,
                        Percentage = b.ProgressPercentage
                    }).ToList()
                },
                GoalProgress = new GoalProgressDto
                {
                    TotalGoals = goals.Count,
                    ActiveGoals = goals.Count,
                    TotalTargetAmount = goals.Sum(g => g.TargetAmount),
                    TotalCurrentAmount = goals.Sum(g => g.CurrentAmount),
                    Goals = goals.Select(g => new GoalItemDto
                    {
                        Id = g.Id,
                        Name = g.Name,
                        TargetAmount = g.TargetAmount,
                        CurrentAmount = g.CurrentAmount,
                        ProgressPercentage = g.ProgressPercentage,
                        TargetDate = g.TargetDate,
                        DaysRemaining = g.TargetDate.HasValue
                            ? (int)(g.TargetDate.Value - DateTime.UtcNow).TotalDays
                            : 0
                    }).ToList()
                }
            };
        }

        private async Task CacheAnalytics(Guid userId, int month, int year, decimal income,
            decimal expenses, decimal netSavings, decimal savingsRate, decimal avgDaily,
            Dictionary<string, decimal> expensesByCategory)
        {
            var existing = await _context.AnalyticsSummaries
                .FirstOrDefaultAsync(a => a.UserId == userId && a.Month == month && a.Year == year);

            var summary = existing ?? new AnalyticsSummary
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Month = month,
                Year = year
            };

            summary.TotalIncome = income;
            summary.TotalExpenses = expenses;
            summary.NetSavings = netSavings;
            summary.SavingsRate = savingsRate;
            summary.AverageDailySpending = avgDaily;
            summary.ExpensesByCategoryJson = JsonSerializer.Serialize(expensesByCategory);
            summary.GeneratedAt = DateTime.UtcNow;

            if (existing == null)
                await _context.AnalyticsSummaries.AddAsync(summary);
            else
                _context.AnalyticsSummaries.Update(summary);

            await _context.SaveChangesAsync();
        }

        public async Task<AnalyticsSummary?> GenerateAndCacheAnalyticsAsync(Guid userId, int month, int year)
        {
            await GenerateFreshAnalytics(userId, month, year);
            return await _context.AnalyticsSummaries
                .FirstOrDefaultAsync(a => a.UserId == userId && a.Month == month && a.Year == year);
        }

        public async Task<Dictionary<string, decimal>> GetSpendingTrendAsync(Guid userId, int months)
        {
            var trends = new Dictionary<string, decimal>();
            var currentDate = DateTime.UtcNow;

            for (int i = 0; i < months; i++)
            {
                var targetDate = currentDate.AddMonths(-i);
                var startDate = new DateTime(targetDate.Year, targetDate.Month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var totalExpenses = await _context.Transactions
                    .Where(t => t.UserId == userId &&
                                t.Type == TransactionType.Expense &&
                                t.TransactionDate >= startDate &&
                                t.TransactionDate <= endDate)
                    .SumAsync(t => t.Amount);

                trends[$"{targetDate.Year}-{targetDate.Month:D2}"] = totalExpenses;
            }

            return trends;
        }

        public async Task<Dictionary<string, decimal>> GetCategoryTrendAsync(Guid userId, string category, int months)
        {
            var trends = new Dictionary<string, decimal>();
            var currentDate = DateTime.UtcNow;

            for (int i = 0; i < months; i++)
            {
                var targetDate = currentDate.AddMonths(-i);
                var startDate = new DateTime(targetDate.Year, targetDate.Month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var categoryExpenses = await _context.Transactions
                    .Where(t => t.UserId == userId &&
                                t.Type == TransactionType.Expense &&
                                t.Category == category &&
                                t.TransactionDate >= startDate &&
                                t.TransactionDate <= endDate)
                    .SumAsync(t => t.Amount);

                trends[$"{targetDate.Year}-{targetDate.Month:D2}"] = categoryExpenses;
            }

            return trends;
        }
    }
}