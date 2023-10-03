﻿using Microsoft.EntityFrameworkCore;
using forum.Models;

namespace forum.DAL;

//Source: https://dotnettutorials.net/lesson/generic-repository-pattern-csharp-mvc/
//Source: https://learn.microsoft.com/en-us/aspnet/mvc/overview/older-versions/getting-started-with-ef-5-using-mvc-4/implementing-the-repository-and-unit-of-work-patterns-in-an-asp-net-mvc-application#implement-a-generic-repository-and-a-unit-of-work-class

// A generic repository class for the forum
public class ForumRepository<TEntity> : IForumRepository<TEntity> where TEntity : class
{
    // DB session and logger
    private readonly ForumDbContext _db;
    private readonly ILogger<ForumRepository<TEntity>> _logger;

    // Constructor for initializing the logger
    public ForumRepository(ForumDbContext db, ILogger<ForumRepository<TEntity>> logger)
    {
        _db = db;
        _logger = logger;
    }

    // Fetches all entities from the database
    public async Task<IEnumerable<TEntity>?> GetAll()
    {
        // Tries to retrieve all records from the database as a list
        try
        {
            return await _db.Set<TEntity>().ToListAsync();
        }
        // Exception error handling if it can't fetch entities from the database
        catch (Exception e)
        {
            LogError("GetAll", e);
            return null;
        }
    }

    public async Task<ApplicationUser?> GetUserActivity(string userid)
    {
        // Tries to retrieve all activity from the database as object
        try
        {
            return await _db.Set<ApplicationUser>().Include(user => user.Posts).Include(user => user.Comments)
                .Include(user => user.LikedPosts).Include(user => user.LikedComments)
                .Where(user => user.Id == userid).FirstAsync();
        }
        // Exception error handling if it can't fetch entities from the database
        catch (Exception e)
        {
            LogError("GetUserActivity", e);
            return null;
        }
    }


    // Get all posts from database, based on a search term 
    public async Task<IEnumerable<Post>?> GetAllPostsByTerm(string term)
    {
        try
        {
            // Make the search term lowercase
            term = term.ToLower();

            // Search in title, content, tags and comments (might be costly)
            var result = await _db.Posts
                .Include(post => post.Tags)
                .Include(post => post.Category)
                .Include(post => post.Comments)
                .Include(post => post.User)
                .Where(post =>
                    ((post.Title.ToLower().Contains(term) || post.Content.ToLower().Contains(term)) ||
                     post.Category!.Name.ToLower().Contains(term) ||
                     post.Tags!.Any(tag => tag.Name!.ToLower().Contains(term)) ||
                     post.Comments!.Any(comment => comment.Content.ToLower().Contains(term)) ||
                     post.User!.UserName.ToLower().Contains(term)
                    )
                )
                .ToListAsync();

            return result;
        }
        // Error handling if it can't fetch posts from the search term
        catch (Exception e)
        {
            LogError("GetAllPostsByTerm", e);
            return null;
        }
    }

    // Fetches posts from the database, based on post id
    public async Task<Post?> GetPostById(int id)
    {
        try
        {
            // Query the database for posts by id. Includes tags and categories (eagerly loading)
            return await _db.Posts.Include(post => post.Tags).Include(post => post.Category)
                .Where(post => post.PostId == id).FirstAsync();
        }
        // Error handling if it can't fetch the post
        catch (Exception e)
        {
            LogError("GetPostById", e);
            return null;
        }
    }

    // Fetches all posts from the database
    public async Task<IEnumerable<Post>?> GetAllPosts()
    {
        try
        {
            // Query the database for all posts. Includes tags and categories (eagerly loading)
            return await _db.Posts.Include(post => post.Tags).Include(post => post.Category).ToListAsync();
        }
        // Error handling if it can't fetch posts
        catch (Exception e)
        {
            LogError("GetAllPosts", e);
            return null;
        }
    }

