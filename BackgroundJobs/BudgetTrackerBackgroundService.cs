using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BudgetTracker.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BudgetTracker.BackgroundJobs
{
    public class BudgetTrackerBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BudgetTrackerBackgroundService> _logger;
        private Timer _recurringTransactionTimer;
        private Timer _subscriptionTimer;
        private Timer _reminderTimer;

        public BudgetTrackerBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<BudgetTrackerBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Budget Tracker Background Service started");

            // Run daily at midnight
            var now = DateTime.UtcNow;
            var midnight = now.Date.AddDays(1);
            var timeUntilMidnight = midnight - now;

            // Set up recurring transaction processor - runs daily at midnight
            _recurringTransactionTimer = new Timer(
                async _ => await ProcessRecurringTransactions(),
                null,
                timeUntilMidnight,
                TimeSpan.FromDays(1));

            // Set up subscription processor - runs daily at midnight
            _subscriptionTimer = new Timer(
                async _ => await ProcessSubscriptions(),
                null,
                timeUntilMidnight,
                TimeSpan.FromDays(1));

            // Set up reminder processor - runs every hour
            _reminderTimer = new Timer(
                async _ => await ProcessReminders(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromHours(1));

            await Task.CompletedTask;
        }

        private async Task ProcessRecurringTransactions()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider
                    .GetRequiredService<IRecurringTransactionProcessor>();
                await processor.ProcessRecurringTransactionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in recurring transaction processing");
            }
        }

        private async Task ProcessSubscriptions()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider
                    .GetRequiredService<ISubscriptionProcessor>();
                await processor.ProcessSubscriptionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in subscription processing");
            }
        }

        private async Task ProcessReminders()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var reminderService = scope.ServiceProvider
                    .GetRequiredService<IReminderService>();
                await reminderService.ProcessDueRemindersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in reminder processing");
            }
        }

        public override void Dispose()
        {
            _recurringTransactionTimer?.Dispose();
            _subscriptionTimer?.Dispose();
            _reminderTimer?.Dispose();
            base.Dispose();
        }
    }
}
