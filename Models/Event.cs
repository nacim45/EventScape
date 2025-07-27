using System;
using System.Collections.Generic;

namespace soft20181_starter.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Location { get; set; }
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public List<string> Images { get; set; }
        public string Link { get; set; }
        public bool IsDeleted { get; set; }
        
        // Constructor
        public Event()
        {
            Images = new List<string>();
        }
    }
} 