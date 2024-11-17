using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QCUForum.Models
{
        public class Thread
        {
            // Primary Key
            public int Id { get; set; }

            // Title of the thread
            public string Title { get; set; }

            // Foreign Key: Category to which the thread belongs
            public int CategoryId { get; set; }

            // Date and time the thread was created
            public DateTime CreatedAt { get; set; }
        }
    }