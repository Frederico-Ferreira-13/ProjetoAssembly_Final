using Contracts.Repository;
using Contracts.Service;
using Core.Common;
using Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Services
{
    public class UserSettingsService : IUserSettingsService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserSettingsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<UserSettings>> GetSettingsByUserIdAsync(int userId)
        {
            var settings = await _unitOfWork.UserSettings.GetByUserId(userId);

            if (settings == null)
            {
                return Result<UserSettings>.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Configurações para o Utilizador com ID {userId} não foram encontradas.")
                );
            }

            return Result<UserSettings>.Success(settings);
        }

        public async Task<Result> UpdateUserSettingsAsync(UserSettings settings)
        {
            if (settings == null || settings.UserId <= 0)
            {
                return Result.Failure(
                    Error.Validation(
                    "O ID do Utilizador é obrigatório para a atualização das configurações.")
                );
            }            

            var existingSettings = await _unitOfWork.UserSettings.GetByUserId(settings.UserId);
            if (existingSettings == null)
            {
                return Result.Failure(
                    Error.NotFound(
                    ErrorCodes.NotFound,
                    $"Configurações para o Utilizador com ID {settings.UserId} não foram encontradas. Tente criar as configurações padrão primeiro.")
                );
            }

            try
            {
                existingSettings.UpdateSettings(
                    settings.Theme,
                    settings.Language,
                    settings.ReceiveNotifications
                );

                await _unitOfWork.UserSettings.UpdateAsync(existingSettings);
                await _unitOfWork.CommitAsync();

                return Result.Success("Configurações atualizadas com sucesso.");
            }
            catch (ArgumentException ex)
            {
                string fieldName = ex.ParamName ?? "Geral";
                return Result.Failure(
                    Error.Validation(
                    "Dados de entrada inválidos para a atualização das configurações.",
                    new Dictionary<string, string[]> { { fieldName, new[] { ex.Message } } })
                );
            }


        }

        public async Task<Result<UserSettings>> CreateDefaultSettingsAsync(int userId)
        {
            var userExists = await _unitOfWork.Users.ReadByIdAsync(userId) != null;
            if (!userExists)
            {
                return Result<UserSettings>.Failure(
                    Error.BusinessRuleViolation(
                    ErrorCodes.BizInvalidOperation,
                    $"Utilizador com ID {userId} não existe. Não é possível criar configurações para um utilizador inexistente.")
                );
            }

            var existingSettings = await _unitOfWork.UserSettings.GetByUserId(userId);
            if (existingSettings != null)
            {
                return Result<UserSettings>.Failure(
                    Error.Validation(
                    $"Configurações para o Utilizador com ID {userId} já existem.")
                );
            }

            var defaultSettings = new UserSettings(
                userId: userId,
                theme: "Light",
                language: "pt-PT",
                receiveNotifications: true
            );

            await _unitOfWork.UserSettings.CreateAddAsync(defaultSettings);
            await _unitOfWork.CommitAsync();

            return Result<UserSettings>.Success(defaultSettings);
        }
    }
}
