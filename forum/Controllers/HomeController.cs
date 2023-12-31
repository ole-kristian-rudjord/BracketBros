using System.Security.Claims;
using forum.DAL;
using forum.Models;
using forum.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace forum.Controllers;

// Controller for the homepage
public class HomeController : Controller
{
    // Connect the controller to the different models
    private readonly IForumRepository<Category> _categoryRepository;
    private readonly ILogger<HomeController> _logger;
    private readonly IForumRepository<Tag> _tagsRepository;

    // Constructor for Dependency Injection to the Data Access Layer from the different repositories
    public HomeController(IForumRepository<Category> categoryRepository,
        IForumRepository<Tag> tagsRepository,
        ILogger<HomeController> logger)
    {
        _categoryRepository = categoryRepository;
        _tagsRepository = tagsRepository;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public string GetUserId()
    {
        //https://stackoverflow.com/questions/29485285/can-not-find-user-identity-getuserid-method
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    // Sends user to index
    public async Task<IActionResult> Index()
    {
        // Fetching the categories and tags data
        var categories = await _categoryRepository.GetAll();
        var tags = await _tagsRepository.GetAll();

        // Exception if there are no tags or categories to show the user
        if (categories == null || tags == null)
        {
            _logger.LogError("[Home controller] Index() failed, error message: Categories or tags not found");
            TempData["error"] = "Categories or tags not found, cannot show view";
            return View(new DashboardViewModel
            {
                CategoryList = new List<Category>(),
                TagList = new List<Tag>()
            });
        }

        // New view model for creating a post
        var dashboardViewModel = new DashboardViewModel
        {
            CategoryList = categories,
            TagList = tags
        };

        return View(dashboardViewModel);
    }
}