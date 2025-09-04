using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Greggs.Products.Api.Controllers;
using Greggs.Products.Api.DataAccess;
using Greggs.Products.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
namespace Greggs.Products.Tests;
public class ProductControllerTests
{
    private readonly Mock<ILogger<ProductController>> _loggerMock;
    private readonly Mock<IDataAccess<Product>> _dataAccessMock;
    private readonly ProductController _controller;
    private readonly List<Product> _sampleProducts = new()
    {
        new Product { Name = "Sausage Roll", PriceInPounds = 1m },
        new Product { Name = "Vegan Sausage Roll", PriceInPounds = 1.1m },
        new Product { Name = "Steak Bake", PriceInPounds = 1.2m },
        new Product { Name = "Yum Yum", PriceInPounds = 0.7m },
        new Product { Name = "Pink Jammie", PriceInPounds = 0.5m }
    };

    public ProductControllerTests()
    {
        _loggerMock = new Mock<ILogger<ProductController>>();
        _dataAccessMock = new Mock<IDataAccess<Product>>();
        _controller = new ProductController(_loggerMock.Object, _dataAccessMock.Object);
    }

    [Fact]
    public async Task Get_InvalidPageParams_ReturnsBadRequest()
    {
        var result = await _controller.Get(pageStart: -1, pageSize: 5);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid request parameters.", badRequest.Value);
    }

    [Fact]
    public async Task Get_InvalidCurrency_ReturnsBadRequest()
    {
        var result = await _controller.Get(currency: "CAD"); // Not Tim Hortons

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Invalid currency. Supported currencies: GBP, EUR", badRequest.Value);
    }

    // Testing against hardcoded sample data, can be changed if needed
    [Fact]
    public async Task Get_ValidRequest_GbpCurrency_ReturnsProducts()
    {
        _dataAccessMock.Setup(d => d.List(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(_sampleProducts);

        var result = await _controller.Get();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var products = Assert.IsAssignableFrom<IEnumerable<Product>>(okResult.Value);

        Assert.Equal(5, products.Count());
        Assert.Contains(products, p => p.Name == "Sausage Roll" && p.PriceInPounds == 1m);
    }

    [Fact]
    public async Task Get_ValidRequest_EurCurrency_ConvertsPrices()
    {
        _dataAccessMock.Setup(d => d.List(It.IsAny<int>(), It.IsAny<int>()))
            .Returns(_sampleProducts);

        var result = await _controller.Get(currency: "EUR");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var products = Assert.IsAssignableFrom<IEnumerable<Product>>(okResult.Value);
     
        Assert.Contains(products, p => p.Name == "Sausage Roll" && p.PriceInPounds == 1.11m);
        Assert.Contains(products, p => p.Name == "Pink Jammie" && p.PriceInPounds == 0.56m);
    }

    [Fact]
    public async Task Get_DataAccessThrows_ReturnsInternalServerError()
    {
        _dataAccessMock.Setup(d => d.List(It.IsAny<int>(), It.IsAny<int>()))
            .Throws(new Exception("DB failure"));

        var result = await _controller.Get();

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An error occurred while retrieving products.", objectResult.Value);
    }
}
    