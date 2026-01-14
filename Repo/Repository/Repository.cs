using Contracts.Repository;
using Core.Common;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repo.Repository
{
    public abstract class Repository<TEntity> : IRepository<TEntity>
        where TEntity : class, IEntity
    {
        protected readonly string _tableName;

        public Repository(string tableName)
        {
            _tableName = tableName;
        }

        protected abstract TEntity MapFromReader(SqlDataReader reader);
        protected abstract string BuildInsertSql(TEntity entity);
        protected abstract SqlParameter[] GetInsertParameters(TEntity entity);
        protected abstract string BuildUpdateSql(TEntity entity);
        protected abstract SqlParameter[] GetUpdateParameters(TEntity entity);

        public async Task CreateAddAsync(TEntity entity)
        {
            string sql = BuildInsertSql(entity);
            SqlParameter[] parameters = GetInsertParameters(entity);

            try
            {
                int newId = await SQL.ExecuteInsertAsync(sql, parameters);

                if (newId > 0)
                {
                    entity.SetId(newId);
                }
                else
                {
                    throw new Exception("Falha ao obter o novo ID após a criação.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório CREATE: {ex.Message}");
                throw;
            }
        }

        public async Task<TEntity?> ReadByIdAsync(int id)
        {
            string sql = $"SELECT * FROM {_tableName} Where {_tableName}Id = @Id AND IsActive = 1";
            SqlParameter[] parameters = new SqlParameter[] { new SqlParameter("@Id", id) };

            try
            {
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, parameters))
                {
                    if (reader.Read())
                    {
                        return MapFromReader(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório RETRIEVE: {ex.Message}");
                throw;
            }

            return default;
        }

        public async Task<IEnumerable<TEntity>> ReadAllAsync()
        {
            var entities = new List<TEntity>();
            string sql = $"SELECT * FROM {_tableName} WHERE IsActive = 1";

            try
            {
                // Chama o método estático para obter o DataReader.
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql))
                {
                    while (reader.Read())
                    {
                        entities.Add(MapFromReader(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório ReadAll: {ex.Message}");
                throw;
            }
            return entities;
        }

        public async Task UpdateAsync(TEntity entity)
        {
            string sql = BuildUpdateSql(entity);
            SqlParameter[] parameters = GetUpdateParameters(entity);

            try
            {
                // Chama o método estático.
                await SQL.ExecuteNonQueryAsync(sql, parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório UPDATE: {ex.Message}");
                throw;
            }
        }

        public async Task RemoveAsync(TEntity entity)
        {
            string sql = $"UPDATE {_tableName} SET IsActive = 0, LastUpdatedAt = GETDATE() WHERE {_tableName}Id = @Id";
            SqlParameter paramId = new SqlParameter("@Id", entity.GetId());

            try
            {
                await SQL.ExecuteNonQueryAsync(sql, paramId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório DELETE: {ex.Message}");
                throw;
            }
        }

        public Task<int> SaveChangesAsync()
        {
            return Task.FromResult(1);
        }

        protected async Task<TEntity?> ExecuteSingleAsync(string sql, params SqlParameter[] parameters)
        {
            try
            {
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, parameters))
                {
                    if (reader.Read())
                    {
                        return MapFromReader(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório ExecuteSingleAsync para {_tableName}: {ex.Message}");
                throw;
            }
            return default!;
        }

        protected async Task<IEnumerable<TEntity>> ExecuteListAsync(string sql, params SqlParameter[] parameters)
        {
            var entities = new List<TEntity>();
            try
            {
                using (SqlDataReader reader = await SQL.ExecuteQueryAsync(sql, parameters))
                {
                    while (reader.Read())
                    {
                        entities.Add(MapFromReader(reader));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro no Repositório ExecuteListAsync para {_tableName}: {ex.Message}");
                throw;
            }
            return entities;
        }
    }
}
