using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.Models
{
    public class RecurringTransaction
    {
        [Key]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid AccountId { get; set; }
        public string Name { get; set; } // e.g., "Monthly Rent"
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; }
        public RecurrenceFrequency Frequency { get; set; }
        public int DayOfMonth { get; set; } // For monthly: 1-31
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? NextOccurrence { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public User User { get; set; }
        public Account Account { get; set; }
        public ICollection<Transaction> GeneratedTransactions { get; set; }
    }

    public enum RecurrenceFrequency
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Yearly
    }
}
