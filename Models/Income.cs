using System;
using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.Models
{
    public class Income
    {
        [Key]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Source { get; set; } // "Salary", "Freelance", "Business"
        public decimal Amount { get; set; }
        public IncomeFrequency Frequency { get; set; }
        public DateTime? NextExpectedDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public User User { get; set; }
    }

    public enum IncomeFrequency
    {
        OneTime,
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Yearly
    }
}
