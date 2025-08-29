using Microsoft.AspNetCore.Mvc;

namespace cutypai.Controllers;

public class AuthController : Controller
{
    // GET
    [Route("login")]
    public IActionResult Login() => View();
    
    [Route("signup")]
    public IActionResult Signup() => View();
}