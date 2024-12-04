using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QCUForum.Models
{
        public class Post
        {
            // Primary Key
            public int Id { get; set; }

            // Content of the post
            public string Content { get; set; }

            // Foreign Key: Thread to which the post belongs
            public int ThreadId { get; set; }

            // Date and time the post was created
            public DateTime CreatedAt { get; set; }
            public int ReportCount { get; set; }
        }
}