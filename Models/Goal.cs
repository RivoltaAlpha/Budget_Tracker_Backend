using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTracker.Models
{
    public class Goal
    {
        [Key]
        public Guid Id { get; set; }
        public Guid user_id { get; set; }
        public string Name { get; set; } // "Holiday Trip", "Emergency Fund"
        public GoalCategory Category { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public DateTime? TargetDate { get; set; }
        public GoalStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public User User { get; set; }
        public ICollection<GoalContribution> Contributions { get; set; }

        [NotMapped]
        public decimal ProgressPercentage => TargetAmount > 0
            ? Math.Round((CurrentAmount / TargetAmount) * 100, 2)
            : 0;
    }

    public enum GoalCategory
    {
        Emergency,
        Personal,
        Investment,
        Education,
        Travel,
        Purchase,
        Other
    }

    public enum GoalStatus
    {
        Active,
        Completed,
        Paused,
        Cancelled
    }
}
