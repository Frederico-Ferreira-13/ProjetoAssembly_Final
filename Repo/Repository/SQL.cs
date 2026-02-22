using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Repo.Repository
{
    public static class SQL
    {
        private static string? _connectionString;

        public static void Initialize(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("A string de coneção não pode ser nula ou vazia.", nameof(connectionString));
            }

            _connectionString = connectionString;
        }

        public static async Task<int> ExecuteNonQueryAsync(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddRange(parameters);
                await conn.OpenAsync();

                return await cmd.ExecuteNonQueryAsync();
            }
        }

        public static async Task<int> ExecuteInsertAsync(string sql, params SqlParameter[] parameters)
        {
            string insertSql = sql + "; SELECT CAST(scope_identity() AS int)";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(insertSql, conn))
                {
                    cmd.Parameters.AddRange(parameters);

                    await conn.OpenAsync();
                    try
                    {
                        object? result = await cmd.ExecuteScalarAsync();

                        return (result is int newId) ? newId : 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\nERRO FATAL no SQL.ExecuteInsert: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public static async Task<object?> ExecuteScalarAsync(string sql, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddRange(parameters);
                    await conn.OpenAsync();
                    try
                    {
                        return await cmd.ExecuteScalarAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\nERRO FATAL no SQL.ExecuteScalar: {ex.Message}");
                        throw;
                    }
                }
            }
        }

        public static async Task<SqlDataReader> ExecuteQueryAsync(string sql, params SqlParameter[] parameters)
        {
            try
            {
                SqlConnection conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                SqlCommand cmd = new SqlCommand(sql, conn);
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro SQL Query: {ex.Message}");
                throw;
            }
        }
    }
}
