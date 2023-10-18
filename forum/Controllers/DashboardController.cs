﻿using System.Security.Claims;
using forum.DAL;
using forum.Models;
using forum.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace forum.Controllers;

public class DashBoardController : Controller
{
    private readonly ILogger<ApplicationUser> _logger; // Ikke satt opp enda!

    // Connect the controller to the different models
    private readonly IForumRepository<ApplicationUser> _userRepository;
    private readonly IForumRepository<Category> _categoryRepository;
    private readonly IForumRepository<Tag> _tagsRepository;


    // Constructor for Dependency Injection to the Data Access Layer from the different repositories
    public DashBoardController(
        IForumRepository<ApplicationUser> userRepository, IForumRepository<Category> categoryRepository
        , IForumRepository<Tag> tags,
        ILogger<ApplicationUser> logger)
    {
        _userRepository = userRepository;
        _categoryRepository = categoryRepository;
        _tagsRepository = tags;
        _logger = logger;
    }

    // Get request to fetch user identity
    [HttpGet]
    [Authorize]
    public string GetUserId()
    {
        //https://stackoverflow.com/questions/29485285/can-not-find-user-identity-getuserid-method
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    // Get request to fetch the Dashboard view
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        // Get all activity for the user
        var userActivity = await _userRepository.GetUserActivity(GetUserId());

        // If no posts, return NotFound and log error
        if (userActivity == null)
        {
            _logger.LogError($"[Dashboard controller] Dashboard() failed, error message: userActivity is null");
            return NotFound("User data not found");
        }

        return View(userActivity);
    }
    
    // Method for fetching the admin dashboard view
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminDashboard()
    {
        // Initialize categories and tags
        IEnumerable<Category>? categories = null;
        IEnumerable<Tag>? tags = null;

        try
        {
            // Fetching the categories and tags data
            categories = await _categoryRepository.GetAll();
            tags = await _tagsRepository.GetAll();
        }
        catch (Exception e)
        {
            // Exception and error logging if the server can't fetch the data
            _logger.LogError("[Dashboard controller] An exception occurred while fetching categories or tags: {e}", e.Message);
            return StatusCode(500, "Internal server error. Please try again later.");
        }
        
        // Exception and error logging if there are no tags or categories to show the user
        if (categories == null || tags == null)
        {
            _logger.LogError(
                "[Dashboard controller] _categoryRepository.GetAll() and/or _tagsRepository.GetAll() returned null");
            return NotFound("Categories or tags not found, cannot create post"); 
        }
        
        // New view model for creating a post
        var adminDashboardViewModel = new DashboardViewModel
        {
            CategoryList = categories,
            TagList = tags
        };

        return View(adminDashboardViewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(Category category)
    {
        if (ModelState.IsValid)
        {
            await _categoryRepository.Update(category);
        }

        return RedirectToAction("AdminDashboard");
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> NewCategory(Category category)
    {
        if (ModelState.IsValid)
        {
            // If the user has selected a file
            if (Request.Form.Files.Count > 0) // If the user has selected a file
            {
                if (!await FileUpload(category))
                {
                    _logger.LogError($"[Dashboard controller] NewCategory() failed, error message: fileUpload failed");
                    return NotFound("Category not found");
                }
            }

            var result = await _categoryRepository.Create(category);

            if (result == null)
            {
                _logger.LogError($"[Dashboard controller] NewCategory() failed, error message: result is null");
                return NotFound("Category not found");
            }
        }

        return RedirectToAction("AdminDashboard");
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _categoryRepository.GetTById(id);


        if (category == null)
        {
            _logger.LogError($"[Dashboard controller] DeleteCategory() failed, error message: category is null");
            return NotFound("Category not found");
        }

        // Delete the old picture if it exists
        if (category.PicturePath != null)
        {
            string deletePath = category.PicturePath;

            //Replace the ../ with wwwroot/ to get the correct path
            deletePath = deletePath.Replace("../", "wwwroot/");

            Console.WriteLine("Delete path " + deletePath);

            // Check if the file exists
            if (System.IO.File.Exists(deletePath))
            {
                // Delete the file
                System.IO.File.Delete(deletePath);
                //https://learn.microsoft.com/en-us/dotnet/api/system.io.file.delete?view=net-7.0#system-io-file-delete(system-string)

                if (System.IO.File.Exists(deletePath))
                {
                    _logger.LogError(
                        $"[Dashboard controller] DeleteCategory() failed, error message: could not delete file");
                }
            }
        }


        /*var result = await _categoryRepository.Delete(id);
        if (!result)
        {
            _logger.LogError($"[Dashboard controller] DeleteCategory() failed, error message: result is null");
            return NotFound("Category not found");
        }*/

        return RedirectToAction("AdminDashboard");
    }

    public async Task<bool> FileUpload(Category category)
    {
        // Delete the old picture if it exists
        if (category.PicturePath != null)
        {
            try
            {
                string deletePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "categories",
                    category.PicturePath);

                // Check if the file exists
                if (Directory.Exists(Path.GetDirectoryName(deletePath)))
                {
                    // Delete the file
                    Directory.Delete(Path.GetDirectoryName(deletePath) ?? throw new InvalidOperationException(), true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e); // TODO: Logg exception
                return false;
            }
        }


        // Get the file
        var file = Request.Form.Files.FirstOrDefault();

        // If the file is null or empty
        if (file == null || file.Length == 0)
        {
            return false;
        }

        long maxSize = 8 * 1024 * 1024; // 8MB

        if (file.Length > maxSize) // If the file is greater than 8MB
        {
            return false;
        }

        // Create a new file name with a GUID and the file extension
        string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);

        // Create the path to the file
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "categories",
            fileName);

        // If the directory does not exist, create it
        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        }

        // Copy the file to the path
        await using (FileStream fs = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(fs);
        }

        //category.PicturePath = filePath;

        category.PicturePath = "../images/categories/" + fileName;

        Console.WriteLine("File uploaded successfully");


        Console.WriteLine("Picture path " + category.PicturePath);

        //Set the PictureBytes to null, since we don't want to save the image in the database
        category.PictureBytes = null;

        return true;
    }

