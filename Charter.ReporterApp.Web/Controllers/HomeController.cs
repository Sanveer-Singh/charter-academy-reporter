using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Charter.ReporterApp.Web.Models;

namespace Charter.ReporterApp.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        // If user is authenticated, redirect to dashboard
        if (User.Identity?.IsAuthenticated ?? false)
        {
            return RedirectToAction("Index", "Dashboard");
        }
        
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
