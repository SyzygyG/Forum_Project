using System;
using System.Collections.Generic;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using QCUForum.Models;
using QCUForum.Helpers;

namespace QCUForum.Controllers
{
    public class ThreadsController : Controller
    {
        public ActionResult Index(int categoryId, int page = 1, int pageSize = 10)
        {
            var threads = new List<Thread>();

            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                var query = @"SELECT id, title, category_id, created_at 
                              FROM Threads 
                              WHERE category_id = @categoryId
                              ORDER BY created_at DESC
                              LIMIT @offset, @pageSize";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@categoryId", categoryId);
                    command.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
                    command.Parameters.AddWithValue("@pageSize", pageSize);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            threads.Add(new Thread
                            {
                                Id = reader.GetInt32("id"),
                                Title = reader.GetString("title"),
                                CategoryId = reader.GetInt32("category_id"),
                                CreatedAt = reader.GetDateTime("created_at")
                            });
                        }
                    }
                }
            }

            // Pass pagination data to the view
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.CategoryId = categoryId;

            return View(threads);
        }

        [HttpPost]
        public ActionResult Create(Thread thread)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                var query = "INSERT INTO Threads (title, category_id, created_at) VALUES (@title, @categoryId, @createdAt)";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@title", thread.Title);
                    command.Parameters.AddWithValue("@categoryId", thread.CategoryId);
                    command.Parameters.AddWithValue("@createdAt", DateTime.Now);
                    command.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index", new { categoryId = thread.CategoryId });
        }
    }
}
