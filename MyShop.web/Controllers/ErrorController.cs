using Microsoft.AspNetCore.Mvc;

namespace MyShop.web.Controllers
{
    public class ErrorController : Controller
    {
        // Action for the "Not Found" page
        [Route("Error/NotFound")]
        public IActionResult NotFound()
        {
            return View();
        }
    }
}