    // Fetches comments from the database, based on the comment id
    public async Task<IEnumerable<Comment>?> GetCommentsByPostId(int postId)
    {
        try
        {
            // Query the database for comments by id. Includes tags and categories (eagerly loading)
            return await _db.Comments.Include(comment => comment.CommentReplies)
                .Where(comment => comment.PostId == postId).ToListAsync();
        }
        // Error handling if it can't fetch the comment
        catch (Exception e)
        {
            LogError("GetCommentsByPostId", e);
            return null;
        }
    }

    // Generic method to fetch any entity based on id
    public async Task<TEntity?> GetTById(int id)
    {
        try
        {
            // Query the database for all entities with primary key as id
            return await _db.Set<TEntity>().FindAsync(id);
        }
        // Error handling if it can't fetch entities
        catch (Exception e)
        {
            LogError("GetTById", e);
            return null;
        }
    }

    // Generic method to fetch any entity based on id
    public async Task<ApplicationUser?> GetUserById(string id)
    {
        try
        {
            // Query the database for all entities with primary key as id
            return await _db.Set<ApplicationUser>().FindAsync(id);
        }
        // Error handling if it can't fetch entities
        catch (Exception e)
        {
            LogError("GetUserById", e);
            return null;
        }
    }


    // Generic method to create and save an entity
    public async Task<TEntity?> Create(TEntity entity)
    {
        try
        {
            // Tries to add an entity, save the changes and return the entity
            _db.Set<TEntity>().Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }
        // Error handling if it can't create a new entity
        catch (Exception e)
        {
            LogError("Create", e);
            return null;
        }
    }

    // Generic method to update an entity
    public async Task<bool> Update(TEntity entity)
    {
        try
        {
            // Tries to update an entity in the database and save the changes
            _db.Set<TEntity>().Update(entity);
            await _db.SaveChangesAsync();
            return true;
        }
        // Error handling if it can't update an entity
        catch (Exception e)
        {
            LogError("Update", e);
            return false;
        }
    }

    // Generic method to delete an entity based on id
    public async Task<bool> Delete(int id)
    {
        try
        {
            // Finds the entity in the database
            var entity = await _db.Set<TEntity>().FindAsync(id);
            // Error handling if there are no entity with the provided id
            if (entity == null)
            {
                LogError($"Delete, entity not found for id {id}", new Exception("Entity not found"));
                return false;
            }

            // Removes the entity from the database and save the changes
            _db.Set<TEntity>().Remove(entity);
            await _db.SaveChangesAsync();
            return true;
        }
        // Error handling if it is not able to delete the entity
        catch (Exception e)
        {
            LogError("Delete", e);
            return false;
        }
    }

    // Method for deleting all the tags from a post based on entity id
    public async Task<bool> RemoveAllPostTags(int id)
    {
        if (id < 0) // id is not valid (negative)
        {
            LogError($"RemoveAllPostTags, entity not found for id {id}", new Exception("Entity not found"));
            return false;
        }

        try
        {
            // Source: https://learn.microsoft.com/en-us/ef/core/querying/sql-queries
            // According to the documentation it's faster to do this than to select all the tags and then remove them one by one for updating
            var executeSqlAsync =
                await _db.Database.ExecuteSqlAsync(
                    $"DELETE FROM PostTag WHERE PostsPostId = {id}");


            // Error handling if it could not find the entity 
            if (executeSqlAsync == 0)
            {
                LogError($"RemoveAllPostTags, entity not found for id {id}", new Exception("Entity not found"));
                return false;
            }

            return true;
        }
        // Error handling if it failed to delete the tags from the entity
        catch (Exception e)
        {
            LogError("RemoveAllPostTags", e);

            return false;
        }
    }

    // Common method for logging errors
    private void LogError(string methodName, Exception exception)
    {
        _logger.LogError(
            $"[{typeof(TEntity).Name} Repository] {methodName}() failed, error message: {exception.Message}");
    }
}