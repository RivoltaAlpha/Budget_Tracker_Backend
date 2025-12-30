using System;
using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.Models
{
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid AccountId { get; set; }
        public TransactionType Type { get; set; } // Income, Expense, Transfer
        public decimal Amount { get; set; }
        public string Category { get; set; } // "Salary", "Rent", "Shopping", etc.
        public string Description { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime CreatedAt { get; set; }

        // Link to recurring transaction if applicable
        public Guid? RecurringTransactionId { get; set; }

        // Link to subscription if applicable
        public Guid? SubscriptionId { get; set; }

        // For transfers between accounts
        public Guid? ToAccountId { get; set; }

        // Navigation Properties
        public User User { get; set; }
        public Account Account { get; set; }
        public Account ToAccount { get; set; }
        public RecurringTransaction RecurringTransaction { get; set; }
        public Subscription Subscription { get; set; }
    }

    public enum TransactionType
    {
        Income,
        Expense,
        Transfer
    }
}
