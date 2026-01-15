using Microsoft.EntityFrameworkCore;
using TyBudget_backend.Data;
using TyBudget_backend.Models.Entities;

namespace TyBudget_backend.Services
{
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
        private readonly TyBudget_backendDbContext _context;

        public ReminderService(TyBudget_backendDbContext context)
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