
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TyBudget_backend.Models.Entities
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string Currency { get; set; } = "KES";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public ICollection<Account> Accounts { get; set; } // many accounts
        public ICollection<Transaction> Transactions { get; set; } // many transactions
        public ICollection<Goal> Goals { get; set; } // many goals
        public ICollection<Budget> Budgets { get; set; } // many budgets
    }
}