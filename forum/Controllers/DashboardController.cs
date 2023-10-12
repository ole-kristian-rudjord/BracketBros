﻿using System.Security.Claims;
using forum.DAL;
using forum.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace forum.Controllers;

public class DashBoardController : Controller
{
    private readonly ILogger<ApplicationUser> _logger; // Ikke satt opp enda!

    // Connect the controller to the different models
    private readonly IForumRepository<ApplicationUser> _userRepository;

    // Constructor for Dependency Injection to the Data Access Layer from the different repositories
    public DashBoardController(
        IForumRepository<ApplicationUser> userRepository,
        ILogger<ApplicationUser> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public string GetUserId()
    {
        //https://stackoverflow.com/questions/29485285/can-not-find-user-identity-getuserid-method
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }


    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        // Get all activity for the user
        var userActivity = await _userRepository.GetUserActivity(GetUserId());

        // If no posts, return NotFound
        if (userActivity == null)
        {
            _logger.LogError($"[Dashboard controller] Dashboard() failed, error message: userActivity is null");
            return NotFound("Post list not found");
        }

        return View(userActivity);
    }
}