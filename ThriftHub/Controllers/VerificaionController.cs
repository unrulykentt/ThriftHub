using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ThriftHub.Models;

namespace ThriftHub.Controllers
{
    [Authorize]
    public class VerificationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public VerificationController(
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _environment = environment;
        }


        // GET: /Verification
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }

            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email;
            ViewBag.UserType = user.UserType;
            ViewBag.IsVerified = user.IsVerified;
            ViewBag.VerificationStatus = user.VerificationStatus;

            return View();
        }


        // POST: /Verification/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(IFormFile? identificationDocument)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge();
            }


            if (identificationDocument == null ||
                identificationDocument.Length == 0)
            {
                TempData["ErrorMessage"] =
                    "Please select an identification document.";

                return RedirectToAction(nameof(Index));
            }


            // Allowed file types
            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".pdf"
            };


            var extension =
                Path.GetExtension(
                    identificationDocument.FileName)
                .ToLowerInvariant();


            if (!allowedExtensions.Contains(extension))
            {
                TempData["ErrorMessage"] =
                    "Only JPG, JPEG, PNG and PDF files are allowed.";

                return RedirectToAction(nameof(Index));
            }


            // Maximum file size: 5 MB
            if (identificationDocument.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] =
                    "The identification document must not exceed 5 MB.";

                return RedirectToAction(nameof(Index));
            }


            // Create verification folder
            var uploadFolder =
                Path.Combine(
                    _environment.WebRootPath,
                    "Uploads",
                    "Verification");


            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }


            // Generate safe unique filename
            var uniqueFileName =
                $"{Guid.NewGuid()}{extension}";


            var filePath =
                Path.Combine(
                    uploadFolder,
                    uniqueFileName);


            // Save file
            using (var stream =
                   new FileStream(
                       filePath,
                       FileMode.Create))
            {
                await identificationDocument.CopyToAsync(stream);
            }


            // Update verification status
            user.VerificationStatus = "Submitted";
            user.IsVerified = false;

            await _userManager.UpdateAsync(user);


            TempData["SuccessMessage"] =
                "Your identification document has been submitted successfully. Your account is now awaiting verification.";


            return RedirectToAction(nameof(Index));
        }
    }
}