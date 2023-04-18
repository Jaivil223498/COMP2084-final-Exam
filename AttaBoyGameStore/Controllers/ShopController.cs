using AttaBoyGameStore.Data;
using AttaBoyGameStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Stripe;
using Stripe.Checkout;

namespace AttaBoyGameStore.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        public ShopController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // GET Shop
        public IActionResult Index()
        {
            var categories = _context.Categories
                .OrderBy(c => c.Name)
                .ToList();

            return View(categories);
        }

        // GET Shop/Category/5
        public IActionResult Category(int Id)
        {
            var category = _context.Categories.Find(Id);

            if (category == null)
            {
                return NotFound();
            }

            ViewData["CategoryName"] = category.Name;

            var products = _context.Products
                .Where(p => p.CategoryId == Id)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .ToList();

            products = products.OrderBy(p => p.Name).ToList();

            return View(products);
        }

        public IActionResult Cart()
        {
            var customerId = GetCustomerId();

            var cartLines = _context.CartLines
                .Where(cl => cl.CustomerId == customerId)
                .Include(cl => cl.Product)
                .OrderByDescending(cl => cl.Id)
                .ToList();

            ViewData["TotalPrice"] = cartLines
                .Sum(cl => cl.Price)
                .ToString("C");

            /*
            decimal totalPrice = 0;

            for (int i = 0; i < cartLines.Count(); i++)
            {
                CartLine cartLine = cartLines[i];

                totalPrice += cartLine.Price;
            }

            ViewData["TotalPrice"] = totalPrice.ToString("C");
            */

            return View(cartLines);
        }

        // GET Shop/Checkout
        [Authorize]
        [HttpGet]
        public IActionResult Checkout()
        {
            // Calculate total price of cart
            var customerId = GetCustomerId();

            var cartLines = _context.CartLines
                .Where(cl => cl.CustomerId == customerId)
                .ToList();

            ViewData["TotalPrice"] = cartLines
                .Sum(cl => cl.Price)
                .ToString("C");

            return View();
        }

        // POST Shop/Checkout
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(
            [Bind("FirstName,LastName,Address,City,Province,PostalCode,Phone")] Order Order
        )
        {
            Order.OrderDate = DateTime.UtcNow;
            Order.CustomerId = GetCustomerId();
            Order.InProgress = true;

            // Delete any old in-progress orders for this customer
            var inProgressOrders = await _context.Orders
                .Where(o => o.InProgress && o.CustomerId == Order.CustomerId)
                .ToListAsync();

            foreach (var inProgressOrder in inProgressOrders)
            {
                _context.Orders.Remove(inProgressOrder);
            }

            var cartLines = await _context.CartLines
                .Where(cl => cl.CustomerId == Order.CustomerId)
                .ToListAsync();

            Order.Total = cartLines.Sum(cl => cl.Price);

            // Store in db
            await _context.Orders.AddAsync(Order);
            await _context.SaveChangesAsync();

            return Redirect("Payment");
        }

        // GET Shop/Payment
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Payment()
        {
            var customerId = GetCustomerId();
            var order = await GetCurrentOrderAsync(customerId);

            if (order == null)
            {
                return NotFound();
            }

            ViewData["TotalPrice"] = order.Total;

            ViewData["PublishableKey"] = _config["Payments:Stripe:PublishableKey"];

            return View();
        }

        // POST Shop/Payment
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Payment(String StripeToken)
        {
            var customerId = GetCustomerId();
            var order = await GetCurrentOrderAsync(customerId);

            if (order == null)
            {
                return NotFound();
            }

            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long) (order.Total * 100),
                            Currency = "CAD",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Store Purchase",
                            },
                        },
                        Quantity = 1,
                    },
                },
                PaymentMethodTypes = new List<String>
                {
                    "card",
                },
                Mode = "payment",

                SuccessUrl = "https://" + Request.Host + "/Shop/SaveOrder",
                CancelUrl = "https://" + Request.Host + "/Shop/Cart",
            };

            var service = new SessionService();
            var session = service.Create(options);

            return Json(new { id = session.Id });
        }

        // GET Shop/SaveOver
        [Authorize]
        public async Task<IActionResult> SaveOrder()
        {
            var customerId = GetCustomerId();
            var order = await GetCurrentOrderAsync(customerId);

            if (order == null)
            {
                return NotFound();
            }

            // Vulnerability: Anybody with an in-progress order
            // can call this method, without processing payments through Stripe

            order.InProgress = false;

            var cartLines = await _context.CartLines
                .Where(cl => cl.CustomerId == customerId)
                .ToListAsync();

            foreach (var cartLine in cartLines)
            {
                // Create OrderLine from the CartLine
                var orderLine = new OrderLine
                {
                    OrderId = order.Id,
                    ProductId = cartLine.ProductId,
                    Quantity = cartLine.Quantity,
                    Price = cartLine.Price,
                };

                // move cart line to order line
                _context.CartLines.Remove(cartLine);
                _context.OrderLines.Add(orderLine);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Orders", new { @id = order.Id });
        }

        // POST Shop/AddToCart
        [HttpPost]
        public IActionResult AddToCart([FromForm] int ProductId, [FromForm] int Quantity)
        {
            if (Quantity <= 0)
            {
                return BadRequest();
            }

            var product = _context.Products.Find(ProductId);
            if (product == null)
            {
                return BadRequest();
            }

            var price = product.Price * Quantity;

            var customerId = GetCustomerId();

            var cartLine = _context.CartLines
                .Where(cl => cl.ProductId == ProductId && cl.CustomerId == customerId)
                .FirstOrDefault();

            if (cartLine == null)
            {
                cartLine = new CartLine()
                {
                    ProductId = ProductId,
                    Quantity = Quantity,
                    Price = price,
                    CustomerId = customerId,
                };

                _context.CartLines.Add(cartLine);
            }
            else
            {
                cartLine.Quantity += Quantity;
                cartLine.Price += product.Price * Quantity;

                _context.CartLines.Update(cartLine);
            }

            _context.SaveChanges();

            return Redirect("Cart");
        }

        // POST Shop/UpdateCart
        [HttpPost]
        public IActionResult UpdateCart([FromForm] int CartLineId, [FromForm] int Quantity)
        {
            if (Quantity <= 0)
            {
                return BadRequest();
            }

            var cartLine = _context.CartLines.Find(CartLineId);
            if (cartLine == null)
            {
                return BadRequest();
            }

            var product = _context.Products.Find(cartLine.ProductId);

            var discount = Math.Max((product.Price * cartLine.Quantity) - cartLine.Price, 0);

            cartLine.Price = (product.Price * Quantity) - discount;
            cartLine.Quantity = Quantity;

            _context.CartLines.Update(cartLine);
            _context.SaveChanges();

            return Redirect("Cart");
        }

        // POST Shop/RemoveFromCart
        [HttpPost]
        public IActionResult RemoveFromCart([FromForm] int CartLineId)
        {
            var cartLine = _context.CartLines.Find(CartLineId);
            if (cartLine == null)
            {
                return BadRequest();
            }

            _context.CartLines.Remove(cartLine);
            _context.SaveChanges();

            return Redirect("Cart");
        }

        // GetCustomerId gets the customer id from the session
        // This might be a GUID, or the logged in user's email
        // If it isn't in the session, it stores it
        /*
        private String GetCustomerId()
        {
            var customerId = HttpContext.Session.GetString("CustomerId");

            if (!String.IsNullOrWhiteSpace(customerId))
            {
                return customerId;
            }

            // Didn't find customer id, need to generate

            var isLoggedIn = User?.Identity?.IsAuthenticated ?? false;

            if (isLoggedIn)
            {
                customerId = User.Identity.Name; // Email address
            }
            else
            {
                customerId = Guid.NewGuid().ToString(); // Generate new Id
            }

            HttpContext.Session.SetString("CustomerId", customerId);

            return customerId;
        }
        */

        private String GetCustomerId()
        {
            var isLoggedIn = User?.Identity?.IsAuthenticated ?? false;

            if (isLoggedIn)
            {
                return User.Identity.Name; // Email address
            }

            var customerId = HttpContext.Session.GetString("CustomerId");

            if (!String.IsNullOrWhiteSpace(customerId))
            {
                return customerId;
            }
            
            customerId = Guid.NewGuid().ToString(); // Generate new Id

            HttpContext.Session.SetString("CustomerId", customerId);

            return customerId;
        }

        private async Task<Order?> GetCurrentOrderAsync(String customerId)
        {
            var orders = await _context.Orders
                .Where(o => o.InProgress && o.CustomerId == customerId)
                .ToListAsync();

            if (orders.Count() == 0)
            {
                return null;
            }

            return orders[0];
        }
    }
}
