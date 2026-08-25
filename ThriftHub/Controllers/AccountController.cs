using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThriftHub.Data;
using ThriftHub.Models;
using ThriftHub.Services;

namespace ThriftHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        // ============================================================
        // ADMIN EMAIL
        // ============================================================

        private const string AdminEmail =
            "antwiagyeibright9@gmail.com";


        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _emailSender = emailSender;
        }


        // ============================================================
        // REGISTER - GET
        // ============================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }


        // ============================================================
        // REGISTER - POST
        // ============================================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email =
                model.Email?
                    .Trim()
                    .ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Please enter your email address.");

                return View(model);
            }

            var existingUser =
                await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                if (!existingUser.EmailConfirmed)
                {
                    // The user registered but never confirmed their email.
                    // Let's delete this stale unconfirmed account so they can register fresh.
                    await _userManager.DeleteAsync(existingUser);
                }
                else
                {
                    ModelState.AddModelError(
                        "Email",
                        "An account with this email already exists.");

                    return View(model);
                }
            }


            // ========================================================
            // ACCOUNT TYPE
            // ========================================================

            var userType =
                model.UserType?.Trim();

            if (string.IsNullOrWhiteSpace(userType))
            {
                userType = "Customer";
            }

            if (userType.Equals(
                    "Admin",
                    StringComparison.OrdinalIgnoreCase) ||
                userType.Equals(
                    "Administrator",
                    StringComparison.OrdinalIgnoreCase))
            {
                userType = "Customer";
            }

            if (!userType.Equals(
                    "Customer",
                    StringComparison.OrdinalIgnoreCase) &&
                !userType.Equals(
                    "Seller",
                    StringComparison.OrdinalIgnoreCase))
            {
                userType = "Customer";
            }


            var idCardNumber = string.Empty;
            var frontExtension = string.Empty;
            var backExtension = string.Empty;

            if (userType.Equals("Seller", StringComparison.OrdinalIgnoreCase))
            {
                // ========================================================
                // ID TYPE
                // ========================================================

                if (string.IsNullOrWhiteSpace(
                        model.IdCardType))
                {
                    ModelState.AddModelError(
                        "IdCardType",
                        "Please select your ID type.");

                    return View(model);
                }


                // ========================================================
                // ID NUMBER
                // ========================================================

                idCardNumber =
                    model.IdCardNumber?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(
                        idCardNumber))
                {
                    ModelState.AddModelError(
                        "IdCardNumber",
                        "Please enter your ID card number.");

                    return View(model);
                }


                // ========================================================
                // ID FRONT
                // ========================================================

                if (model.IdCardFront == null ||
                    model.IdCardFront.Length == 0)
                {
                    ModelState.AddModelError(
                        "IdCardFront",
                        "Please upload the front of your ID card.");

                    return View(model);
                }


                // ========================================================
                // FILE VALIDATION
                // ========================================================

                var allowedExtensions =
                    new[]
                    {
                        ".jpg",
                        ".jpeg",
                        ".png",
                        ".webp",
                        ".pdf"
                    };

                const long maximumFileSize =
                    5 * 1024 * 1024;


                frontExtension =
                    Path.GetExtension(
                        model.IdCardFront.FileName)
                        .ToLowerInvariant();


                if (!allowedExtensions.Contains(
                        frontExtension))
                {
                    ModelState.AddModelError(
                        "IdCardFront",
                        "Invalid ID front file type. Please upload JPG, JPEG, PNG, WEBP or PDF.");

                    return View(model);
                }


                if (model.IdCardFront.Length >
                    maximumFileSize)
                {
                    ModelState.AddModelError(
                        "IdCardFront",
                        "The ID front file must not be larger than 5 MB.");

                    return View(model);
                }


                // ========================================================
                // ID BACK
                // ========================================================

                if (model.IdCardBack != null &&
                    model.IdCardBack.Length > 0)
                {
                    backExtension =
                        Path.GetExtension(
                            model.IdCardBack.FileName)
                            .ToLowerInvariant();


                    if (!allowedExtensions.Contains(
                            backExtension))
                    {
                        ModelState.AddModelError(
                            "IdCardBack",
                            "Invalid ID back file type. Please upload JPG, JPEG, PNG, WEBP or PDF.");

                        return View(model);
                    }


                    if (model.IdCardBack.Length >
                        maximumFileSize)
                    {
                        ModelState.AddModelError(
                            "IdCardBack",
                            "The ID back file must not be larger than 5 MB.");

                        return View(model);
                    }
                }
            }


            // ========================================================
            // CREATE USER
            // ========================================================

            var user =
                new ApplicationUser
                {
                    UserName = email,

                    Email = email,

                    PhoneNumber =
                        model.PhoneNumber?.Trim(),

                    FullName =
                        model.FullName?.Trim(),

                    Country =
                        model.Country?.Trim(),

                    City =
                        model.City?.Trim(),

                    UserType =
                        userType,

                    EmailConfirmed =
                        false,

                    IsVerified =
                        false,

                    VerificationStatus =
                        userType.Equals(
                            "Seller",
                            StringComparison.OrdinalIgnoreCase)
                            ? "Verification Pending"
                            : "NotSubmitted",

                    IdCardType =
                        model.IdCardType?.Trim() ?? string.Empty,

                    IdCardNumber =
                        idCardNumber,

                    IdCardVerified =
                        false,

                    SubscriptionWaived =
                        false,

                    IsOnline =
                        false,

                    IsSuspended =
                        false,

                    SuspendedAt =
                        null,

                    SuspensionReason =
                        null,

                    CreatedAt =
                        DateTime.UtcNow
                };


            // ========================================================
            // CREATE IDENTITY ACCOUNT
            // ========================================================

            var createResult =
                await _userManager.CreateAsync(
                    user,
                    model.Password);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View(model);
            }


            // ========================================================
            // SAVE ID DOCUMENTS
            // ========================================================

            var savedFrontPath =
                string.Empty;

            var savedBackPath =
                string.Empty;


            if (userType.Equals("Seller", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var idDirectory =
                        Path.Combine(
                            Directory.GetCurrentDirectory(),
                            "wwwroot",
                            "uploads",
                            "id-cards");


                    if (!Directory.Exists(idDirectory))
                    {
                        Directory.CreateDirectory(idDirectory);
                    }


                    // ----------------------------------------------------
                    // FRONT
                    // ----------------------------------------------------

                    var frontFileName =
                        Guid.NewGuid()
                            .ToString("N")
                        + frontExtension;


                    var frontFilePath =
                        Path.Combine(
                            idDirectory,
                            frontFileName);


                    await using (
                        var frontStream =
                            new FileStream(
                                frontFilePath,
                                FileMode.CreateNew))
                    {
                        await model.IdCardFront!.CopyToAsync(
                            frontStream);
                    }


                    savedFrontPath =
                        frontFilePath;


                    user.IdCardFrontUrl =
                        "/uploads/id-cards/" +
                        frontFileName;


                    // ----------------------------------------------------
                    // BACK
                    // ----------------------------------------------------

                    if (model.IdCardBack != null &&
                        model.IdCardBack.Length > 0 &&
                        !string.IsNullOrWhiteSpace(
                            backExtension))
                    {
                        var backFileName =
                            Guid.NewGuid()
                                .ToString("N")
                            + backExtension;


                        var backFilePath =
                            Path.Combine(
                                idDirectory,
                                backFileName);


                        await using (
                            var backStream =
                                new FileStream(
                                    backFilePath,
                                    FileMode.CreateNew))
                        {
                            await model.IdCardBack.CopyToAsync(
                                backStream);
                        }


                        savedBackPath =
                            backFilePath;


                        user.IdCardBackUrl =
                            "/uploads/id-cards/" +
                            backFileName;
                    }


                    var updateResult =
                        await _userManager.UpdateAsync(user);


                    if (!updateResult.Succeeded)
                    {
                        foreach (var error in updateResult.Errors)
                        {
                            ModelState.AddModelError(
                                "",
                                error.Description);
                        }

                        await _userManager.DeleteAsync(user);
                        return View(model);
                    }
                }
                catch
                {
                    await _userManager.DeleteAsync(user);


                    if (System.IO.File.Exists(
                            savedFrontPath))
                    {
                        try
                        {
                            System.IO.File.Delete(
                                savedFrontPath);
                        }
                        catch
                        {
                        }
                    }


                    if (System.IO.File.Exists(
                            savedBackPath))
                    {
                        try
                        {
                            System.IO.File.Delete(
                                savedBackPath);
                        }
                        catch
                        {
                        }
                    }


                    ModelState.AddModelError(
                        "",
                        "We could not save your verification ID documents. Please try again.");

                    return View(model);
                }
            }





            // ========================================================
            // EMAIL VERIFICATION CODE
            // ========================================================

            var verificationCode =
                Random.Shared.Next(
                    100000,
                    1000000)
                .ToString();


            user.EmailVerificationCode =
                verificationCode;


            user.EmailVerificationCodeExpiresAt =
                DateTime.UtcNow.AddMinutes(10);


            var verificationUpdateResult =
                await _userManager.UpdateAsync(user);


            if (!verificationUpdateResult.Succeeded)
            {
                foreach (var error in verificationUpdateResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }


                await _userManager.DeleteAsync(user);

                return View(model);
            }


            // ========================================================
            // EMAIL
            // ========================================================

            var emailSubject =
                "ThriftHub Email Verification Code";


            var safeFullName =
                System.Net.WebUtility.HtmlEncode(
                    user.FullName);


            var emailMessage = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>ThriftHub Email Verification</title>
