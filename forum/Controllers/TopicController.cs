using forum.Models;
using forum.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace forum.Controllers;

public class TopicController : Controller
{
    private readonly TopicDbContext _topicDbContext;

    public TopicController(TopicDbContext topicDbContext)
    {
        _topicDbContext = topicDbContext;
    }
    
    // GET
    public IActionResult Topics()
    {
        List<Topic> topics = _topicDbContext.Topics.ToList();
        var topicListViewModel = new TopicListViewModel(topics);
        return View(topicListViewModel);
    }
}