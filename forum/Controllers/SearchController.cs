using System.ComponentModel;
using forum.DAL;
using Microsoft.AspNetCore.Mvc;
using forum.Models;
using forum.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace forum.Controllers;

// Controller for the search function
public class SearchController : Controller
{
    // Connect the controller to the different models
    private readonly IForumRepository<Post> _postRepository;
    private readonly IForumRepository<Category> _categoryRepository;
    private readonly IForumRepository<Tag> _tags;
    private readonly IForumRepository<Comment> _commentRepository;

    private readonly ILogger<PostController> _logger; // Ikke satt opp enda!

    // Constructor for Dependency Injection to the Data Access Layer from the different repositories
    public SearchController(IForumRepository<Category> categoryRepository,
        IForumRepository<Tag> tagRepo, IForumRepository<Post> postRepository,
        IForumRepository<Comment> commentRepository,
        ILogger<PostController> logger)
    {
        _categoryRepository = categoryRepository;
        _tags = tagRepo;
        _postRepository = postRepository;
        _commentRepository = commentRepository;
        _logger = logger;
    }

    // Function to refresh
    // NOT USED?????
    public IActionResult Refresh()
    {
        return Redirect(Request.Headers["Referer"].ToString());
    }
    
    // Function to go to post based on id
    public IActionResult GoToPost(int id)
    {
        return RedirectToAction("Post", "Post", new { id });
    }

    // Get request to search based on a provided search term
    [HttpGet]
    public async Task<IActionResult> Search(string term)
    {
        // Error handling for the search term
        if (string.IsNullOrWhiteSpace(term)) return Index(); // TODO: Redirect to error page
        if (term.Length < 2) return Index(); // TODO: Redirect to error page

        // Fetch all posts based on the search term
        var posts = await _postRepository.GetAllPostsByTerm(term);
        
        // Error handling if the term does not provide any posts
        if (posts == null)
        {
            _logger.LogError("[ItemController] Item list not found while executing _itemRepository.GetAll()");
            return NotFound("Item list not found");
        }

        // Return view with all the posts matching the search term
        var postsListViewModel = new PostsListViewModel(posts, "Search");
        return View(postsListViewModel);
    }

    // Sends user to index
    public IActionResult Index()
    {
        return View();
    }
}