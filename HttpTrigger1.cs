using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OP_BANK.Repositories;
using OP_BANK.models;

namespace BlueSea.Function;

public class HttpTrigger1
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    private readonly ILogger<HttpTrigger1> _logger;
    private readonly ICustomerRepository _customerRepository;

    public HttpTrigger1(
        ILogger<HttpTrigger1> logger,
        ICustomerRepository customerRepository)
    {
        _logger = logger;
        _customerRepository = customerRepository;
    }

    [Function("HttpTrigger1")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "put", "post", "delete", Route = "customers/{id?}")] HttpRequest req,
        int? id)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request: {Method}", req.Method);

        if (req.Method.Equals(HttpMethods.Get, StringComparison.OrdinalIgnoreCase))
        {
            if (id.HasValue)
            {
                var customer = await _customerRepository.GetByIdAsync(id.Value);
                return customer is null
                    ? new NotFoundObjectResult($"Customer with id {id.Value} was not found.")
                    : new OkObjectResult(customer);
            }

            return new OkObjectResult(await _customerRepository.GetAllAsync());
        }

        if (req.Method.Equals(HttpMethods.Put, StringComparison.OrdinalIgnoreCase))
        {
            if (!id.HasValue)
            {
                return new BadRequestObjectResult("Customer id is required for an update.");
            }

            var updatedCustomer = await ReadCustomerAsync(req);
            if (updatedCustomer is null)
            {
                return new BadRequestObjectResult("Customer payload is missing or invalid.");
            }

            var savedCustomer = await _customerRepository.UpdateAsync(id.Value, updatedCustomer);
            if (savedCustomer is null)
            {
                return new NotFoundObjectResult($"Customer with id {id.Value} was not found.");
            }

            return new OkObjectResult(new
            {
                message = "Customer updated successfully.",
                customer = savedCustomer
            });
        }

        if (req.Method.Equals(HttpMethods.Post, StringComparison.OrdinalIgnoreCase))
        {
            var customer = await ReadCustomerAsync(req);
            if (customer is null)
            {
                return new BadRequestObjectResult("Customer payload is missing or invalid.");
            }

            customer.CreatedAt = customer.CreatedAt == default ? DateTime.UtcNow : customer.CreatedAt;
            var savedCustomer = await _customerRepository.AddAsync(customer);

            return new OkObjectResult(new
            {
                message = "Customer created successfully.",
                customer = savedCustomer
            });
        }

        if (req.Method.Equals(HttpMethods.Delete, StringComparison.OrdinalIgnoreCase))
        {
            if (!id.HasValue)
            {
                return new BadRequestObjectResult("Customer id is required for deletion.");
            }

            var deleted = await _customerRepository.DeleteAsync(id.Value);
            return deleted
                ? new NoContentResult()
                : new NotFoundObjectResult($"Customer with id {id.Value} was not found.");
        }

        return new StatusCodeResult(StatusCodes.Status405MethodNotAllowed);
    }

    private static async Task<Customer?> ReadCustomerAsync(HttpRequest req)
    {
        using var reader = new StreamReader(req.Body);
        var body = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Customer>(body, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
