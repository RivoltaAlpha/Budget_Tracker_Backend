using System.Threading.Tasks;

namespace TyBudget_backend.BackgroundJobs
{
    public interface ISubscriptionProcessor
    {
        Task ProcessSubscriptionsAsync();
    }
}
