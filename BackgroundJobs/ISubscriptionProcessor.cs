using System.Threading.Tasks;

namespace BudgetTracker.BackgroundJobs
{
    public interface ISubscriptionProcessor
    {
        Task ProcessSubscriptionsAsync();
    }
}
