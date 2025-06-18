using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace soft20181_starter.Models;

public class TheEvent
{
    public int id { get; set; }

    [Required(ErrorMessage = "Location is required")]
    public string location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
    public string title { get; set; } = string.Empty;

    public List<string> images { get; set; } = new List<string>();

    [Required(ErrorMessage = "Description is required")]
    public string description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Date is required")]
    public string date { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price is required")]
    public string price { get; set; } = string.Empty;

    [Url(ErrorMessage = "Please enter a valid URL")]
    public string link { get; set; } = string.Empty;
    
    public bool IsDeleted { get; set; } = false;
}