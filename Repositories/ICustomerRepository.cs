using OP_BANK.models;

namespace OP_BANK.Repositories;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync();
    Task<Customer?> GetByIdAsync(int id);
    Task<Customer> AddAsync(Customer customer);
    Task<Customer?> UpdateAsync(int id, Customer customer);
    Task<bool> DeleteAsync(int id);
}
