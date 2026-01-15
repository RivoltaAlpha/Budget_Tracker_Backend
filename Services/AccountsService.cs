using Microsoft.EntityFrameworkCore;
using TyBudget_backend.Data;
using TyBudget_backend.Models.Entities;

namespace TyBudget_backend.Services
{
    public interface IAccountsService
    {
        Task<Account> CreateAccountAsync(Account account);
        Task<Account?> GetAccountByIdAsync(Guid accountId);
        Task<List<Account>> GetUserAccountsAsync(Guid userId);
        Task<bool> UpdateAccountAsync(Account account);
        Task<bool> DeleteAccountAsync(Guid accountId);
    }

    public class AccountsService : IAccountsService
    {
        private readonly TyBudget_backendDbContext _context;

        public AccountsService(TyBudget_backendDbContext context)
        {
            _context = context;
        }

        public async Task<Account> CreateAccountAsync(Account account)
        {
            account.Id = Guid.NewGuid();
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            return account;
        }

        public async Task<Account?> GetAccountByIdAsync(Guid accountId)
        {
            return await _context.Accounts.FindAsync(accountId);
        }

        public async Task<List<Account>> GetUserAccountsAsync(Guid userId)
        {
            return await _context.Accounts
                .Where(a => a.UserId == userId)
                .ToListAsync();
        }

        public async Task<bool> UpdateAccountAsync(Account account)
        {
            var existingAccount = await _context.Accounts.FindAsync(account.Id);
            if (existingAccount == null)
            {
                return false;
            }

            existingAccount.Name = account.Name;
            existingAccount.Balance = account.Balance;
            existingAccount.Institution = account.Institution;

            _context.Accounts.Update(existingAccount);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAccountAsync(Guid accountId)
        {
            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null)
            {
                return false;
            }

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();
            return true;
        }


    }
}