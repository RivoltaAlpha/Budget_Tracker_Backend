using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using BudgetTracker.Data;
using BudgetTracker.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BudgetTracker.BackgroundJobs
{
    public class RecurringTransactionProcessor : IRecurringTransactionProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RecurringTransactionProcessor> _logger;

        public RecurringTransactionProcessor(
            IServiceProvider serviceProvider,
            ILogger<RecurringTransactionProcessor> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task ProcessRecurringTransactionsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BudgetTrackerDbContext>();

            try
            {
                var today = DateTime.UtcNow.Date;

                // Get all recurring transactions due today
                var dueTransactions = await context.RecurringTransactions
                    .Where(rt => rt.IsActive &&
                                 rt.NextOccurrence.HasValue &&
                                 rt.NextOccurrence.Value.Date <= today)
                    .Include(rt => rt.Account)
                    .ToListAsync();

                foreach (var recurring in dueTransactions)
                {
                    // Create the transaction
                    var transaction = new Transaction
                    {
                        Id = Guid.NewGuid(),
                        UserId = recurring.UserId,
                        AccountId = recurring.AccountId,
                        Type = recurring.Type,
                        Amount = recurring.Amount,
                        Category = recurring.Category,
                        Description = $"{recurring.Name} (Auto-generated)",
                        TransactionDate = today,
                        RecurringTransactionId = recurring.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    await context.Transactions.AddAsync(transaction);

                    // Update account balance
                    if (recurring.Type == TransactionType.Expense)
                    {
                        recurring.Account.Balance -= recurring.Amount;
                    }
                    else if (recurring.Type == TransactionType.Income)
                    {
                        recurring.Account.Balance += recurring.Amount;
                    }

                    // Update budget if applicable
                    if (recurring.Type == TransactionType.Expense)
                    {
                        var budget = await context.Budgets
                            .FirstOrDefaultAsync(b => b.UserId == recurring.UserId &&
                                                     b.Category == recurring.Category &&
                                                     b.Month == today.Month &&
                                                     b.Year == today.Year);

                        if (budget != null)
                        {
                            budget.SpentAmount += recurring.Amount;
                            budget.UpdatedAt = DateTime.UtcNow;
                        }
                    }

                    // Calculate next occurrence
                    recurring.NextOccurrence = CalculateNextOccurrence(
                        recurring.NextOccurrence.Value,
                        recurring.Frequency);

                    // Check if we've passed the end date
                    if (recurring.EndDate.HasValue &&
                        recurring.NextOccurrence > recurring.EndDate)
                    {
                        recurring.IsActive = false;
                    }

                    _logger.LogInformation(
                        $"Processed recurring transaction: {recurring.Name} for user {recurring.UserId}");
                }

                await context.SaveChangesAsync();

                _logger.LogInformation(
                    $"Processed {dueTransactions.Count} recurring transactions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recurring transactions");
                throw;
            }
        }

        private DateTime CalculateNextOccurrence(DateTime current, RecurrenceFrequency frequency)
        {
            return frequency switch
            {
                RecurrenceFrequency.Daily => current.AddDays(1),
                RecurrenceFrequency.Weekly => current.AddDays(7),
                RecurrenceFrequency.Monthly => current.AddMonths(1),
                RecurrenceFrequency.Quarterly => current.AddMonths(3),
                RecurrenceFrequency.Yearly => current.AddYears(1),
                _ => current.AddMonths(1)
            };
        }
    }
}
