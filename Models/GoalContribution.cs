using System;
using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.Models
{
    public class GoalContribution
    {
        [Key]
        public Guid Id { get; set; }
        public Guid GoalId { get; set; }
        public Guid TransactionId { get; set; }
        public decimal Amount { get; set; }
        public DateTime ContributionDate { get; set; }

        // Navigation Properties
        public Goal Goal { get; set; }
        public Transaction Transaction { get; set; }
    }
}
