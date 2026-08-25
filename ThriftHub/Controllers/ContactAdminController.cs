using Microsoft.AspNetCore.Mvc;

namespace ThriftHub.Controllers
{
    public class ContactAdminController : Controller
    {
        // ============================================================
        // CONTACT ADMIN PAGE
        // ============================================================

        [HttpGet]
        public IActionResult Index()
        {
            return View("~/Views/ContactAdmin/Index.cshtml");
        }
    }
}