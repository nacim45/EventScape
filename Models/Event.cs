using System;
using System.Collections.Generic;

namespace soft20181_starter.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Images { get; set; } = new List<string>();
        public string Link { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
    }
} 