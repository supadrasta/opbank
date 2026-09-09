using System;
using System.Collections.Generic;
using System.Linq;

namespace OP_BANK.models
{
    public static class CustomerStore
    {
        private static readonly List<Customer> _customers = new();

        public static IReadOnlyList<Customer> Customers => _customers;

        public static Customer AddCustomer(Customer customer)
        {
            if (customer is null)
            {
                throw new ArgumentNullException(nameof(customer));
            }

            if (customer.Id == 0)
            {
                customer.Id = _customers.Any() ? _customers.Max(c => c.Id) + 1 : 1;
            }

            customer.CreatedAt = customer.CreatedAt == default ? DateTime.UtcNow : customer.CreatedAt;
            _customers.Add(customer);

            return customer;
        }

        public static IReadOnlyList<Customer> GetAllCustomers()
        {
            return _customers;
        }

        public static Customer? GetCustomerById(int id)
        {
            return _customers.FirstOrDefault(c => c.Id == id);
        }

        public static Customer? UpdateCustomer(int id, Customer updatedCustomer)
        {
            if (updatedCustomer is null)
            {
                throw new ArgumentNullException(nameof(updatedCustomer));
            }

            var existingCustomer = _customers.FirstOrDefault(c => c.Id == id);
            if (existingCustomer is null)
            {
                return null;
            }

            existingCustomer.FirstName = updatedCustomer.FirstName;
            existingCustomer.LastName = updatedCustomer.LastName;
            existingCustomer.Email = updatedCustomer.Email;
            existingCustomer.PhoneNumber = updatedCustomer.PhoneNumber;
            existingCustomer.IsActive = updatedCustomer.IsActive;
            existingCustomer.CreatedAt = existingCustomer.CreatedAt == default ? DateTime.UtcNow : existingCustomer.CreatedAt;

            return existingCustomer;
        }

        public static bool DeleteCustomer(int id)
        {
            var customer = _customers.FirstOrDefault(c => c.Id == id);
            if (customer is null)
            {
                return false;
            }

            _customers.Remove(customer);
            return true;
        }
    }
}
