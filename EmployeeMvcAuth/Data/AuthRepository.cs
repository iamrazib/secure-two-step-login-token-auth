using EmployeeMvcAuth.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using System.Data;

namespace EmployeeMvcAuth.Data
{
    public class AuthRepository
    {
        private readonly string _connectionString;
        private readonly IPasswordHasher<EmployeePasswordUser> _passwordHasher;

        public AuthRepository(IConfiguration configuration,IPasswordHasher<EmployeePasswordUser> passwordHasher)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string not found.");

            _passwordHasher = passwordHasher;
        }

        public async Task<(bool Success, Guid? ChallengeId, string Message)> StartLoginAsync(string userId)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_StartLogin", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add("@UserId", SqlDbType.NVarChar, 50).Value = userId.Trim();

            await connection.OpenAsync();

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var success = reader.GetBoolean(reader.GetOrdinal("Success"));

                Guid? challengeId = reader.IsDBNull(reader.GetOrdinal("ChallengeId"))
                    ? null
                    : reader.GetGuid(reader.GetOrdinal("ChallengeId"));

                var message = reader.GetString(reader.GetOrdinal("Message"));

                return (success, challengeId, message);
            }

            return (false, null, "Login failed.");
        }

        public async Task<(bool Success, Guid? SessionToken, string Message)> CheckPasswordAndCreateSessionAsync(
            Guid challengeId,
            string password)
        {
            var passwordResult = await GetPasswordHashByChallengeAsync(challengeId);

            if (!passwordResult.Success || passwordResult.EmployeeKey == null || passwordResult.PasswordHash == null)
            {
                return (false, null, "Invalid or expired login request.");
            }

            var user = new EmployeePasswordUser(
                passwordResult.EmployeeKey.Value,
                passwordResult.UserId!
            );

            var verifyResult = _passwordHasher.VerifyHashedPassword(
                user,
                passwordResult.PasswordHash,
                password
            );

            if (verifyResult == PasswordVerificationResult.Failed)
            {
                return (false, null, "Invalid password.");
            }

            return await CreateSessionAsync(challengeId);
        }

        private async Task<(bool Success, Guid? EmployeeKey, string? UserId, string? PasswordHash)> GetPasswordHashByChallengeAsync(
            Guid challengeId)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_GetPasswordHashByChallenge", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add("@ChallengeId", SqlDbType.UniqueIdentifier).Value = challengeId;

            await connection.OpenAsync();

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var success = reader.GetBoolean(reader.GetOrdinal("Success"));

                if (!success)
                {
                    return (false, null, null, null);
                }

                return
                (
                    true,
                    reader.GetGuid(reader.GetOrdinal("EmployeeKey")),
                    reader.GetString(reader.GetOrdinal("UserId")),
                    reader.GetString(reader.GetOrdinal("PasswordHash"))
                );
            }

            return (false, null, null, null);
        }

        private async Task<(bool Success, Guid? SessionToken, string Message)> CreateSessionAsync(Guid challengeId)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_CreateSessionAfterPasswordSuccess", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add("@ChallengeId", SqlDbType.UniqueIdentifier).Value = challengeId;

            await connection.OpenAsync();

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var success = reader.GetBoolean(reader.GetOrdinal("Success"));

                if (!success)
                {
                    return (false, null, "Session creation failed.");
                }

                var sessionToken = reader.GetGuid(reader.GetOrdinal("SessionToken"));

                return (true, sessionToken, "Login successful.");
            }

            return (false, null, "Session creation failed.");
        }

        public async Task<DashboardViewModel?> GetMyEmployeeProfileAsync(Guid sessionToken)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_GetMyEmployeeProfile", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add("@SessionToken", SqlDbType.UniqueIdentifier).Value = sessionToken;

            await connection.OpenAsync();

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new DashboardViewModel
            {
                EmployeeKey = reader.GetGuid(reader.GetOrdinal("EmployeeKey")),
                UserId = reader.GetString(reader.GetOrdinal("UserId")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Email = reader.IsDBNull(reader.GetOrdinal("Email"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("Email"))
            };
        }

        public async Task CreateEmployeeIfNotExistsAsync(
            string userId,
            string password,
            string fullName,
            string email)
        {
            var employeeKey = Guid.NewGuid();

            var user = new EmployeePasswordUser(employeeKey, userId);

            var passwordHash = _passwordHasher.HashPassword(user, password);

            await using var connection = new SqlConnection(_connectionString);

            const string sql = """
                IF NOT EXISTS (SELECT 1 FROM Employees WHERE UserId = @UserId)
                BEGIN
                    INSERT INTO Employees
                    (
                        EmployeeKey,
                        UserId,
                        PasswordHash,
                        FullName,
                        Email
                    )
                    VALUES
                    (
                        @EmployeeKey,
                        @UserId,
                        @PasswordHash,
                        @FullName,
                        @Email
                    )
                END
                """;

            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@EmployeeKey", SqlDbType.UniqueIdentifier).Value = employeeKey;
            command.Parameters.Add("@UserId", SqlDbType.NVarChar, 50).Value = userId;
            command.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 500).Value = passwordHash;
            command.Parameters.Add("@FullName", SqlDbType.NVarChar, 150).Value = fullName;
            command.Parameters.Add("@Email", SqlDbType.NVarChar, 150).Value = email;

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }

        public async Task<(bool Success, string Message)> CreateEmployeeAsync(CreateEmployeeViewModel model)
        {
            var employeeKey = Guid.NewGuid();

            var user = new EmployeePasswordUser(employeeKey, model.UserId.Trim());

            var passwordHash = _passwordHasher.HashPassword(user, model.Password);

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_CreateEmployee", connection);

            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("@EmployeeKey", SqlDbType.UniqueIdentifier).Value = employeeKey;
            command.Parameters.Add("@UserId", SqlDbType.NVarChar, 50).Value = model.UserId.Trim();
            command.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 500).Value = passwordHash;
            command.Parameters.Add("@FullName", SqlDbType.NVarChar, 150).Value = model.FullName.Trim();

            command.Parameters.Add("@Email", SqlDbType.NVarChar, 150).Value =
                string.IsNullOrWhiteSpace(model.Email)
                    ? DBNull.Value
                    : model.Email.Trim();

            await connection.OpenAsync();

            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var success = reader.GetBoolean(reader.GetOrdinal("Success"));
                var message = reader.GetString(reader.GetOrdinal("Message"));

                return (success, message);
            }

            return (false, "User creation failed.");
        }

        public async Task<EmployeeSalaryViewModel?> GetEmployeeSalaryAsync(Guid sessionToken)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_GetEmployeeSalary", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add("@SessionToken", SqlDbType.UniqueIdentifier).Value = sessionToken;

            await connection.OpenAsync();

            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            var success = reader.GetBoolean(reader.GetOrdinal("Success"));

            if (!success)
            {
                return null;
            }

            return new EmployeeSalaryViewModel
            {
                UserId = reader.GetString(reader.GetOrdinal("UserId")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                BasicSalary = reader.GetDecimal(reader.GetOrdinal("BasicSalary")),
                HouseRent = reader.GetDecimal(reader.GetOrdinal("HouseRent")),
                MedicalAllowance = reader.GetDecimal(reader.GetOrdinal("MedicalAllowance")),
                Bonus = reader.GetDecimal(reader.GetOrdinal("Bonus")),
                TotalSalary = reader.GetDecimal(reader.GetOrdinal("TotalSalary")),
                EffectiveFrom = reader.GetDateTime(reader.GetOrdinal("EffectiveFrom"))
            };
        }

        public async Task RevokeSessionAsync(Guid sessionToken)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_RevokeSession", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add("@SessionToken", SqlDbType.UniqueIdentifier).Value = sessionToken;

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }



        public async Task<EmployeeLeaveDetailsViewModel?> GetEmployeeLeaveDetailsAsync(Guid sessionToken)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("dbo.usp_GetEmployeeLeaveDetails", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add("@SessionToken", SqlDbType.UniqueIdentifier).Value = sessionToken;

            await connection.OpenAsync();

            await using var reader = await command.ExecuteReaderAsync();

            EmployeeLeaveDetailsViewModel? model = null;

            while (await reader.ReadAsync())
            {
                var isSessionValid = reader.GetBoolean(reader.GetOrdinal("IsSessionValid"));
                var message = reader.GetString(reader.GetOrdinal("Message"));

                if (!isSessionValid)
                {
                    return new EmployeeLeaveDetailsViewModel
                    {
                        IsSessionValid = false,
                        Message = message
                    };
                }

                if (model == null)
                {
                    model = new EmployeeLeaveDetailsViewModel
                    {
                        IsSessionValid = true,
                        Message = message,
                        UserId = reader.GetString(reader.GetOrdinal("UserId")),
                        FullName = reader.GetString(reader.GetOrdinal("FullName"))
                    };
                }

                // No leave record case
                if (reader.IsDBNull(reader.GetOrdinal("LeaveType")))
                {
                    continue;
                }

                model.LeaveItems.Add(new EmployeeLeaveItemViewModel
                {
                    LeaveType = reader.GetString(reader.GetOrdinal("LeaveType")),
                    TotalLeave = reader.GetInt32(reader.GetOrdinal("TotalLeave")),
                    UsedLeave = reader.GetInt32(reader.GetOrdinal("UsedLeave")),
                    RemainingLeave = reader.GetInt32(reader.GetOrdinal("RemainingLeave")),
                    LeaveYear = reader.GetInt32(reader.GetOrdinal("LeaveYear"))
                });
            }

            return model;
        }

    }
}
