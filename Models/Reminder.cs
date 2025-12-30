using System;
using System.ComponentModel.DataAnnotations;

namespace BudgetTracker.Models
{
    public class Reminder
    {
        [Key]
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public ReminderType Type { get; set; }
        public DateTime ReminderDate { get; set; }
        public bool IsRecurring { get; set; }
        public RecurrenceFrequency? RecurrenceFrequency { get; set; }
        public bool IsSent { get; set; }
        public bool IsActive { get; set; } = true;

        // Reference to related entity
        public Guid? RelatedEntityId { get; set; }
        public string RelatedEntityType { get; set; } // "Subscription", "Goal", "Bill"

        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public User User { get; set; }
    }

    public enum ReminderType
    {
        BillDue,
        SubscriptionRenewal,
        GoalDeadline,
        BudgetLimit,
        Custom
    }
}
