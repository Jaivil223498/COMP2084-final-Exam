using AttaBoyGameStore.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace AttaBoyGameStoreTests
{
    [TestClass]
    public class HomeControllerTest
    {
        private HomeController _controller;

        [TestInitialize]
        public void Init()
        {
            // Arrage - setting up the data to be tested
            _controller = new HomeController();
	    }

        [TestMethod]
        public void IndexIsNotNull()
        {
            // Act - actually running the code that is being tested
            var result = _controller.Index();

            // Assert - checking that the expected actions happened,
            //          and expected results were returned
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void IndexReturnsIndexView()
        {
            var result = (ViewResult) _controller.Index();

            Assert.AreEqual("Index", result.ViewName);
        }

        [TestMethod]
        public void IndexHasMessage()
        {
            var result = (ViewResult) _controller.Index();

            Assert.AreEqual("Hello world", result.ViewData["Message"]);
	    }


        // TDD (Test-Driven Development)

        [TestMethod]
        public void AttaBoyIsNotNull()
        {
            var result = _controller.AttaBoy();

            Assert.IsNotNull(result);
	    }

        [TestMethod]
        public void AttaBoyReturnsGameStoreView()
        {
            var result = (ViewResult) _controller.AttaBoy();

            Assert.AreEqual("GameStore", result.ViewName);
  	    }
    }
}