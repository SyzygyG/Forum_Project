using System;
using System.Collections.Generic;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using QCUForum.Models;
using QCUForum.Helpers;

namespace QCUForum.Controllers
{
    public class PostsController : Controller
    {
        public ActionResult Index(int threadId, int page = 1, int pageSize = 10)
        {
            var posts = new List<Post>();
            int categoryId = 0; // Default value or retrieve dynamically from the database

            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                var query = "SELECT id, content, thread_id, created_at FROM Posts WHERE thread_id = @threadId";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@threadId", threadId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            posts.Add(new Post
                            {
                                Id = reader.GetInt32("id"),
                                Content = reader.GetString("content"),
                                ThreadId = reader.GetInt32("thread_id"),
                                CreatedAt = reader.GetDateTime("created_at")
                            });
                        }
                    }
                }

                // Retrieve categoryId based on threadId (this depends on your database schema)
                var categoryQuery = "SELECT category_id FROM Threads WHERE id = @threadId";
                using (var categoryCommand = new MySqlCommand(categoryQuery, connection))
                {
                    categoryCommand.Parameters.AddWithValue("@threadId", threadId);
                    categoryId = Convert.ToInt32(categoryCommand.ExecuteScalar());
                }
            }

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.ThreadId = threadId;
            ViewBag.CategoryId = categoryId; // Pass categoryId to the view

            return View(posts);
        }

        [HttpPost]
        public ActionResult Create(string Content, int ThreadId)
        {
            string clientIp = Request.UserHostAddress;
            if (clientIp == "::1") clientIp = "127.0.0.1"; // Handle localhost

            DateTime now = DateTime.UtcNow;

            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();

                // Check last post submission time for this IP
                var checkQuery = "SELECT last_post_submission FROM IpTracking WHERE ip_address = @ip";
                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@ip", clientIp);
                    var lastPostSubmission = checkCommand.ExecuteScalar() as DateTime?;

                    if (lastPostSubmission.HasValue && (now - lastPostSubmission.Value).TotalSeconds < 30)
                    {
                        // User is submitting posts too quickly
                        TempData["Error"] = "Please wait at least 30 seconds before submitting another post.";
                        return RedirectToAction("Index", new { threadId = ThreadId });

                    }
                }

                // Insert or update IP tracking for posts
                var updateQuery = @"
            INSERT INTO IpTracking (ip_address, last_post_submission) 
            VALUES (@ip, @time)
            ON DUPLICATE KEY UPDATE last_post_submission = @time";
                using (var updateCommand = new MySqlCommand(updateQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@ip", clientIp);
                    updateCommand.Parameters.AddWithValue("@time", now);
                    updateCommand.ExecuteNonQuery();
                }

                // Proceed with post creation
                var createQuery = "INSERT INTO Posts (content, thread_id, created_at) VALUES (@content, @threadId, @createdAt)";
                using (var createCommand = new MySqlCommand(createQuery, connection))
                {
                    createCommand.Parameters.AddWithValue("@content", Content);
                    createCommand.Parameters.AddWithValue("@threadId", ThreadId);
                    createCommand.Parameters.AddWithValue("@createdAt", now);
                    createCommand.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index", new { threadId = ThreadId });
        }



    }
}
