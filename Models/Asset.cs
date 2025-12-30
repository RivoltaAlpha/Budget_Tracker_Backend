using System;
using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.Models
{
    public class Asset
    {
        [Key]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public AssetType Type { get; set; }
        public decimal PurchaseValue { get; set; }
        public decimal CurrentValue { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string Description { get; set; }
        public bool IsLiquid { get; set; } // Can be quickly converted to cash
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public User User { get; set; }
    }

    public enum AssetType
    {
        RealEstate,
        Vehicle,
        Investment,
        Jewelry,
        Electronics,
        Other
    }
}
