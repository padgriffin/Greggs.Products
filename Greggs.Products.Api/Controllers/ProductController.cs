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

    public ProductController(ILogger<ProductController> logger, IDataAccess<Product> dataAccess)
    {
        _logger = logger;
        _dataAccess = dataAccess;
    }

    [HttpGet]
    public ActionResult<IEnumerable<Product>> Get(int pageStart = 0, int pageSize = 5)
    {
        // Catching invalid (<= 0) requests
        if (pageStart < 0 || pageSize <= 0)
        {
            _logger.LogWarning("Invalid request params: pageStart={PageStart}, pageSize={PageSize}", pageStart, pageSize);
            return BadRequest("Invalid request parameters.");
        }

        // Not reimplementing the access method since that's fully tested
        try
        {
            var products = _dataAccess.List(pageStart, pageSize);
            return Ok(products);
        }

        // Catching unexpected errors
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching list");
            return StatusCode(500, "An error occurred while retrieving products.");
        }
    }
}
