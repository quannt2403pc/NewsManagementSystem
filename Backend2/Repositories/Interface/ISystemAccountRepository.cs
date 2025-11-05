using Backend2.Models;

namespace Backend2.Repositories.Interface
{
    public interface ISystemAccountRepository
    {
        Task<SystemAccount> GetAccountByEmailAndPasswordAsync(string email, string password);
        IEnumerable<SystemAccount> GetAccounts(string search = null, int? role = null);
        SystemAccount GetAccountById(int accountId);
        void AddAccount(SystemAccount account);
        void UpdateAccount(SystemAccount account);
        void DeleteAccount(int accountId);
        bool IsEmailExist(string email, int? accountId = null);
        bool HasCreatedNewsArticles(int accountId);
        SystemAccount GetAccountByEmail(string email);


    }
}

