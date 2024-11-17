using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QCUForum.Models
{
        public class Category
        {
            // Primary Key
            public int Id { get; set; }

            // Name of the category (unique and required)
            public string Name { get; set; }

            // Description of the category
            public string Description { get; set; }
        }
    }