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
        public ActionResult Create(Post post)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                var query = "INSERT INTO Posts (content, thread_id, created_at) VALUES (@content, @threadId, @createdAt)";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@content", post.Content);
                    command.Parameters.AddWithValue("@threadId", post.ThreadId);
                    command.Parameters.AddWithValue("@createdAt", DateTime.Now);
                    command.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index", new { threadId = post.ThreadId });
        }
    }
}
