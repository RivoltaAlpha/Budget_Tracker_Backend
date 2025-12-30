using System;
using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.Models
{
    public class Expense
    {
        [Key]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid TransactionId { get; set; }
        public ExpenseCategory Category { get; set; }
        public string SubCategory { get; set; }
        public bool IsRecurring { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public User User { get; set; }
        public Transaction Transaction { get; set; }
    }

    public enum ExpenseCategory
    {
        Rent,
        Shopping,
        FoodAndGroceries,
        Transport,
        Utilities,
        Entertainment,
        Healthcare,
        Education,
        Other
    }
}
