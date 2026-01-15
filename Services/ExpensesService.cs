using Microsoft.EntityFrameworkCore;
using TyBudget_backend.Data;
using TyBudget_backend.Models.Entities;

namespace TyBudget_backend.Services
{
    public interface IExpensesService
    {
        Task<Expense> CreateExpenseAsync(Expense expense);
        Task<Expense?> GetExpenseByIdAsync(Guid expenseId);
        Task<List<Expense>> GetUserExpensesAsync(Guid userId);
        Task<bool> UpdateExpenseAsync(Expense expense);
        Task<bool> DeleteExpenseAsync(Guid expenseId);
    }

    public class ExpensesService : IExpensesService
    {
        private readonly TyBudget_backendDbContext _context;

        public ExpensesService(TyBudget_backendDbContext context)
        {
            _context = context;
        }

        public async Task<Expense> CreateExpenseAsync(Expense expense)
        {
            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();
            return expense;
        }

        public async Task<Expense?> GetExpenseByIdAsync(Guid expenseId)
        {
            return await _context.Expenses.FindAsync(expenseId);
        }

        public async Task<List<Expense>> GetUserExpensesAsync(Guid userId)
        {
            return await _context.Expenses
                .Where(e => e.UserId == userId)
                .ToListAsync();
        }

        public async Task<bool> UpdateExpenseAsync(Expense expense)
        {
            _context.Expenses.Update(expense);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteExpenseAsync(Guid expenseId)
        {
            var expense = await _context.Expenses.FindAsync(expenseId);
            if (expense == null) return false;

            _context.Expenses.Remove(expense);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}