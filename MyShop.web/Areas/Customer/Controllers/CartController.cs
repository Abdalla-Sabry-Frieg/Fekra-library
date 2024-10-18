using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyShop_Entities.Helper;
using MyShop_Entities.Models;
using MyShop_Entities.Repositories;
using MyShop_Entities.ViewModels;
using Stripe.BillingPortal;
using Stripe.Checkout;
using System.Security.Claims;
using Session = Stripe.Checkout.Session;
using SessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using SessionService = Stripe.Checkout.SessionService;

namespace MyShop.web.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            // get user id is active now from database using claims
            var claimasIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimasIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var cart = new ShoppingCartViewModel()
            {
                CardList = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == claim.Value , includeWord: "Product" ),
                Category = _unitOfWork.Categories.GetAll(),
                Order = new Order()
            }; 
            
            // to calculate the total price
            foreach (var item in cart.CardList)
            {
                cart.TotalPrice += (item.Count * item.Product.Price);
            }

            return View(cart);
        }
        [HttpPost]
        public IActionResult plus(int cartid)
        {
            //var cart = _unitOfWork.ShoppingCart.GetFirstOrDefualt(x => x.Id == cartid);

            //if(cart!= null)
            //{
            //    _unitOfWork.ShoppingCart.InceaseCount(cart, 1);
            //    _unitOfWork.Complet();

            //    var count = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == cart.ApplicationUserId).Sum(x=>x.Count);
            //    HttpContext.Session.SetInt32(Helpers.SessionKey, count);
            //}

            //return RedirectToAction("Index");

            // use ajax 

            var cart = _unitOfWork.ShoppingCart.GetFirstOrDefualt(x => x.Id == cartid);

            if (cart != null)
            {
                //  _unitOfWork.ShoppingCart.InceaseCount(cart , 1);
                var count = cart.Count += 1;
                _unitOfWork.Complet();

             //   var totalPrice = GetTotalPrice(); // Adjust the logic for calculating the total price

             //   var count = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == cart.ApplicationUserId).Sum(x => x.Count);
                HttpContext.Session.SetInt32(Helpers.SessionKey, count);

                return Json(new { success = true, message = "Product added", totalPrice = GetTotalPrice(cart.ApplicationUserId), itemCount = count });
            }

            return Json(new { success = false, message = "Error occurred" });


        }
        [HttpPost]
        public IActionResult minuis(int cartid)
        {
            var cart = _unitOfWork.ShoppingCart.GetFirstOrDefualt(x => x.Id == cartid);

            if (cart == null)
            {
                return Json(new { success = false, message = "Cart item not found" });
            }

            bool itemRemoved = false;
            int itemCount = cart.Count;

            if (cart.Count <= 1)
            {
                // Remove item if count is less than or equal to 1
                _unitOfWork.ShoppingCart.Delete(cart);
                itemRemoved = true;
            }
            else
            {
                // Decrease count by 1 if count is greater than 1
                cart.Count -= 1;
                itemCount = cart.Count; // Update the item count after decrementing
               // _unitOfWork.ShoppingCart.Add(cart); // Ensure the updated cart is saved
            }

            _unitOfWork.Complet(); // Commit the changes

            // Calculate the total price after modification
            var totalPrice = GetTotalPrice(cart.ApplicationUserId);

            return Json(new
            {
                success = true,
                message = itemRemoved ? "Item removed" : "Product updated",
                totalPrice = totalPrice,
                itemCount = itemCount, // Return the updated item count
                itemRemoved = itemRemoved // Inform the front-end if the item was removed
            });
        }





        public IActionResult Remove(int cartId)
        {
            var cart = _unitOfWork.ShoppingCart.GetFirstOrDefualt(x => x.Id == cartId);

            if (cart != null)
            {
                _unitOfWork.ShoppingCart.Delete(cart);
                _unitOfWork.Complet();

                var count = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == cart.ApplicationUserId).Sum(x => x.Count);
                HttpContext.Session.SetInt32(Helpers.SessionKey, count);

                return Json(new
                {
                    success = true,
                    message = "Product removed",
                    totalPrice = GetTotalPrice(cart.ApplicationUserId),
                    itemCount = count,
                    itemRemoved = true // Send flag to inform if the item was removed
                });
            }

            return Json(new { success = false, message = "Error occurred" });
        }


        private decimal GetTotalPrice(string userId)
        {
            var cartItems = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == userId, includeWord: "Product");
            return cartItems.Sum(x => x.Count * x.Product.Price);
        }


        [HttpGet]
        public IActionResult Summary()
        {
            // get user id is active now from database using claims
            var claimasIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimasIdentity.FindFirst(ClaimTypes.NameIdentifier);

            var cart = new ShoppingCartViewModel()
            {
                CardList = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == claim.Value, includeWord: "Product"),
                Category = _unitOfWork.Categories.GetAll(),
                Order = new Order(),
          
            };

            cart.Order.ApplicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefualt(x => x.Id == claim.Value);
            // Fill data in order at summary card 
            cart.Order.Name = cart.Order.ApplicationUser.Name;
            cart.Order.Email = cart.Order.ApplicationUser.Email;
            cart.Order.Address = cart.Order.ApplicationUser.Address;
            cart.Order.PhoneNumber = cart.Order.ApplicationUser.PhoneNumber; 
            cart.Order.City = cart.Order.ApplicationUser.City;  

            // to calculate the total price
            foreach (var item in cart.CardList)
            {
                cart.TotalPrice += (item.Count * item.Product.Price);
            }
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
		public IActionResult Summary(ShoppingCartViewModel cart)
		{
            // get user id is active now from database using claims
            // Get the current user's ID from claims
            var claimasIdentity = (ClaimsIdentity)User.Identity;
			var claim = claimasIdentity.FindFirst(ClaimTypes.NameIdentifier);

            cart.CardList = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == claim.Value  , includeWord: "Product");

            // Fill data at Strip 
            // Fetch the user's shopping cart items
            if (cart.Order == null )
            {
                cart.Order = new Order();

            }
            // Set order details (mark as approved)

            cart.Order.OrderStatus = Helpers.Approve;
            cart.Order.PaymentStatus = Helpers.Approve;
            cart.Order.OrderDate = DateTime.Now;
            cart.Order.UserId = claim.Value;



            // to calculate the total price
            // Calculate the total price of the order

            foreach (var item in cart.CardList)
			{
				cart.TotalPrice += (item.Count * item.Product.Price);
			}
            cart.Order.TotalPrice = cart.TotalPrice;
            _unitOfWork.Order.Add(cart.Order);
            _unitOfWork.Complet();

            // Strip part "Payment"
            // Save each cart item as an order detail

            foreach (var item in cart.CardList)
            {
                var orderDetail = new OrderDetail()
                {
                    ProductId = item.ProductId,
                    OrderId = cart.Order.Id,
                    Price = item.Product.Price,
                    Count = item.Count,
                };
                _unitOfWork.OrderDetails.Add(orderDetail);
                _unitOfWork.Complet();
            }

            // will remove this part when larama need to turn on stripe
            // Clear the shopping cart for the user after the order is confirmed
            foreach (var item in cart.CardList)
            {
                _unitOfWork.ShoppingCart.Delete(item);
            }

            _unitOfWork.Complet();  // Commit the changes


            // part of stripe
            //var domain = "https://localhost:44317/";
            //var options = new SessionCreateOptions
            //{
            //    LineItems = new List<SessionLineItemOptions>(),
            //    Mode="payment",
            //    SuccessUrl = domain+$"customer/cart/orderconfirmation?id={cart.Order.Id}",
            //    CancelUrl= domain+$"customer/cart/index",

            //};

            //foreach (var item in cart.CardList)
            //{
            //    var sessionOptins = new SessionLineItemOptions
            //    {
            //        PriceData = new SessionLineItemPriceDataOptions
            //        {
            //            UnitAmount = (long)(item.Product.Price * 100),
            //            Currency = "usd",
            //            ProductData = new SessionLineItemPriceDataProductDataOptions
            //            {
            //                Name = item.Product.Name,
            //            },
            //        },
            //        Quantity = item.Count,
            //    };
            //    options.LineItems.Add(sessionOptins);
            //}


            //var servies = new SessionService();
            //Session session = servies.Create(options);
            //cart.Order.SessionId = session.Id;
            cart.Order.OrderDate = DateTime.UtcNow;

            _unitOfWork.Complet();

            //Response.Headers.Add("Location", session.Url);
            //return new StatusCodeResult(303);



            // Pass the order status to the view
            TempData["OrderStatusMessage"] = "طلبك في مرحله التجهيز و الاعداد ";

            // Redirect to the confirmation view
            return RedirectToAction("OrderConfirmation", new { id = cart.Order.Id });

        }
     
        public IActionResult OrderConfirmation(int id)
        {
            var order = _unitOfWork.Order.GetFirstOrDefualt(x => x.Id == id);
            //var servce = new SessionService();
            //Session session = servce.Get(order.SessionId);

            //if (session.PaymentStatus.ToLower() == "paid")
            //{
            //    _unitOfWork.Order.UpdateOderStatus(id, Helpers.Approve, Helpers.Approve);
            //     order.PaymentIntentId = session.PaymentIntentId;

            //    _unitOfWork.Complet();
            //}

            //List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart.GetAll(x => x.ApplicationUserId == order.UserId).ToList();
            //_unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            //_unitOfWork.Complet();
            if (order == null)
            {
                return NotFound();
            }
            ViewBag.OrderStatusMessage = TempData["OrderStatusMessage"]?.ToString();

            return View(order);
        }
    }
}
