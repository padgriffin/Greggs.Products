using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Greggs.Products.Api.DataAccess;
using Greggs.Products.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Greggs.Products.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ProductController : ControllerBase
{
    private readonly ILogger<ProductController> _logger;
    private readonly IDataAccess<Product> _dataAccess;
    // Defining fixed conversion rate 
    private const decimal GbpToEurRate = 1.11m;

    public ProductController(ILogger<ProductController> logger, IDataAccess<Product> dataAccess)
    {
        _logger = logger;
        _dataAccess = dataAccess;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> Get(int pageStart = 0, int pageSize = 5, string currency = "GBP")
    {
        // Catching invalid (<= 0) requests
        if (pageStart < 0 || pageSize <= 0)
        {
            _logger.LogWarning("Invalid request params: pageStart={PageStart}, pageSize={PageSize}", pageStart, pageSize);
            return BadRequest("Invalid request parameters.");
        }

        // Validate currency parameter
        if (!IsValidCurrency(currency))
        {
            _logger.LogWarning("Invalid currency requested: {Currency}", currency);
            return BadRequest("Invalid currency. Supported currencies: GBP, EUR");
        }

        try
        {
            var products = await Task.FromResult(_dataAccess.List(pageStart, pageSize));

            // Convert prices if EUR is requested
            if (currency.ToUpperInvariant() == "EUR")
            {
                var convertedProducts = products.Select(ConvertProductPriceToEur).ToList();
                _logger.LogInformation("Converted {Count} products to EUR", convertedProducts.Count);
                return Ok(convertedProducts);
            }

            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching list");
            return StatusCode(500, "An error occurred while retrieving products.");
        }
    }

    // Currency conversion process, rounds to 2 decimal spaces
    private Product ConvertProductPriceToEur(Product product)
    {
        return new Product
        {
            Name = product.Name,
            PriceInPounds = Math.Round(product.PriceInPounds * GbpToEurRate, 2),
        };
    }

    // Converting currency to uppercase, checking if currency is valid
    private static bool IsValidCurrency(string currency)
    {
        var validCurrencies = new[] { "GBP", "EUR" };
        return validCurrencies.Contains(currency?.ToUpperInvariant());
    }
}