
namespace TyBudget_backend.Models.DTOs
{
    public class MonthlyAnalyticsDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetSavings { get; set; }
        public decimal SavingsRate { get; set; }
        public decimal AverageDailySpending { get; set; }
        public required Dictionary<string, decimal> ExpensesByCategory { get; set; }
        public required Dictionary<string, decimal> IncomeBySource { get; set; }
        public required List<TopExpenseDto> TopExpenses { get; set; }
        public required BudgetSummaryDto BudgetSummary { get; set; }
        public required GoalProgressDto GoalProgress { get; set; }
    }

    public class TopExpenseDto
    {
        public required string Description { get; set; }
        public required string Category { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
    }

    public class BudgetSummaryDto
    {
        public decimal TotalBudgeted { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal Remaining { get; set; }
        public required List<BudgetItemDto> BudgetItems { get; set; }
    }

    public class BudgetItemDto
    {
        public required string Category { get; set; }
        public decimal Budgeted { get; set; }
        public decimal Spent { get; set; }
        public decimal Percentage { get; set; }
    }

    public class GoalProgressDto
    {
        public int TotalGoals { get; set; }
        public int CompletedGoals { get; set; }
        public int ActiveGoals { get; set; }
        public decimal TotalTargetAmount { get; set; }
        public decimal TotalCurrentAmount { get; set; }
        public required List<GoalItemDto> Goals { get; set; }
    }

    public class GoalItemDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal ProgressPercentage { get; set; }
        public DateTime? TargetDate { get; set; }
        public int DaysRemaining { get; set; }
    }

    
}
