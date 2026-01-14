using Core.Common;
using Core.Model;
using Contracts.Service;
using Contracts.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Services
{
    public class AccountService : IAccountService
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthenticationService _authService;        

        public AccountService(IUnitOfWork unitOfWork, IAuthenticationService authService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));            
        }

        public async Task<Result<Account>> CreateAccountAsync(Account account)
        {
            var currentUserIdResult = await _authService.GetCurrentUserIdAsync();

            if (!currentUserIdResult.IsSuccessful)
            {
                return Result<Account>.Failure(
                    Error.Unauthorized(
                    currentUserIdResult.ErrorCode ?? ErrorCodes.AuthUnauthorized,
                    currentUserIdResult.Message ?? "Nenhum utilizador autenticado encontrado para criar a conta.")
                );
            }

            int currentUserId = currentUserIdResult.Value;

            var existingAccount = await _unitOfWork.Accounts.GetByNameAsync(account.AccountName);
            if (existingAccount != null)
            {
                return Result<Account>.Failure(
                    Error.Conflict(
                        ErrorCodes.AlreadyExists,
                        $"Já existe uma conta ativa com o Nome '{account.AccountName}'.",
                        new Dictionary<string, string[]> { { nameof(account.AccountName), new[] { "O nome da conta já está em uso." } } }
                    )
                );
            }

            try
            {
                var newAccount = new Account(
                accountName: account.AccountName,
                subscriptionLevel: account.SubscriptionLevel,
                userId: currentUserId
            );

                await _unitOfWork.Accounts.CreateAddAsync(newAccount);
                await _unitOfWork.CommitAsync();
                
                return Result<Account>.Success(newAccount);
            }
            catch (ArgumentException ex)
            {
                string fieldName = ex.ParamName ?? "Geral";

                return Result<Account>.Failure(
                    Error.Validation(
                    "Dados de entrada inválidos para a conta",
                    new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } }
                    )
                );
            }

        }

        public async Task<bool> ExistsByIdAsync(int accountId)
        {
            return await _unitOfWork.Accounts.ReadByIdAsync(accountId) != null;
        }

        public async Task<Result<Account>> GetAccountByIdAsync(int id)
        {
            var account = await _unitOfWork.Accounts.ReadByIdAsync(id);

            if (account == null)
            {
                // Devolve NotFound
                return Result<Account>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Conta com ID {id} não encontrada.")
                );
            }

            
            return Result<Account>.Success(account);
        }

        public async Task<Result<IEnumerable<Account>>> GetAccountsByUserIdAsync(int userId)
        {
            var userExists = await _unitOfWork.Users.ReadByIdAsync(userId) != null;
            if (!userExists)
            {
                return Result<IEnumerable<Account>>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"O utilizador com ID {userId} não existe.")
                );
            }

            var accounts = await _unitOfWork.Accounts.GetAccountsByUserIdAsync(userId);            
            return Result<IEnumerable<Account>>.Success(accounts);
        }

        public async Task<Result<IEnumerable<Account>>> GetUserActiveAccountsAsync(int userId)
        {
            var userExists = await _unitOfWork.Users.ReadByIdAsync(userId) != null;
            if (!userExists)
            {
                return Result<IEnumerable<Account>>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"O utilizador com ID {userId} não existe.")
                );
            }

            var accounts = await _unitOfWork.Accounts.GetUserActiveAccountsAsync(userId);            
            return Result<IEnumerable<Account>>.Success(accounts);
        }

        public async Task<Result<IEnumerable<Account>>> GetCurrentUserAccountsAsync()
        {
            var currentUserIdResult = await _authService.GetCurrentUserIdAsync();

            if (!currentUserIdResult.IsSuccessful)
            {
                return Result<IEnumerable<Account>>.Failure(
                    Error.Conflict(
                    currentUserIdResult.ErrorCode ?? ErrorCodes.AuthUnauthorized,
                    currentUserIdResult.Message ?? "Nenhum utilizador autenticado encontrado.")
                );
            }

            int currentUserId = currentUserIdResult.Value;

            return await GetAccountsByUserIdAsync(currentUserId);
        }

        public async Task<Result<Account>> UpdateAccountAsync(Account updateAccount)
        {
            var existingAccount = await _unitOfWork.Accounts.ReadByIdAsync(updateAccount.AccountId);

            if (existingAccount == null)
            {
                return Result<Account>.Failure(
                    Error.NotFound(
                    Core.Common.ErrorCodes.NotFound,
                    $"Conta com ID {updateAccount.AccountId} não encontrada para atualização.")
                );
            }

            if (!string.Equals(existingAccount.AccountName, updateAccount.AccountName, StringComparison.OrdinalIgnoreCase))
            {
                var nameExists = await _unitOfWork.Accounts.AccountNameExistsAsync(updateAccount.AccountName, updateAccount.AccountId);

                if (nameExists)
                {
                    return Result<Account>.Failure(
                        Error.Conflict(
                            ErrorCodes.AlreadyExists,
                            $"Já existe outra conta ativa com o Nome '{updateAccount.AccountName}'.",
                            new Dictionary<string, string[]> { { nameof(updateAccount.AccountName), new[] { "O nome da conta já está em uso por outra conta." } } }
                        )
                    );
                }
            }

            try
            {
                existingAccount.UpdateDetails(
                newAccountName: updateAccount.AccountName,
                newSubscriptionLevel: updateAccount.SubscriptionLevel
            );

                await _unitOfWork.Accounts.UpdateAsync(existingAccount);
                await _unitOfWork.CommitAsync();
               
                return Result<Account>.Success(existingAccount);
            }
            catch (ArgumentException ex)
            {
                string fieldName = ex.ParamName ?? "Geral";

                return Result<Account>.Failure(
                    Error.Validation(
                    "Dados de entrada inválidos para a atualização da conta.",
                    new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } }
                    )
                );
            }

        }

        public async Task<Result<Account>> DesactivateAccountAsync(int accountId)
        {
            var accountToDeactivate = await _unitOfWork.Accounts.ReadByIdAsync(accountId);

            if (accountToDeactivate == null)
            {
                return Result<Account>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Conta com ID {accountId} não encontrada para desativação.")
                );
            }

            if (!accountToDeactivate.IsActive)
            {                
                return Result<Account>.Success(accountToDeactivate, "A conta já se encontra desativada.");
            }

            accountToDeactivate.Deactive();

            await _unitOfWork.Accounts.UpdateAsync(accountToDeactivate);
            await _unitOfWork.CommitAsync();
            
            return Result<Account>.Success(accountToDeactivate);
        }
    }
}
