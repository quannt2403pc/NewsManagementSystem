using Backend2.Models;
using Backend2.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Backend2.Repositories.Class
{
    public class SystemAccountRepository : ISystemAccountRepository
    {

        private readonly Prn232Assignment1Context _context;

        public SystemAccountRepository(Prn232Assignment1Context context)
        {
            _context = context;
        }

        public void AddAccount(SystemAccount account)
        {
            _context.SystemAccounts.Add(account);
            _context.SaveChanges();
        }

        public void DeleteAccount(int accountId)
        {
            var account = _context.SystemAccounts.Find(accountId);
            if (account != null)
            {
                _context.SystemAccounts.Remove(account);
                _context.SaveChanges();
            }
        }

        public SystemAccount GetAccountByEmail(string email)
        {
            return _context.SystemAccounts.FirstOrDefault(a => a.AccountEmail == email);
        }

        public async Task<SystemAccount> GetAccountByEmailAndPasswordAsync(string email, string password)
        {
            return await _context.SystemAccounts
                                 .FirstOrDefaultAsync(a => a.AccountEmail == email && a.AccountPassword == password);
        }

        public SystemAccount GetAccountById(int accountId)
        {
            return _context.SystemAccounts.Find(accountId);
        }

        public IEnumerable<SystemAccount> GetAccounts(string search = null, int? role = null)
        {
            var query = _context.SystemAccounts.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a =>
                    a.AccountName.Contains(search) ||
                    a.AccountEmail.Contains(search)
                );
            }

            if (role.HasValue)
            {
                query = query.Where(a => a.AccountRole == role.Value);
            }

            return query.ToList();
        }

        public bool HasCreatedNewsArticles(int accountId)
        {
            return _context.NewsArticles.Any(na => na.CreatedById == accountId);
        }

        public bool IsEmailExist(string email, int? accountId = null)
        {
            var query = _context.SystemAccounts.Where(a => a.AccountEmail == email);
            if (accountId.HasValue)
            {
                query = query.Where(a => a.AccountId != accountId.Value);
            }
            return query.Any();
        }

        public void UpdateAccount(SystemAccount account)
        {
            _context.SystemAccounts.Update(account);
            _context.SaveChanges();
        }
    }
}
