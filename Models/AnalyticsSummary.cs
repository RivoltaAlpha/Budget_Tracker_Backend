using System;
using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.Models
{
    public class AnalyticsSummary
    {
        [Key]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }

        // Income metrics
        public decimal TotalIncome { get; set; }
        public decimal RecurringIncome { get; set; }

        // Expense metrics
        public decimal TotalExpenses { get; set; }
        public decimal RecurringExpenses { get; set; }

        // Category breakdown (stored as JSON)
        public string ExpensesByCategoryJson { get; set; }

        // Other metrics
        public decimal NetSavings { get; set; }
        public decimal SavingsRate { get; set; } // Percentage
        public decimal AverageDailySpending { get; set; }

        public DateTime GeneratedAt { get; set; }

        // Navigation Properties
        public User User { get; set; }
    }
}
