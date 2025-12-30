using System.Threading.Tasks;

namespace BudgetTracker.BackgroundJobs
{
    public interface IRecurringTransactionProcessor
    {
        Task ProcessRecurringTransactionsAsync();
    }
}
