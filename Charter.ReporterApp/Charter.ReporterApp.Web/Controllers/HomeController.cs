using Charter.ReporterApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Charter.ReporterApp.Web.Controllers;

/// <summary>
/// Home controller for public pages
/// </summary>
public class HomeController : BaseController
{
    public HomeController(
        ILogger<HomeController> logger,
        IAuditService auditService,
        ISecurityValidationService securityService)
        : base(logger, auditService, securityService)
    {
    }

    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToRoleDashboard();
        }

        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}