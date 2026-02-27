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
            return await GetAccountsByUserIdAsync(userId);
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
            if (string.IsNullOrWhiteSpace(accountName))
            {
                return Result<Account>.Failure(
                    Error.Validation(
                        "O nome da conta é obrigatório.",
                        new Dictionary<string, string[]>
                        {
                            { nameof(accountName), new[] { "Campo obrigatório" } }
                        }
                    )
                );
            }

            if(accountName.Length > 255)
            {
                return Result<Account>.Failure(
                    Error.Validation(
                        "O nome da conta não pode exceder 255 caracteres.",
                        new Dictionary<string, string[]>
                        {
                            { nameof(accountName), new[] { "Máximo 255 caracteres" } }
                        }
                    )
                );
            }

            if (!string.IsNullOrWhiteSpace(subscriptionLevel) &&
                subscriptionLevel != "Free" && subscriptionLevel != "Premium" && subscriptionLevel != "Enterprise")
            {
                return Result<Account>.Failure(
                    Error.Validation(
                        "Nível de subscrição inválido.",
                        new Dictionary<string, string[]>
                        {
                            { nameof(subscriptionLevel), new[] { "Valores permitidos: Free, Premium, Enterprise" } }
                        }
                    )
                );
            }

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
            if (await _unitOfWork.Accounts.AccountNameExistsAsync(accountName))
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
            if(updateAccount == null)
            {
                return Result<Account>.Failure(
                    Error.Validation(
                        "Conta inválida para atualização.")
                );
            }

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
                if (await _unitOfWork.Accounts.AccountNameExistsAsync(updateAccount.AccountName, updateAccount.AccountId))
                {
                    return Result<Account>.Failure(
                        Error.Conflict(ErrorCodes.AlreadyExists, $"Já existe outra conta com o nome '{updateAccount.AccountName}'.")
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

                return Result<Account>.Failure(
                    Error.Validation(
                        "Dados inválidos para atualizar a conta.", 
                        new Dictionary<string, string[]> { { ex.ParamName ?? "Geral", new[] { ex.Message } } })
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
            var account = await _unitOfWork.Accounts.ReadByIdAsync(accountId);
            if (account == null)
            {
                return Result<Account>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Conta com ID {accountId} não encontrada para desativação.")
                );
            }

            if (!account.IsActive)
            {                
                return Result<Account>.Success(account, "A conta já se encontra desativada.");
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                account.Deactivate();
                await _unitOfWork.Accounts.UpdateAsync(account);
                await _unitOfWork.CommitAsync();

                return Result<Account>.Success(account);
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
