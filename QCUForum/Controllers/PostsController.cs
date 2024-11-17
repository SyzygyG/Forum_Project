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

            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                var query = @"SELECT id, content, thread_id, created_at 
                              FROM Posts 
                              WHERE thread_id = @threadId
                              ORDER BY created_at ASC
                              LIMIT @offset, @pageSize";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@threadId", threadId);
                    command.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
                    command.Parameters.AddWithValue("@pageSize", pageSize);

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
            }

            // Pass pagination data to the view
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.ThreadId = threadId;

            return View(posts); // Return paginated posts
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
