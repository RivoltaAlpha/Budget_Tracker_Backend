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
    public class SubscriptionProcessor : ISubscriptionProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SubscriptionProcessor> _logger;

        public SubscriptionProcessor(
            IServiceProvider serviceProvider,
            ILogger<SubscriptionProcessor> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task ProcessSubscriptionsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<BudgetTrackerDbContext>();

            try
            {
                var today = DateTime.UtcNow.Date;

                var dueSubscriptions = await context.Subscriptions
                    .Where(s => s.IsActive && s.NextBillingDate.Date <= today)
                    .Include(s => s.Account)
                    .ToListAsync();

                foreach (var subscription in dueSubscriptions)
                {
                    // Create transaction for subscription payment
                    var transaction = new Transaction
                    {
                        Id = Guid.NewGuid(),
                        UserId = subscription.UserId,
                        AccountId = subscription.AccountId,
                        Type = TransactionType.Expense,
                        Amount = subscription.Amount,
                        Category = "Subscriptions",
                        Description = $"{subscription.ServiceName} subscription",
                        TransactionDate = today,
                        SubscriptionId = subscription.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    await context.Transactions.AddAsync(transaction);

                    // Update account balance
                    subscription.Account.Balance -= subscription.Amount;

                    // Update next billing date
                    subscription.NextBillingDate = CalculateNextBillingDate(
                        subscription.NextBillingDate,
                        subscription.BillingCycle);

                    _logger.LogInformation(
                        $"Processed subscription: {subscription.ServiceName} for user {subscription.UserId}");
                }

                await context.SaveChangesAsync();

                _logger.LogInformation(
                    $"Processed {dueSubscriptions.Count} subscriptions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing subscriptions");
                throw;
            }
        }

        private DateTime CalculateNextBillingDate(DateTime current, SubscriptionBillingCycle cycle)
        {
            return cycle switch
            {
                SubscriptionBillingCycle.Weekly => current.AddDays(7),
                SubscriptionBillingCycle.Monthly => current.AddMonths(1),
                SubscriptionBillingCycle.Quarterly => current.AddMonths(3),
                SubscriptionBillingCycle.Yearly => current.AddYears(1),
                _ => current.AddMonths(1)
            };
        }
    }
}
