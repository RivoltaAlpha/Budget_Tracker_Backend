using Microsoft.EntityFrameworkCore;
using TyBudget_backend.Data;
using TyBudget_backend.Models.Entities;

namespace TyBudget_backend.Services
{
    public interface IBudgetsService
    {
        Task<Budget> CreateBudgetAsync(Budget budget);
        Task<Budget?> GetBudgetByIdAsync(Guid budgetId);
        Task<List<Budget>> GetUserBudgetsAsync(Guid userId);
        Task<bool> UpdateBudgetAsync(Budget budget);
        Task<bool> DeleteBudgetAsync(Guid budgetId);
    }

    public class BudgetsService : IBudgetsService
    {
        private readonly TyBudget_backendDbContext _context;

        public BudgetsService(TyBudget_backendDbContext context)
        {
            _context = context;
        }

        public async Task<Budget> CreateBudgetAsync(Budget budget)
        {
            _context.Budgets.Add(budget);
            await _context.SaveChangesAsync();
            return budget;
        }

        public async Task<Budget?> GetBudgetByIdAsync(Guid budgetId)
        {
            return await _context.Budgets.FindAsync(budgetId);
        }

        public async Task<List<Budget>> GetUserBudgetsAsync(Guid userId)
        {
            return await _context.Budgets
                .Where(b => b.UserId == userId)
                .ToListAsync();
        }

        public async Task<bool> UpdateBudgetAsync(Budget budget)
        {
            _context.Budgets.Update(budget);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteBudgetAsync(Guid budgetId)
        {
            var budget = await _context.Budgets.FindAsync(budgetId);
            if (budget == null) return false;

            _context.Budgets.Remove(budget);
            return await _context.SaveChangesAsync() > 0;
        }

    }
}