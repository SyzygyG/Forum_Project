using System;
using System.Collections.Generic;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using QCUForum.Helpers;
using QCUForum.Models;

namespace QCUForum.Controllers
{
    [AuthorizeAdmin]
    public class AdminController : Controller
    {
        [AllowAnonymous]
        public ActionResult Login()
        {
            // Check if the admin is already logged in
            if (Session["IsAdmin"] != null && (bool)Session["IsAdmin"])
            {
                return RedirectToAction("Dashboard");
            }

            // Otherwise, show the login page
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Login(string username, string password, string returnUrl)
        {
            if (ValidateAdminCredentials(username, password))
            {
                Session["IsAdmin"] = true;

                // Redirect to the original URL or Dashboard
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Dashboard");
            }

            ViewBag.Error = "Invalid username or password.";
            return View();
        }

        [AllowAnonymous]
        public ActionResult Logout()
        {
            Session["IsAdmin"] = null;
            return RedirectToAction("Login");
        }

        // Admin Dashboard
        [AuthorizeAdmin]
        public ActionResult Dashboard()
        {
            var stats = new DashboardStats();

            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();

                // Fetch category count
                var categoryQuery = "SELECT COUNT(*) FROM Categories";
                using (var command = new MySqlCommand(categoryQuery, connection))
                {
                    stats.CategoryCount = Convert.ToInt32(command.ExecuteScalar());
                }

                // Fetch thread count
                var threadQuery = "SELECT COUNT(*) FROM Threads";
                using (var command = new MySqlCommand(threadQuery, connection))
                {
                    stats.ThreadCount = Convert.ToInt32(command.ExecuteScalar());
                }

                // Fetch post count
                var postQuery = "SELECT COUNT(*) FROM Posts";
                using (var command = new MySqlCommand(postQuery, connection))
                {
                    stats.PostCount = Convert.ToInt32(command.ExecuteScalar());
                }
            }

            return View(stats);
        }




