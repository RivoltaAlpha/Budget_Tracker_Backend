using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BudgetTracker.Models
{
    public class Investment
    {
        [Key]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public InvestmentType Type { get; set; }
        public decimal InitialAmount { get; set; }
        public decimal CurrentValue { get; set; }
        public DateTime StartDate { get; set; }
        public decimal? ExpectedReturn { get; set; } // Percentage
        public string Institution { get; set; }
        public InvestmentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public User User { get; set; }

        [NotMapped]
        public decimal ReturnPercentage => InitialAmount > 0
            ? Math.Round(((CurrentValue - InitialAmount) / InitialAmount) * 100, 2)
            : 0;
    }

    public enum InvestmentType
    {
        Stocks,
        Bonds,
        MoneyMarketFund,
        MutualFund,
        RealEstate,
        Cryptocurrency,
        FixedDeposit,
        Other
    }

    public enum InvestmentStatus
    {
        Active,
        Matured,
        Closed
    }
}
