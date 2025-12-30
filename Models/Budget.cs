using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTracker.Models
{
    public class Budget
    {
        [Key]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Category { get; set; } // "Shopping", "Food", "Entertainment"
        public decimal Amount { get; set; }
        public decimal SpentAmount { get; set; }
        public int Month { get; set; } // 1-12
        public int Year { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public User User { get; set; }

        [NotMapped]
        public decimal RemainingAmount => Amount - SpentAmount;

        [NotMapped]
        public decimal ProgressPercentage => Amount > 0
            ? Math.Round((SpentAmount / Amount) * 100, 2)
            : 0;
    }
}