    // Post request to update an existing tag in the repo and redirects to the admin dashboard
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateTag(Tag tag)
    {
        // Checks if the submitted form values passes validation
        if (ModelState.IsValid)
        {
            // Tries to update tag in repo, logs error if it cannot update tag
            bool updated = await _tagsRepository.Update(tag);
            if (!updated)
            {
                TempData["ErrorMessage"] = "Tag update failed.";
                _logger.LogWarning("[Dashboard controller] Tag update failed for {@tag}", tag); 
            }
        }

        TempData["TestMessage"] = "This is a test, babe.";
        // Redirects to admin dashboard
        return RedirectToAction("AdminDashboard"); 
    }

    // Post request to create a new tag in the repo and redirect to admin dashboard
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> NewTag(Category tag)
    {
        // Checks if the submitted form values passes validation
        if (ModelState.IsValid)
        {
            // Tries to create tag in repo, logs error if it cannot create tag 
            var newCategory = await _categoryRepository.Create(tag);
            if (newCategory == null)
                _logger.LogWarning("[Dashboard controller] Tag creation failed for {@tag}", tag);
        }
        
        // Redirects to admin dashboard
        return RedirectToAction("AdminDashboard");
    }

    // Get request to delete a tag in the repo
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteTag(int id)
    {
        // Tries to delete the tag, logs and returns error if there is no category to delete
        var result = await _tagsRepository.Delete(id);
        if (!result)
        {
            _logger.LogError($"[Dashboard controller] DeleteCategory() failed, error message: result is null");
            return NotFound("Category not found");
        }

        // Redirects to admin dashboard
        return RedirectToAction("AdminDashboard");
    }
}