</head>

<body style='font-family:Arial,sans-serif;'>

<div style='max-width:600px;
            margin:auto;
            padding:30px;
            border:1px solid #ddd;
            border-radius:10px;'>

    <h2 style='color:#6f42c1;'>
        Welcome to ThriftHub
    </h2>

    <p>
        Hello <strong>{safeFullName}</strong>,
    </p>

    <p>
        Thank you for creating your ThriftHub account.
    </p>

    <p>
        Your email verification code is:
    </p>

    <div style='font-size:32px;
                font-weight:bold;
                letter-spacing:8px;
                text-align:center;
                padding:20px;
                background:#f5f5f5;
                border-radius:8px;'>

        {verificationCode}

    </div>

    <p>
        This code will expire in
        <strong>10 minutes</strong>.
    </p>

    <p>
        Your identity document has also been submitted
        for ThriftHub account verification.
    </p>

    <p>
        Your ID information is private and will not be
        displayed publicly on your profile.
    </p>

    <p>
        Regards,<br>
        <strong>ThriftHub Team</strong>
    </p>

</div>

</body>
</html>
";


            try
            {
                await _emailSender.SendEmailAsync(
                    email,
                    emailSubject,
                    emailMessage);
            }
            catch
            {
                await _userManager.DeleteAsync(user);


                if (System.IO.File.Exists(
                        savedFrontPath))
                {
                    try
                    {
                        System.IO.File.Delete(
                            savedFrontPath);
                    }
                    catch
                    {
                    }
                }


                if (System.IO.File.Exists(
                        savedBackPath))
                {
                    try
                    {
                        System.IO.File.Delete(
                            savedBackPath);
                    }
                    catch
                    {
                    }
                }


                ModelState.AddModelError(
                    "",
                    "We could not send the verification email. Please check your email settings and try again.");

                return View(model);
            }


            return RedirectToAction(
                nameof(VerifyEmail),
                new
                {
                    email
                });
        }


        // ============================================================
        // VERIFY EMAIL - GET
        // ============================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult VerifyEmail(
            string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(
                    nameof(Register));
            }


            return View(
                new VerifyEmailModel
                {
                    Email =
                        email.Trim()
                            .ToLowerInvariant()
                });
        }


        // ============================================================
        // VERIFY EMAIL - POST
        // ============================================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyEmail(
            VerifyEmailModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var email =
                model.Email?
                    .Trim()
                    .ToLowerInvariant();


            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Email address is required.");

                return View(model);
            }


            var user =
                await _userManager.FindByEmailAsync(email);


            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Account not found.");

                return View(model);
            }


            if (user.EmailConfirmed)
            {
                TempData["SuccessMessage"] =
                    "Your email has already been verified.";

                return RedirectToAction(
                    nameof(Login));
            }


            if (string.IsNullOrWhiteSpace(
                    user.EmailVerificationCode))
            {
                ModelState.AddModelError(
                    "",
                    "No verification code was found. Please request a new code.");

                return View(model);
            }


            if (!user.EmailVerificationCodeExpiresAt.HasValue ||
                user.EmailVerificationCodeExpiresAt.Value <
                DateTime.UtcNow)
            {
                ModelState.AddModelError(
                    "",
                    "Your verification code has expired. Please request a new code.");

                return View(model);
            }


            if (!string.Equals(
                    user.EmailVerificationCode,
                    model.Code?.Trim(),
                    StringComparison.Ordinal))
            {
                ModelState.AddModelError(
                    "Code",
                    "Invalid verification code.");

                return View(model);
            }


            user.EmailConfirmed =
                true;

            user.EmailVerificationCode =
                null;

            user.EmailVerificationCodeExpiresAt =
                null;


            var updateResult =
                await _userManager.UpdateAsync(user);


            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(
                        "",
                        error.Description);
                }

                return View(model);
            }


            TempData["SuccessMessage"] =
                "Your email has been verified successfully. You can now log in.";


            return RedirectToAction(
                nameof(Login));
        }


        // ============================================================
        // RESEND VERIFICATION CODE - GET
        // ============================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResendVerificationCode(
            string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return RedirectToAction(
                    nameof(Register));
            }


            return View(
                new VerifyEmailModel
                {
                    Email =
                        email.Trim()
                });
        }


        // ============================================================
        // RESEND VERIFICATION CODE - POST
        // ============================================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerificationCode(
            VerifyEmailModel model)
        {
            var email =
                model.Email?
                    .Trim()
                    .ToLowerInvariant();


            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Please enter your email address.");

                return View(
                    "VerifyEmail",
                    model);
            }


            var user =
                await _userManager.FindByEmailAsync(email);


            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Account not found.");

                return View(
                    "VerifyEmail",
                    model);
            }


            if (user.EmailConfirmed)
            {
                TempData["SuccessMessage"] =
                    "Your email has already been verified.";

                return RedirectToAction(
                    nameof(Login));
            }


            var verificationCode =
                Random.Shared.Next(
                    100000,
                    1000000)
                .ToString();


            user.EmailVerificationCode =
                verificationCode;


            user.EmailVerificationCodeExpiresAt =
                DateTime.UtcNow.AddMinutes(10);


            var updateResult =
                await _userManager.UpdateAsync(user);


            if (!updateResult.Succeeded)
            {
                ModelState.AddModelError(
                    "",
                    "We could not generate a new verification code.");

                return View(
                    "VerifyEmail",
                    model);
            }


            var emailSubject =
                "Your New ThriftHub Verification Code";


            var emailMessage = $@"
