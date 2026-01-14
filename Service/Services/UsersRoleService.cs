using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Services
{
    public class UsersRoleService : IUsersRoleService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UsersRoleService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<UsersRole>> CreateUsersRoleAsync(UsersRole dto)
        {
            if (await _unitOfWork.UsersRole.GetByNameAsync(dto.RoleName) != null)
            {
                return Result<UsersRole>.Failure(
                    Error.Validation(
                    $"O Nível de Acesso '{dto.RoleName}' já existe.",
                    new Dictionary<string, string[]> { { nameof(dto.RoleName), new[] { "Nome já em uso." } } })
                );
            }

            var newUserRole = new UsersRole(dto.RoleName);

            await _unitOfWork.UsersRole.CreateAddAsync(newUserRole);
            await _unitOfWork.CommitAsync();

            return Result<UsersRole>.Success(newUserRole);
        }

        public async Task<Result<UsersRole>> GetUsersRoleByIdAsync(int id)
        {
            var usersRole = await _unitOfWork.UsersRole.ReadByIdAsync(id);

            if (usersRole == null)
            {
                return Result<UsersRole>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Nível de Acesso com ID {id} não encontrado.")
                );
            }

            return Result<UsersRole>.Success(usersRole);
        }

        public async Task<Result<UsersRole>> GetUsersRoleByNameAsync(string name)
        {
            var usersRole = await _unitOfWork.UsersRole.GetByNameAsync(name);

            if (usersRole == null)
            {
                return Result<UsersRole>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Nível de Acesso com nome '{name}' não encontrado.")
                );
            }

            return Result<UsersRole>.Success(usersRole);
        }

        public async Task<Result<IEnumerable<UsersRole>>> GetAllUsersRolesAsync()
        {
            var usersRoles = await _unitOfWork.UsersRole.ReadAllAsync();           

            return Result<IEnumerable<UsersRole>>.Success(usersRoles);
        }

        public async Task<Result> UpdateUsersRoleAsync(UsersRole updateUserRole)
        {
            var existingRole = await _unitOfWork.UsersRole.ReadByIdAsync(updateUserRole.UsersRoleId);

            if (existingRole == null)
            {
                return Result.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Nível de Acesso com ID {updateUserRole.UsersRoleId} não encontrado.")
                );
            }

            if (!existingRole.RoleName.Equals(updateUserRole.RoleName, StringComparison.Ordinal))
            {
                if (await _unitOfWork.UsersRole.GetByNameAsync(updateUserRole.RoleName) != null)
                {
                    return Result.Failure(
                        Error.Validation(
                        $"O nome do Nível de Acesso '{updateUserRole.RoleName}' já está em uso.")
                    );
                }

                existingRole.UpdateName(updateUserRole.RoleName);

                await _unitOfWork.UsersRole.UpdateAsync(existingRole);
            }

            await _unitOfWork.CommitAsync();

            return Result.Success("Nível de Acesso atualizado com sucesso.");
        }

        public async Task<Result> DeleteUsersRoleAsync(int id)
        {
            var existingRole = await _unitOfWork.UsersRole.ReadByIdAsync(id);

            if (existingRole == null)
            {
                return Result.Success($"Nível de Acesso com ID {id} não encontrado (Idempotência).");
            }

            await _unitOfWork.UsersRole.RemoveAsync(existingRole);
            await _unitOfWork.CommitAsync();

            return Result.Success("Nível de Acesso eliminado com sucesso.");
        }
    }
}
