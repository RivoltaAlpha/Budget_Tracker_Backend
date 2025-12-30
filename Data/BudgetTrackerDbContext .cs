using Microsoft.EntityFrameworkCore;
using BudgetTracker.Models;

namespace BudgetTracker.Data
{
    public class BudgetTrackerDbContext : DbContext
    {
        public BudgetTrackerDbContext(DbContextOptions<BudgetTrackerDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Income> Incomes { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<RecurringTransaction> RecurringTransactions { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Goal> Goals { get; set; }
        public DbSet<GoalContribution> GoalContributions { get; set; }
        public DbSet<Budget> Budgets { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Investment> Investments { get; set; }
        public DbSet<Reminder> Reminders { get; set; }
        public DbSet<AnalyticsSummary> AnalyticsSummaries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User relationships
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            });

            // Configure Account
            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Accounts)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Balance).HasPrecision(18, 2);
            });

            // Configure Transaction
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Transactions)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Account)
                    .WithMany(a => a.Transactions)
                    .HasForeignKey(e => e.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);

                // For transfers
                entity.HasOne(e => e.ToAccount)
                    .WithMany()
                    .HasForeignKey(e => e.ToAccountId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.RecurringTransaction)
                    .WithMany(rt => rt.GeneratedTransactions)
                    .HasForeignKey(e => e.RecurringTransactionId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Subscription)
                    .WithMany(s => s.Transactions)
                    .HasForeignKey(e => e.SubscriptionId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.HasIndex(e => new { e.UserId, e.TransactionDate });
            });

            // Configure Income
            modelBuilder.Entity<Income>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Amount).HasPrecision(18, 2);
            });

            // Configure Expense
            modelBuilder.Entity<Expense>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Transaction)
                    .WithOne()
                    .HasForeignKey<Expense>(e => e.TransactionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure RecurringTransaction
            modelBuilder.Entity<RecurringTransaction>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Account)
                    .WithMany()
                    .HasForeignKey(e => e.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Amount).HasPrecision(18, 2);
            });

            // Configure Subscription
            modelBuilder.Entity<Subscription>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Account)
                    .WithMany()
                    .HasForeignKey(e => e.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Amount).HasPrecision(18, 2);
            });

            // Configure Goal
            modelBuilder.Entity<Goal>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Goals)
                    .HasForeignKey(e => e.user_id)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.TargetAmount).HasPrecision(18, 2);
                entity.Property(e => e.CurrentAmount).HasPrecision(18, 2);
            });

            // Configure GoalContribution
            modelBuilder.Entity<GoalContribution>(entity =>
            {
                entity.HasOne(e => e.Goal)
                    .WithMany(g => g.Contributions)
                    .HasForeignKey(e => e.GoalId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Transaction)
                    .WithOne()
                    .HasForeignKey<GoalContribution>(e => e.TransactionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Amount).HasPrecision(18, 2);
            });

            // Configure Budget
            modelBuilder.Entity<Budget>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Budgets)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.SpentAmount).HasPrecision(18, 2);

                entity.HasIndex(e => new { e.UserId, e.Month, e.Year, e.Category })
                    .IsUnique();
            });

            // Configure Asset
            modelBuilder.Entity<Asset>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.PurchaseValue).HasPrecision(18, 2);
                entity.Property(e => e.CurrentValue).HasPrecision(18, 2);
            });

            // Configure Investment
            modelBuilder.Entity<Investment>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.InitialAmount).HasPrecision(18, 2);
                entity.Property(e => e.CurrentValue).HasPrecision(18, 2);
                entity.Property(e => e.ExpectedReturn).HasPrecision(5, 2);
            });

            // Configure Reminder
            modelBuilder.Entity<Reminder>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.UserId, e.ReminderDate, e.IsSent });
            });

            // Configure AnalyticsSummary
            modelBuilder.Entity<AnalyticsSummary>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.UserId, e.Month, e.Year }).IsUnique();

                entity.Property(e => e.TotalIncome).HasPrecision(18, 2);
                entity.Property(e => e.TotalExpenses).HasPrecision(18, 2);
                entity.Property(e => e.NetSavings).HasPrecision(18, 2);
                entity.Property(e => e.SavingsRate).HasPrecision(5, 2);
            });
        }
    }
}

// Generic Repository Interface
namespace BudgetTracker.Repositories
{
    using System.Linq.Expressions;
    using BudgetTracker.Data;

    public interface IRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T> AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }

    // Generic Repository Implementation
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly BudgetTrackerDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(BudgetTrackerDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public virtual async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public virtual async Task DeleteAsync(Guid id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public virtual async Task<bool> ExistsAsync(Guid id)
        {
            return await _dbSet.FindAsync(id) != null;
        }
    }
}