<!DOCTYPE html>
<html>
<body style='font-family:Arial,sans-serif;'>

<div style='max-width:600px;
            margin:auto;
            padding:30px;'>

    <h2 style='color:#6f42c1;'>
        ThriftHub
    </h2>

    <p>
        Your new email verification code is:
    </p>

    <h1 style='letter-spacing:8px;
               text-align:center;'>
        {verificationCode}
    </h1>

    <p>
        This code expires in
        <strong>10 minutes</strong>.
    </p>

</div>

</body>
</html>
";


            try
            {
                await _emailSender.SendEmailAsync(
                    email,
                    emailSubject,
                    emailMessage);
            }
            catch
            {
                ModelState.AddModelError(
                    "",
                    "We could not send the verification email. Please check your email settings.");

                return View(
                    "VerifyEmail",
                    model);
            }


            TempData["SuccessMessage"] =
                "A new verification code has been sent to your email.";


            return RedirectToAction(
                nameof(VerifyEmail),
                new
                {
                    email
                });
        }


        // ============================================================
        // LOGIN - GET
        // ============================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] =
                returnUrl;

            return View(
                new LoginViewModel());
        }


        // ============================================================
        // LOGIN - POST
        // ============================================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model,
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] =
                returnUrl;


            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var email =
                model.Email?
                    .Trim()
                    .ToLowerInvariant();


            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "",
                    "Please enter your email address.");

                return View(model);
            }


            var user =
                await _userManager.FindByEmailAsync(email);


            if (user == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email or password.");

                return View(model);
            }


            // ========================================================
            // CHECK ACCOUNT SUSPENSION
            // ========================================================

            if (user.IsSuspended)
            {
                ModelState.AddModelError(
                    "",
                    string.IsNullOrWhiteSpace(
                        user.SuspensionReason)
                        ? "Your ThriftHub account has been suspended."
                        : $"Your ThriftHub account has been suspended. Reason: {user.SuspensionReason}");

                return View(model);
            }


            // ========================================================
            // ADMIN
            // ========================================================

            var isAdminEmail =
                email.Equals(
                    AdminEmail,
                    StringComparison.OrdinalIgnoreCase);


            var isAlreadyAdmin =
                await _userManager.IsInRoleAsync(
                    user,
                    "Admin");


            var isAdmin =
                isAdminEmail ||
                isAlreadyAdmin;


            if (isAdmin)
            {
                if (!await _roleManager.RoleExistsAsync(
                        "Admin"))
                {
                    var createRoleResult =
                        await _roleManager.CreateAsync(
                            new IdentityRole("Admin"));


                    if (!createRoleResult.Succeeded)
                    {
                        foreach (var error in createRoleResult.Errors)
                        {
                            ModelState.AddModelError(
                                "",
                                error.Description);
                        }

                        return View(model);
                    }
                }


                if (!await _userManager.IsInRoleAsync(
                        user,
                        "Admin"))
                {
                    var addRoleResult =
                        await _userManager.AddToRoleAsync(
                            user,
                            "Admin");


                    if (!addRoleResult.Succeeded)
                    {
                        foreach (var error in addRoleResult.Errors)
                        {
                            ModelState.AddModelError(
                                "",
                                error.Description);
                        }

                        return View(model);
                    }
                }


                var adminChanged =
                    false;


                if (user.UserType != "Admin")
                {
                    user.UserType =
                        "Admin";

                    adminChanged =
                        true;
                }


                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed =
                        true;

                    adminChanged =
                        true;
                }


                if (!user.IsVerified)
                {
                    user.IsVerified =
                        true;

                    adminChanged =
                        true;
                }


                if (!string.Equals(
                        user.VerificationStatus,
                        "Approved",
                        StringComparison.OrdinalIgnoreCase))
                {
                    user.VerificationStatus =
                        "Approved";

                    adminChanged =
                        true;
                }


                if (adminChanged)
                {
                    var updateAdminResult =
                        await _userManager.UpdateAsync(user);


                    if (!updateAdminResult.Succeeded)
                    {
                        foreach (
                            var error
                            in updateAdminResult.Errors)
                        {
                            ModelState.AddModelError(
                                "",
                                error.Description);
                        }

                        return View(model);
                    }
                }


                var adminPasswordCorrect =
                    await _userManager.CheckPasswordAsync(
                        user,
                        model.Password);


                if (!adminPasswordCorrect)
                {
                    ModelState.AddModelError(
                        "",
                        "Invalid email or password.");

                    return View(model);
                }


                await _signInManager.SignInAsync(
                    user,
                    model.RememberMe);


                user.IsOnline =
                    true;


                await _userManager.UpdateAsync(user);


                return RedirectToAction(
                    "Index",
                    "Admin");
            }


            // ========================================================
            // NORMAL USER
            // ========================================================

            if (!user.EmailConfirmed)
            {
                return RedirectToAction(
                    nameof(VerifyEmail),
                    new
                    {
                        email = user.Email
                    });
            }


            var result =
                await _signInManager.PasswordSignInAsync(
                    user,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);


            if (result.Succeeded)
            {
                // ----------------------------------------------------
                // CHECK SUSPENSION AGAIN
                // ----------------------------------------------------

                var refreshedUser =
                    await _userManager.FindByEmailAsync(email);


                if (refreshedUser != null &&
                    refreshedUser.IsSuspended)
                {
                    await _signInManager.SignOutAsync();

                    ModelState.AddModelError(
                        "",
                        string.IsNullOrWhiteSpace(
                            refreshedUser.SuspensionReason)
                            ? "Your ThriftHub account has been suspended."
                            : $"Your ThriftHub account has been suspended. Reason: {refreshedUser.SuspensionReason}");

                    return View(model);
                }


                user.IsOnline =
                    true;


                await _userManager.UpdateAsync(user);


                if (!string.IsNullOrWhiteSpace(returnUrl) &&
                    Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }


                return RedirectToAction(
                    "Index",
                    "Dashboard");
            }


            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    "",
                    "Your account is temporarily locked.");

                return View(model);
            }


            if (result.IsNotAllowed)
            {
                ModelState.AddModelError(
                    "",
                    "Your account is not currently allowed to log in.");

                return View(model);
            }


            ModelState.AddModelError(
                "",
                "Invalid email or password.");


            return View(model);
        }


        // ============================================================
        // PROFILE
        // ============================================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user =
                await _userManager.GetUserAsync(User);


            if (user == null)
            {
                await _signInManager.SignOutAsync();

                return RedirectToAction(
                    nameof(Login));
            }


            if (user.IsSuspended)
            {
                await _signInManager.SignOutAsync();

                TempData["ErrorMessage"] =
                    string.IsNullOrWhiteSpace(
                        user.SuspensionReason)
                        ? "Your ThriftHub account has been suspended."
                        : $"Your ThriftHub account has been suspended. Reason: {user.SuspensionReason}";

                return RedirectToAction(
                    nameof(Login));
            }


            return View(user);
        }


        // ============================================================
        // EDIT PROFILE - GET
        // ============================================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile()
        {
            var user =
                await _userManager.GetUserAsync(User);


            if (user == null)
            {
                return RedirectToAction(
                    nameof(Login));
            }


            if (user.IsSuspended)
            {
                await _signInManager.SignOutAsync();

                TempData["ErrorMessage"] =
                    string.IsNullOrWhiteSpace(
                        user.SuspensionReason)
                        ? "Your ThriftHub account has been suspended."
                        : $"Your ThriftHub account has been suspended. Reason: {user.SuspensionReason}";

                return RedirectToAction(
                    nameof(Login));
            }


            var model =
                new EditProfileViewModel
                {
                    FullName =
                        user.FullName,

                    Country =
                        user.Country,

                    City =
                        user.City,

                    InstagramUrl =
                        user.InstagramUrl,

                    TikTokUrl =
                        user.TikTokUrl,

                    FacebookUrl =
                        user.FacebookUrl,

                    XUrl =
                        user.XUrl,

                    WhatsAppUrl =
                        user.WhatsAppUrl,

                    YouTubeUrl =
                        user.YouTubeUrl,

                    WebsiteUrl =
                        user.WebsiteUrl
                };


            return View(model);
        }


        // ============================================================
        // EDIT PROFILE - POST
        // ============================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(
            EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            var user =
                await _userManager.GetUserAsync(User);


            if (user == null)
            {
                return RedirectToAction(
                    nameof(Login));
            }


            if (user.IsSuspended)
            {
                await _signInManager.SignOutAsync();

                TempData["ErrorMessage"] =
                    string.IsNullOrWhiteSpace(
                        user.SuspensionReason)
                        ? "Your ThriftHub account has been suspended."
                        : $"Your ThriftHub account has been suspended. Reason: {user.SuspensionReason}";

                return RedirectToAction(
                    nameof(Login));
            }


            user.FullName =
                model.FullName;

            user.Country =
                model.Country;

            user.City =
                model.City;

            user.InstagramUrl =
                model.InstagramUrl;

            user.TikTokUrl =
                model.TikTokUrl;

            user.FacebookUrl =
                model.FacebookUrl;

            user.XUrl =
                model.XUrl;

            user.WhatsAppUrl =
                model.WhatsAppUrl;

            user.YouTubeUrl =
                model.YouTubeUrl;

            user.WebsiteUrl =
                model.WebsiteUrl;


            var result =
                await _userManager.UpdateAsync(user);


            if (result.Succeeded)
            {
                TempData["SuccessMessage"] =
                    "Your profile has been updated successfully.";

                return RedirectToAction(
                    nameof(Profile));
            }


            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    "",
                    error.Description);
            }


            return View(model);
        }


        // ============================================================
        // SELLER VERIFICATION - GET
        // ============================================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> SellerVerification()
        {
            var user =
                await _userManager.GetUserAsync(User);


            if (user == null)
            {
                return RedirectToAction(
                    nameof(Login));
            }


            if (user.IsSuspended)
            {
                await _signInManager.SignOutAsync();

                TempData["ErrorMessage"] =
                    string.IsNullOrWhiteSpace(
                        user.SuspensionReason)
                        ? "Your ThriftHub account has been suspended."
                        : $"Your ThriftHub account has been suspended. Reason: {user.SuspensionReason}";

                return RedirectToAction(
                    nameof(Login));
            }


            var isAdmin =
                user.UserType == "Admin" ||
                await _userManager.IsInRoleAsync(
                    user,
                    "Admin");


            if (isAdmin)
            {
                return RedirectToAction(
                    "Index",
                    "Admin");
            }


            var activeSubscription =
                await _context.SellerSubscriptions
                    .Where(s =>
                        s.SellerId == user.Id &&
                        s.Status == "Active" &&
                        s.PaymentStatus == "Paid" &&
                        s.EndDate > DateTime.UtcNow)
                    .OrderByDescending(
                        s => s.EndDate)
                    .FirstOrDefaultAsync();


            ViewBag.ActiveSubscription =
                activeSubscription;


            ViewBag.HasActiveSubscription =
                activeSubscription != null;


            return View(user);
        }


        // ============================================================
        // SUBMIT SELLER VERIFICATION
        // ============================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            SellerVerificationSubmit()
        {
            var user =
                await _userManager.GetUserAsync(User);


            if (user == null)
            {
                return RedirectToAction(
                    nameof(Login));
            }


            if (user.IsSuspended)
            {
                await _signInManager.SignOutAsync();

                TempData["ErrorMessage"] =
                    string.IsNullOrWhiteSpace(
                        user.SuspensionReason)
                        ? "Your ThriftHub account has been suspended."
                        : $"Your ThriftHub account has been suspended. Reason: {user.SuspensionReason}";

                return RedirectToAction(
                    nameof(Login));
            }


            var isAdmin =
                user.UserType == "Admin" ||
                await _userManager.IsInRoleAsync(
                    user,
                    "Admin");


            if (isAdmin)
            {
                TempData["ErrorMessage"] =
                    "Admin accounts do not require seller verification.";

                return RedirectToAction(
                    "Index",
                    "Admin");
            }


            if (!user.EmailConfirmed)
            {
                TempData["ErrorMessage"] =
                    "Please verify your email address before applying to become a seller.";

                return RedirectToAction(
                    nameof(SellerVerification));
            }


            if (string.IsNullOrWhiteSpace(user.IdCardType) ||
                string.IsNullOrWhiteSpace(user.IdCardNumber) ||
                string.IsNullOrWhiteSpace(user.IdCardFrontUrl))
            {
                TempData["ErrorMessage"] =
                    "Your identity document information is incomplete.";

                return RedirectToAction(
                    nameof(SellerVerification));
            }


            if (user.IsVerified &&
                string.Equals(
                    user.VerificationStatus,
                    "Approved",
                    StringComparison.OrdinalIgnoreCase))
            {
                TempData["SuccessMessage"] =
                    "Your seller account has already been approved.";

                return RedirectToAction(
                    nameof(SellerVerification));
            }


            user.VerificationStatus =
                "Verification Pending";

            user.IsVerified =
                false;


            var result =
                await _userManager.UpdateAsync(user);


            if (result.Succeeded)
            {
                TempData["SuccessMessage"] =
                    "Your seller verification has been submitted successfully. An administrator will review your application.";
            }
            else
            {
                TempData["ErrorMessage"] =
                    "Your verification could not be submitted.";

                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] +=
                        $" {error.Description}";
                }
            }


            return RedirectToAction(
                nameof(SellerVerification));
        }


        // ============================================================
        // LOGOUT
        // ============================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user =
                await _userManager.GetUserAsync(User);


            if (user != null)
            {
                user.IsOnline =
                    false;

                await _userManager.UpdateAsync(user);
            }


            await _signInManager.SignOutAsync();


            return RedirectToAction(
                "Index",
                "Home");
        }


        // ============================================================
        // ACCESS DENIED
        // ============================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied(
            string? returnUrl = null)
        {
            ViewData["ReturnUrl"] =
                returnUrl;

            return View();
        }
    }
}