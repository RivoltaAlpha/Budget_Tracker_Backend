using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BudgetTracker.Data;
using BudgetTracker.Models;
using System.Text.Json;

namespace BudgetTracker.Services
{
    // DTO for Analytics Response
    public class MonthlyAnalyticsDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetSavings { get; set; }
        public decimal SavingsRate { get; set; }
        public decimal AverageDailySpending { get; set; }
        public required Dictionary<string, decimal> ExpensesByCategory { get; set; }
        public required Dictionary<string, decimal> IncomeBySource { get; set; }
        public required List<TopExpenseDto> TopExpenses { get; set; }
        public required BudgetSummaryDto BudgetSummary { get; set; }
        public required GoalProgressDto GoalProgress { get; set; }
    }

    public class TopExpenseDto
    {
        public required string Description { get; set; }
        public required string Category { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }

    public class BudgetSummaryDto
    {
        public decimal TotalBudgeted { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal Remaining { get; set; }
        public required List<BudgetItemDto> BudgetItems { get; set; }
    }

    public class BudgetItemDto
    {
        public required string Category { get; set; }
        public decimal Budgeted { get; set; }
        public decimal Spent { get; set; }
        public decimal Percentage { get; set; }
    }

    public class GoalProgressDto
    {
        public int TotalGoals { get; set; }
        public int CompletedGoals { get; set; }
        public int ActiveGoals { get; set; }
        public decimal TotalTargetAmount { get; set; }
        public decimal TotalCurrentAmount { get; set; }
        public required List<GoalItemDto> Goals { get; set; }
    }

    public class GoalItemDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal ProgressPercentage { get; set; }
        public DateTime? TargetDate { get; set; }
        public int DaysRemaining { get; set; }
    }

    // Analytics Service
    public interface IAnalyticsService
    {
        Task<MonthlyAnalyticsDto> GetMonthlyAnalyticsAsync(Guid userId, int month, int year);
        Task<AnalyticsSummary?> GenerateAndCacheAnalyticsAsync(Guid userId, int month, int year);
        Task<Dictionary<string, decimal>> GetSpendingTrendAsync(Guid userId, int months);
        Task<Dictionary<string, decimal>> GetCategoryTrendAsync(Guid userId, string category, int months);
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly BudgetTrackerDbContext _context;

        public AnalyticsService(BudgetTrackerDbContext context)
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

    // Reminder Service
    public class ReminderDto
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public ReminderType Type { get; set; }
        public DateTime ReminderDate { get; set; }
        public bool IsUrgent { get; set; }
        public required string RelatedEntityType { get; set; }
        public Guid? RelatedEntityId { get; set; }
    }

    public interface IReminderService
    {
        Task<List<ReminderDto>> GetUpcomingRemindersAsync(Guid userId, int days = 7);
        Task CreateReminderAsync(Guid userId, Reminder reminder);
        Task<bool> ProcessDueRemindersAsync(); // For background job
        Task AutoGenerateRemindersAsync(Guid userId); // Generate reminders based on bills, subscriptions, etc.
    }

    public class ReminderService : IReminderService
    {
        private readonly BudgetTrackerDbContext _context;

        public ReminderService(BudgetTrackerDbContext context)
        {
            _context = context;
        }

        public async Task<List<ReminderDto>> GetUpcomingRemindersAsync(Guid userId, int days = 7)
        {
            var endDate = DateTime.UtcNow.AddDays(days);

            var reminders = await _context.Reminders
                .Where(r => r.UserId == userId &&
                            r.IsActive &&
                            !r.IsSent &&
                            r.ReminderDate <= endDate)
                .OrderBy(r => r.ReminderDate)
                .ToListAsync();

            return reminders.Select(r => new ReminderDto
            {
                Id = r.Id,
                Title = r.Title,
                Description = r.Description,
                Type = r.Type,
                ReminderDate = r.ReminderDate,
                IsUrgent = (r.ReminderDate - DateTime.UtcNow).TotalDays <= 1,
                RelatedEntityType = r.RelatedEntityType,
                RelatedEntityId = r.RelatedEntityId
            }).ToList();
        }

        public async Task CreateReminderAsync(Guid userId, Reminder reminder)
        {
            reminder.Id = Guid.NewGuid();
            reminder.UserId = userId;
            reminder.CreatedAt = DateTime.UtcNow;

            await _context.Reminders.AddAsync(reminder);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ProcessDueRemindersAsync()
        {
            var dueReminders = await _context.Reminders
                .Where(r => r.IsActive &&
                            !r.IsSent &&
                            r.ReminderDate <= DateTime.UtcNow)
                .ToListAsync();

            foreach (var reminder in dueReminders)
            {
                // Here you would send notification (email, push notification, etc.)
                // For now, just mark as sent
                reminder.IsSent = true;

                // If recurring, create next reminder
                if (reminder.IsRecurring && reminder.RecurrenceFrequency.HasValue)
                {
                    var nextReminder = new Reminder
                    {
                        Id = Guid.NewGuid(),
                        UserId = reminder.UserId,
                        Title = reminder.Title,
                        Description = reminder.Description,
                        Type = reminder.Type,
                        IsRecurring = true,
                        RecurrenceFrequency = reminder.RecurrenceFrequency,
                        ReminderDate = CalculateNextReminderDate(reminder.ReminderDate, reminder.RecurrenceFrequency.Value),
                        RelatedEntityId = reminder.RelatedEntityId,
                        RelatedEntityType = reminder.RelatedEntityType,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    await _context.Reminders.AddAsync(nextReminder);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task AutoGenerateRemindersAsync(Guid userId)
        {
            // Generate reminders for upcoming subscriptions
            var upcomingSubscriptions = await _context.Subscriptions
                .Where(s => s.UserId == userId &&
                            s.IsActive &&
                            s.NextBillingDate <= DateTime.UtcNow.AddDays(7))
                .ToListAsync();

            foreach (var subscription in upcomingSubscriptions)
            {
                var existingReminder = await _context.Reminders
                    .AnyAsync(r => r.UserId == userId &&
                                   r.RelatedEntityId == subscription.Id &&
                                   r.RelatedEntityType == "Subscription" &&
                                   !r.IsSent);

                if (!existingReminder)
                {
                    await CreateReminderAsync(userId, new Reminder
                    {
                        Title = $"{subscription.ServiceName} Renewal",
                        Description = $"Your {subscription.ServiceName} subscription renews on {subscription.NextBillingDate:MMM dd}",
                        Type = ReminderType.SubscriptionRenewal,
                        ReminderDate = subscription.NextBillingDate.AddDays(-3),
                        RelatedEntityId = subscription.Id,
                        RelatedEntityType = "Subscription"
                    });
                }
            }

            // Generate reminders for goals with target dates
            var goalsNearDeadline = await _context.Goals
                .Where(g => g.user_id == userId &&
                            g.Status == GoalStatus.Active &&
                            g.TargetDate.HasValue &&
                            g.TargetDate.Value <= DateTime.UtcNow.AddDays(30))
                .ToListAsync();

            foreach (var goal in goalsNearDeadline)
            {
                var existingReminder = await _context.Reminders
                    .AnyAsync(r => r.UserId == userId &&
                                   r.RelatedEntityId == goal.Id &&
                                   r.RelatedEntityType == "Goal" &&
                                   !r.IsSent);

                if (!existingReminder && goal.ProgressPercentage < 100)
                {
                    await CreateReminderAsync(userId, new Reminder
                    {
                        Title = $"Goal Deadline: {goal.Name}",
                        Description = $"Your goal '{goal.Name}' is {goal.ProgressPercentage:F1}% complete. Target date: {goal.TargetDate:MMM dd}",
                        Type = ReminderType.GoalDeadline,
                        ReminderDate = goal.TargetDate?.AddDays(-7) ?? DateTime.UtcNow,
                        RelatedEntityId = goal.Id,
                        RelatedEntityType = "Goal"
                    });
                }
            }

            // Generate reminders for budget limits (over 80%)
            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;

            var budgetsNearLimit = await _context.Budgets
                .Where(b => b.UserId == userId &&
                            b.Month == currentMonth &&
                            b.Year == currentYear &&
                            b.ProgressPercentage >= 80)
                .ToListAsync();

            foreach (var budget in budgetsNearLimit)
            {
                var existingReminder = await _context.Reminders
                    .AnyAsync(r => r.UserId == userId &&
                                   r.Title.Contains(budget.Category) &&
                                   r.Type == ReminderType.BudgetLimit &&
                                   !r.IsSent &&
                                   r.ReminderDate.Month == currentMonth);

                if (!existingReminder)
                {
                    await CreateReminderAsync(userId, new Reminder
                    {
                        Title = $"Budget Alert: {budget.Category}",
                        Description = $"You've used {budget.ProgressPercentage:F1}% of your {budget.Category} budget",
                        Type = ReminderType.BudgetLimit,
                        ReminderDate = DateTime.UtcNow,
                        RelatedEntityId = budget.Id,
                        RelatedEntityType = "Budget"
                    });
                }
            }
        }

        private DateTime CalculateNextReminderDate(DateTime currentDate, RecurrenceFrequency frequency)
        {
            return frequency switch
            {
                RecurrenceFrequency.Daily => currentDate.AddDays(1),
                RecurrenceFrequency.Weekly => currentDate.AddDays(7),
                RecurrenceFrequency.Monthly => currentDate.AddMonths(1),
                RecurrenceFrequency.Quarterly => currentDate.AddMonths(3),
                RecurrenceFrequency.Yearly => currentDate.AddYears(1),
                _ => currentDate.AddMonths(1)
            };
        }
    }
}