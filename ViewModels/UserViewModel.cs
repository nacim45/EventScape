using System;
using System.Collections.Generic;

namespace soft20181_starter.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime RegisteredDate { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
} 