        // Manage Categories
        [AuthorizeAdmin]
        public ActionResult ManageCategories()
        {
            var categories = new List<Category>();

            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                var query = "SELECT id, name, description FROM Categories";
                using (var command = new MySqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categories.Add(new Category
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader.GetString("name"),
                            Description = reader.GetString("description")
                        });
                    }
                }
            }

            return View(categories);
        }


        [AuthorizeAdmin]
        [HttpPost]
        public ActionResult CreateCategory(string name, string description)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                var query = "INSERT INTO Categories (name, description) VALUES (@name, @description)";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@description", description);
                    command.ExecuteNonQuery();
                }
            }

            return RedirectToAction("ManageCategories");
        }

        [AuthorizeAdmin]
        [HttpPost]
        public ActionResult EditCategory(int id, string name, string description)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                var query = "UPDATE Categories SET name = @name, description = @description WHERE id = @id";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@description", description);
                    command.ExecuteNonQuery();
                }
            }

            return RedirectToAction("ManageCategories");
        }

        [AuthorizeAdmin]
        [HttpPost]
        public ActionResult DeleteCategory(int id)
        {
            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();

                // Delete the category
                var query = "DELETE FROM Categories WHERE id = @id";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                }
            }

            return RedirectToAction("ManageCategories");
        }


        // Manage Threads
        [AuthorizeAdmin]
        public ActionResult ManageThreads(int? categoryId)
        {
            if (categoryId == null)
            {
                return RedirectToAction("ManageCategories");
            }

            var threads = new List<Thread>();
            string categoryName = null;

            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();

                // Fetch category name
                var categoryQuery = "SELECT name FROM Categories WHERE id = @categoryId";
                using (var command = new MySqlCommand(categoryQuery, connection))
                {
                    command.Parameters.AddWithValue("@categoryId", categoryId.Value);
                    categoryName = command.ExecuteScalar()?.ToString();
                }

                // Fetch threads with report counts
                var threadQuery = @"
            SELECT id, title, category_id, created_at, report_count
            FROM Threads
            WHERE category_id = @categoryId";
                using (var command = new MySqlCommand(threadQuery, connection))
                {
                    command.Parameters.AddWithValue("@categoryId", categoryId.Value);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            threads.Add(new Thread
                            {
                                Id = reader.GetInt32("id"),
                                Title = reader.GetString("title"),
                                CategoryId = reader.GetInt32("category_id"),
                                CreatedAt = reader.GetDateTime("created_at"),
                                ReportCount = reader.GetInt32("report_count") // Fetch report count
                            });
                        }
                    }
                }
            }

            ViewBag.CategoryName = categoryName;
            ViewBag.CategoryId = categoryId;
            return View(threads);
        }


        [AuthorizeAdmin]
        [HttpPost]
        public ActionResult DeleteThread(int id)
        {
            int categoryId = 0;

            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();

                // Fetch the category ID before deletion
                var categoryQuery = "SELECT category_id FROM Threads WHERE id = @id";
                using (var command = new MySqlCommand(categoryQuery, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    categoryId = Convert.ToInt32(command.ExecuteScalar());
                }

                // Delete the thread
                var deleteQuery = "DELETE FROM Threads WHERE id = @id";
                using (var command = new MySqlCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                }
            }

            // Redirect back to the posts page for the same thread
            return RedirectToAction("ManageThreads", new { categoryId });
        }


        // Manage Posts
        [AuthorizeAdmin]
        public ActionResult ManagePosts(int? threadId)
        {
            if (threadId == null)
            {
                return RedirectToAction("ManageThreads");
            }

            var posts = new List<Post>();
            string threadTitle = null;
            string categoryName = null;
            int categoryId = 0;

            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();

                // Fetch thread, category names, and category ID
                var threadQuery = @"
            SELECT t.title, c.name, c.id
            FROM Threads t
            JOIN Categories c ON t.category_id = c.id
            WHERE t.id = @threadId";
                using (var command = new MySqlCommand(threadQuery, connection))
                {
                    command.Parameters.AddWithValue("@threadId", threadId.Value);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            threadTitle = reader.GetString("title");
                            categoryName = reader.GetString("name");
                            categoryId = reader.GetInt32("id");
                        }
                    }
                }

                // Fetch posts with report counts
                var postQuery = @"
            SELECT id, content, thread_id, created_at, report_count
            FROM Posts
            WHERE thread_id = @threadId";
                using (var command = new MySqlCommand(postQuery, connection))
                {
                    command.Parameters.AddWithValue("@threadId", threadId.Value);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            posts.Add(new Post
                            {
                                Id = reader.GetInt32("id"),
                                Content = reader.GetString("content"),
                                ThreadId = reader.GetInt32("thread_id"),
                                CreatedAt = reader.GetDateTime("created_at"),
                                ReportCount = reader.GetInt32("report_count") // Fetch report count
                            });
                        }
                    }
                }
            }

            ViewBag.ThreadTitle = threadTitle;
            ViewBag.CategoryName = categoryName;
            ViewBag.CategoryId = categoryId; // Ensure this is passed back
            ViewBag.ThreadId = threadId;
            return View(posts);
        }


        [AuthorizeAdmin]
        [HttpPost]
        public ActionResult DeletePost(int id)
        {
            int threadId = 0;

            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();

                // Fetch the thread ID before deletion
                var threadQuery = "SELECT thread_id FROM Posts WHERE id = @id";
                using (var command = new MySqlCommand(threadQuery, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    threadId = Convert.ToInt32(command.ExecuteScalar());
                }

                // Delete the post
                var deleteQuery = "DELETE FROM Posts WHERE id = @id";
                using (var command = new MySqlCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                }
            }

            // Redirect back to the posts page for the same thread
            return RedirectToAction("ManagePosts", new { threadId });
        }



        // Helper: Validate Admin Credentials
        private bool ValidateAdminCredentials(string username, string password)
        {
            string hashedPassword = ComputeHash(password);

            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();

                var query = "SELECT COUNT(*) FROM Admins WHERE username = @username AND password_hash = @passwordHash";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@passwordHash", hashedPassword);

                    return Convert.ToInt32(command.ExecuteScalar()) > 0;
                }
            }
        }

        // Helper: Hash Password
        private string ComputeHash(string input)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }
    }

    // Custom Authorization Attribute
    public class AuthorizeAdminAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // Skip authorization for Login and Logout actions
            var actionName = filterContext.ActionDescriptor.ActionName;
            if (actionName.Equals("Login", StringComparison.OrdinalIgnoreCase) ||
                actionName.Equals("Logout", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Check admin session
            if (filterContext.HttpContext.Session["IsAdmin"] == null || !(bool)filterContext.HttpContext.Session["IsAdmin"])
            {
                string returnUrl = filterContext.HttpContext.Request.Url?.PathAndQuery;
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary
                    {
                    { "controller", "Admin" },
                    { "action", "Login" },
                    { "ReturnUrl", returnUrl }
                    }
                );
            }
        }
    }


}
