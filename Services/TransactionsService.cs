using Microsoft.EntityFrameworkCore;
using TyBudget_backend.Data;
using TyBudget_backend.Models.Entities;

namespace TyBudget_backend.Services
{
    public interface ITransactionsService
    {
        Task<Transaction> CreateTransactionAsync(Transaction Transaction);
        Task<Transaction?> GetTransactionByIdAsync(Guid TransactionId);
        Task<List<Transaction>> GetUserTransactionsAsync(Guid userId);
        Task<bool> UpdateTransactionAsync(Transaction Transaction);
        Task<bool> DeleteTransactionAsync(Guid TransactionId);
    }

    public class TransactionsService : ITransactionsService
    {
        private readonly TyBudget_backendDbContext _context;

        public TransactionsService(TyBudget_backendDbContext context)
        {
            _context = context;
        }

        public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<Transaction?> GetTransactionByIdAsync(Guid transactionId)
        {
            return await _context.Transactions.FindAsync(transactionId);
        }

        public async Task<List<Transaction>> GetUserTransactionsAsync(Guid userId)
        {
            return await _context.Transactions
                .Where(t => t.UserId == userId)
                .ToListAsync();
        }

        public async Task<bool> UpdateTransactionAsync(Transaction transaction)
        {
            _context.Transactions.Update(transaction);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteTransactionAsync(Guid transactionId)
        {
            var transaction = await _context.Transactions.FindAsync(transactionId);
            if (transaction == null) return false;
            _context.Transactions.Remove(transaction);
            return await _context.SaveChangesAsync() > 0;
        }

    }
}