using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.Models
{
    public class Subscription
    {
        [Key]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid AccountId { get; set; }
        public string ServiceName { get; set; } // "Netflix", "Spotify"
        public decimal Amount { get; set; }
        public SubscriptionBillingCycle BillingCycle { get; set; }
        public DateTime NextBillingDate { get; set; }
        public DateTime? CancellationDate { get; set; }
        public bool IsActive { get; set; } = true;
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public User User { get; set; }
        public Account Account { get; set; }
        public ICollection<Transaction> Transactions { get; set; }
    }

    public enum SubscriptionBillingCycle
    {
        Weekly,
        Monthly,
        Quarterly,
        Yearly
    }
}
