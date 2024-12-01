using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QCUForum.Models
{
    public class DashboardStats
    {
        public int CategoryCount { get; set; }
        public int ThreadCount { get; set; }
        public int PostCount { get; set; }

        // New properties for dynamic links
        public List<Category> Categories { get; set; }
        public List<Thread> Threads { get; set; }
    }
}

