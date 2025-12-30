
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.Models
{
    public class Account
    {
        [Key]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } // e.g., "M-Pesa", "Sacco Savings"
        public AccountType Type { get; set; } // Checking, Savings, Investment, Mobile Money
        public string AccountNumber { get; set; }
        public decimal Balance { get; set; }
        public string Institution { get; set; } // e.g., "KCB Bank", "M-Pesa"
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public User User { get; set; }
        public ICollection<Transaction> Transactions { get; set; }
    }

    public enum AccountType
    {
        Checking,
        Savings,
        MobileMoney,
        Investment,
        CreditCard
    }
}
