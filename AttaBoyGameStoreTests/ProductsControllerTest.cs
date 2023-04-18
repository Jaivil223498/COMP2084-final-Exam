using AttaBoyGameStore.Controllers;
using AttaBoyGameStore.Data;
using AttaBoyGameStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AttaBoyGameStoreTests
{
    [TestClass]
    public class ProductsControllerTest
    {
        private ApplicationDbContext _context;
        private ProductsController _controller;

        private Brand _brand;
        private Category _category;
        private List<Product> _products = new List<Product>();

        [TestInitialize]
        public void Init()
        { 
            // Mock db
            var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(dbOptions);

            // Mock Brand
            _brand = new Brand
            {
                Id = 1000,
                Name = "Mock Brand",
            };
            _context.Brands.Add(_brand);

            // Mock Category
            _category = new Category
            {
                Id = 2000,
                Name = "Mock Category"
            };
            _context.Categories.Add(_category);

            // Mock products
            _products.Add(new Product
            {
                Id = 1,
                Name = "Mock Product 1",
                Price = 100,
                Rating = 1,

                BrandId = _brand.Id,
                Brand = _brand,

                CategoryId = _category.Id,
                Category = _category,
            });
            _products.Add(new Product
            {
                Id = 2,
                Name = "Mock Product 2",
                Price = 321,
                Rating = 4,

                BrandId = _brand.Id,
                Brand = _brand,

                CategoryId = _category.Id,
                Category = _category,
            });

            foreach (var product in _products)
            {
                _context.Products.Add(product);
	        }

            _context.SaveChanges();

            _controller = new ProductsController(_context);
	    }

        [TestMethod]
        public async Task IndexReturnsProducts()
        {
            var result = (ViewResult) await _controller.Index();

            var actual = (List<Product>) result.Model;

            CollectionAssert.AreEqual(
                _products.OrderBy(p => p.Name).ToList(),
                actual
		    );
        }

        // Happy-path
        [TestMethod]
        public async Task DetailsReturnsProduct()
        {
            var expectedProduct = _products[0];

            var result = (ViewResult) await _controller.Details(expectedProduct.Id);

            Assert.IsNull(result.ViewName);
            Assert.AreEqual(expectedProduct, result.Model);
	    }

        [TestMethod]
        public async Task DetailsNotFoundWithNullId()
        { 
            var result = await _controller.Details(null);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
	    }

        [TestMethod]
        public async Task DetailsNotFoundWithNullProducts()
        {
            // Arrange
            _context.Products = null;

            // Act
            var result = await _controller.Details(_products[0].Id);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
	    }

        [TestMethod]
        public async Task DetailsNotFoundWithInvalidId()
        {
            int invalidId = 999999999;

            var result = await _controller.Details(invalidId);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
	    }


        // Lab 4 tests

        [TestMethod]
        public void GetCreateReturnsView()
        {
            // Act
            var result = (ViewResult) _controller.Create();

            // Assert

            // check for select lists
            Assert.IsInstanceOfType(result.ViewData["BrandId"], typeof(SelectList));
            Assert.IsInstanceOfType(result.ViewData["CategoryId"], typeof(SelectList));

            // check for default view
            Assert.IsNull(result.ViewName);

            // Assert.AreEqual("Create", result.ViewName);
	    }

        [TestMethod]
        public async Task PostCreateAddsToDb()
        {
            // Arrange
            var product = CreateSomeProduct();

            // Act
            var result = (RedirectToActionResult) await _controller.Create(product, null);

            // Assert
            Assert.AreEqual(nameof(_controller.Index), result.ActionName);

            Assert.AreEqual(_products.Count() + 1, _context.Products.Count());
            Assert.AreEqual(product, _context.Products.Find(product.Id));
	    }

        [TestMethod]
        public async Task PostCreateInvalidStateDoesntCreate()
        {
            // Arrange
            var product = CreateSomeProduct();

            _controller.ModelState.AddModelError("Name", "Invalid");

            // Act
            var result = (ViewResult) await _controller.Create(product, null);

            // Assert
            Assert.IsNull(result.ViewName);
            Assert.AreEqual(product, result.Model);

            Assert.AreEqual(_products.Count(), _context.Products.Count());
            Assert.IsNull(_context.Products.Find(product.Id));
	    }

        private Product CreateSomeProduct()
        { 
            return new Product()
            {
                Id = 321,
                Name = "Mock Product",

                Price = 1000,
                Rating = 5,

                BrandId = _brand.Id,
                Brand = _brand,

                CategoryId = _category.Id,
                Category = _category,
            };
	    }
    }
}
