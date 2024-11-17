using System.Collections.Generic;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using QCUForum.Models;
using QCUForum.Helpers;

namespace QCUForum.Controllers
{
    public class CategoriesController : Controller
    {
        public ActionResult Index()
        {
            var categories = new List<Category>();

            using (var connection = DatabaseHelper.GetConnection())
            {
                connection.Open();
                var query = "SELECT id, name, description FROM Categories";
                using (var command = new MySqlCommand(query, connection))
                {
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
            }

            return View(categories); // Returns the list of categories to the view
        }
    }
}
