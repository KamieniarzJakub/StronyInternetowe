using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TournamentSystem.Models;
using TournamentSystem.ViewModels;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
    }

    [HttpGet]
    public IActionResult Register(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            if (await _userManager.IsEmailConfirmedAsync(existingUser))
            {
                ModelState.AddModelError("", "Użytkownik o tym adresie email już istnieje.");
                return View(model);
            }
            else
            {
                var deleteResult = await _userManager.DeleteAsync(existingUser);
                if (!deleteResult.Succeeded)
                {
                    ModelState.AddModelError("", "Wystąpił błąd podczas rejestracji. Spróbuj ponownie.");
                    return View(model);
                }
            }
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action(
                nameof(ConfirmEmail),
                "Account",
                new { userId = user.Id, token, returnUrl },
                Request.Scheme);

            await _emailSender.SendEmailAsync(
                user.Email,
                "Potwierdź rejestrację",
                $"Witaj {user.FirstName},<br/>" +
                $"Kliknij w link, aby potwierdzić konto: <a href='{confirmationLink}'>Potwierdź email</a><br/>" +
                $"Link jest ważny 24 godziny.");

            // Przekieruj na stronę z info o wysłaniu maila, przekazując returnUrl, by ją zachować
            return RedirectToAction(nameof(RegisterConfirmation), new { returnUrl });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult RegisterConfirmation(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string userId, string token, string returnUrl = null)
    {
        if (userId == null || token == null)
            return BadRequest();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var result = await _userManager.ConfirmEmailAsync(user, token);

        if (result.Succeeded)
        {
            // Po potwierdzeniu przekieruj na returnUrl jeśli jest lokalny, albo domyślnie na Home
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return View("ConfirmEmailSuccess");
        }
        else
            return View("Error");
    }

    [HttpGet]
    public IActionResult Login(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null)
        {
            ModelState.AddModelError("", "Nieprawidłowy login lub hasło.");
            return View(model);
        }

        if (!user.EmailConfirmed)
        {
            ModelState.AddModelError("", "Musisz potwierdzić swój email, aby się zalogować.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName,
            model.Password,
            model.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError("", "Nieprawidłowy login lub hasło.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendPasswordResetLink(string email, string returnUrl = null)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user != null && await _userManager.IsEmailConfirmedAsync(user))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var callbackUrl = Url.Action(
                nameof(ResetPassword),
                "Account",
                new { token = encodedToken, email = user.Email, returnUrl },
                Request.Scheme);

            await _emailSender.SendEmailAsync(
                email,
                "Resetowanie hasła",
                $"<p>Aby zresetować swoje hasło, kliknij poniższy link:</p>" +
                $"<p><a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>Resetuj hasło</a></p>");
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    public IActionResult ForgotPassword(string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
        {
            TempData["ResetEmail"] = model.Email;
            TempData["UserExists"] = false;
        }
        else
        {
            TempData["ResetEmail"] = model.Email;
            TempData["UserExists"] = true;
        }

        return RedirectToAction(nameof(ForgotPasswordConfirmRequest));
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmRequest()
    {
        ViewBag.Email = TempData["ResetEmail"] as string;
        ViewBag.UserExists = TempData["UserExists"] as bool? ?? false;
        return View();
    }

    [HttpGet]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ResetPassword(string token, string email, string returnUrl = null)
    {
        if (token == null || email == null)
            return BadRequest("Brak tokena lub emaila");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new ResetPasswordViewModel { Token = token, Email = email });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Token));
        var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(string returnUrl = null)
    {
        await _signInManager.SignOutAsync();

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }
}
