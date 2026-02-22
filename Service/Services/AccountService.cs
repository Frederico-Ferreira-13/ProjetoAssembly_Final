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
                    Error.Unauthorized(
                        ErrorCodes.AuthUnauthorized, 
                        "Nenhum utilizador autenticado encontrado."));
            }

            int currentUserId = currentUserIdResult.Value;
            return await GetAccountsByUserIdAsync(currentUserId);
        }

        public async Task<Result<Account>> CreateAccountAsync(string accountName, string? subscriptionLevel = null)
        {
            var currentUserIdResult = await _authService.GetCurrentUserIdAsync();
            if (!currentUserIdResult.IsSuccessful)
            {
                return Result<Account>.Failure(
                    Error.Unauthorized(
                        ErrorCodes.AuthUnauthorized, 
                        "Nenhum utilizador autenticado encontrado para criar a conta."));
            }

            int currentUserId = currentUserIdResult.Value;

            var existingAccount = await _unitOfWork.Accounts.AccountNameExistsAsync(accountName);
            if (existingAccount)
            {
                return Result<Account>.Failure(
                    Error.Conflict(
                        ErrorCodes.AlreadyExists, 
                        $"Já existe uma conta com o nome '{accountName}'."));
            }

            await _unitOfWork.BeginTransactionAsync(); //Início da transação

            try
            {
                var newAccount = new Account(
                    accountName: accountName,
                    subscriptionLevel: subscriptionLevel ?? "Free",
                    creatorUserId: currentUserId
                );

                await _unitOfWork.Accounts.CreateAddAsync(newAccount);
                await _unitOfWork.CommitAsync(); //Commit se estiver tudo OK
                
                return Result<Account>.Success(newAccount);
            }
            catch (ArgumentException ex)
            {
                _unitOfWork.Rollback(); //Rollback se der erro de validação

                string fieldName = ex.ParamName ?? "Geral";
                return Result<Account>.Failure(
                    Error.Validation(
                    "Dados de entrada inválidos para a conta",
                    new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } }
                    )
                );
            }
            catch(Exception ex)
            {
                _unitOfWork.Rollback();

                return Result<Account>.Failure(
                    Error.InternalServer($"Erro inesperado ao criar conta: {ex.Message}"));
            }

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

            await _unitOfWork.BeginTransactionAsync();

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
                _unitOfWork.Rollback();

                string fieldName = ex.ParamName ?? "Geral";
                return Result<Account>.Failure(
                    Error.Validation(
                    "Dados de entrada inválidos para a atualização da conta.",
                    new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } }
                    )
                );
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result<Account>.Failure(
                    Error.InternalServer($"Erro ao atualizar conta: {ex.Message}"));
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

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                accountToDeactivate.Deactivate();
                await _unitOfWork.Accounts.UpdateAsync(accountToDeactivate);
                await _unitOfWork.CommitAsync();

                return Result<Account>.Success(accountToDeactivate);
            }
            catch (Exception ex)
            {
                _unitOfWork.Rollback();
                return Result<Account>.Failure(
                    Error.InternalServer($"Erro ao desativar conta: {ex.Message}"));
            }            
        }
    }
}
