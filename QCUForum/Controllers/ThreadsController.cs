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
        public ActionResult Create(string Title, int CategoryId)
        {
            string clientIp = Request.UserHostAddress;
            if (clientIp == "::1") clientIp = "127.0.0.1"; // Handle localhost

            DateTime now = DateTime.UtcNow;

            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();

                // Check last thread submission time for this IP
                var checkQuery = "SELECT last_thread_submission FROM IpTracking WHERE ip_address = @ip";
                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@ip", clientIp);
                    var lastThreadSubmission = checkCommand.ExecuteScalar() as DateTime?;

                    if (lastThreadSubmission.HasValue && (now - lastThreadSubmission.Value).TotalSeconds < 30)
                    {
                        // User is submitting threads too quickly
                        TempData["Error"] = "Please wait at least 30 seconds before submitting another thread.";
                        return RedirectToAction("Index", new { categoryId = CategoryId });

                    }
                }

                // Insert or update IP tracking for threads
                var updateQuery = @"
            INSERT INTO IpTracking (ip_address, last_thread_submission) 
            VALUES (@ip, @time)
            ON DUPLICATE KEY UPDATE last_thread_submission = @time";
                using (var updateCommand = new MySqlCommand(updateQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@ip", clientIp);
                    updateCommand.Parameters.AddWithValue("@time", now);
                    updateCommand.ExecuteNonQuery();
                }

                // Proceed with thread creation
                var createQuery = "INSERT INTO Threads (title, category_id, created_at) VALUES (@title, @categoryId, @createdAt)";
                using (var createCommand = new MySqlCommand(createQuery, connection))
                {
                    createCommand.Parameters.AddWithValue("@title", Title);
                    createCommand.Parameters.AddWithValue("@categoryId", CategoryId);
                    createCommand.Parameters.AddWithValue("@createdAt", now);
                    createCommand.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index", new { categoryId = CategoryId });
        }

        [HttpPost]
        public ActionResult ReportThread(int id, int categoryId)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();

                // Increment report count for the thread
                var query = "UPDATE Threads SET report_count = report_count + 1 WHERE id = @id";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                }
            }

            TempData["Success"] = "Thread has been reported successfully.";
            return RedirectToAction("Index", new { categoryId }); // Pass categoryId explicitly
        }




    }
}
