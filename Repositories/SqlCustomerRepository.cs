using System.Data;
using Microsoft.Data.SqlClient;
using OP_BANK.models;

namespace OP_BANK.Repositories;

public class SqlCustomerRepository : ICustomerRepository
{
    private readonly string _connectionString;

    public SqlCustomerRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        InitializeDatabaseAsync().GetAwaiter().GetResult();
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync()
    {
        var customers = new List<Customer>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string query = "SELECT Id, FirstName, LastName, Email, PhoneNumber, CreatedAt, IsActive FROM Customers ORDER BY Id;";

        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            customers.Add(MapCustomer(reader));
        }

        return customers;
    }

    private static Customer MapCustomer(SqlDataReader reader)
    {
        return new Customer
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            FirstName = reader.IsDBNull(reader.GetOrdinal("FirstName")) ? string.Empty : reader.GetString(reader.GetOrdinal("FirstName")),
            LastName = reader.IsDBNull(reader.GetOrdinal("LastName")) ? string.Empty : reader.GetString(reader.GetOrdinal("LastName")),
            Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? string.Empty : reader.GetString(reader.GetOrdinal("Email")),
            PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? string.Empty : reader.GetString(reader.GetOrdinal("PhoneNumber")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
        };
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string query = "SELECT Id, FirstName, LastName, Email, PhoneNumber, CreatedAt, IsActive FROM Customers WHERE Id = @Id;";

        await using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return MapCustomer(reader);
    }

    public async Task<Customer> AddAsync(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        customer.CreatedAt = customer.CreatedAt == default ? DateTime.UtcNow : customer.CreatedAt;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string query = @"
            INSERT INTO Customers (FirstName, LastName, Email, PhoneNumber, CreatedAt, IsActive)
            OUTPUT INSERTED.Id
            VALUES (@FirstName, @LastName, @Email, @PhoneNumber, @CreatedAt, @IsActive);";

        await using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@FirstName", SqlDbType.NVarChar, 200).Value = customer.FirstName ?? string.Empty;
        command.Parameters.Add("@LastName", SqlDbType.NVarChar, 200).Value = customer.LastName ?? string.Empty;
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = customer.Email ?? string.Empty;
        command.Parameters.Add("@PhoneNumber", SqlDbType.NVarChar, 50).Value = customer.PhoneNumber ?? string.Empty;
        command.Parameters.Add("@CreatedAt", SqlDbType.DateTime2).Value = customer.CreatedAt;
        command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = customer.IsActive;

        var id = (int)await command.ExecuteScalarAsync();
        customer.Id = id;
        return customer;
    }

    public async Task<Customer?> UpdateAsync(int id, Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string query = @"
            UPDATE Customers
            SET FirstName = @FirstName,
                LastName = @LastName,
                Email = @Email,
                PhoneNumber = @PhoneNumber,
                IsActive = @IsActive
            WHERE Id = @Id;";

        await using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        command.Parameters.Add("@FirstName", SqlDbType.NVarChar, 200).Value = customer.FirstName ?? string.Empty;
        command.Parameters.Add("@LastName", SqlDbType.NVarChar, 200).Value = customer.LastName ?? string.Empty;
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 255).Value = customer.Email ?? string.Empty;
        command.Parameters.Add("@PhoneNumber", SqlDbType.NVarChar, 50).Value = customer.PhoneNumber ?? string.Empty;
        command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = customer.IsActive;

        var rowsAffected = await command.ExecuteNonQueryAsync();
        if (rowsAffected == 0)
        {
            return null;
        }

        customer.Id = id;
        return customer;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string query = "DELETE FROM Customers WHERE Id = @Id;";

        await using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    private async Task InitializeDatabaseAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string query = @"
            IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Customers (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    FirstName NVARCHAR(200) NOT NULL,
                    LastName NVARCHAR(200) NOT NULL,
                    Email NVARCHAR(255) NULL,
                    PhoneNumber NVARCHAR(50) NULL,
                    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    IsActive BIT NOT NULL DEFAULT 1
                );
            END;";

        await using var command = new SqlCommand(query, connection);
        await command.ExecuteNonQueryAsync();
    }
}
