using System;
using System.ComponentModel.DataAnnotations;

namespace TyBudget_backend.Models.Entities
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
