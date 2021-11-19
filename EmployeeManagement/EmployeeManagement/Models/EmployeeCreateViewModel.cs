using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace EmployeeManagement.Models
{
    public class EmployeeCreateViewModel
    {

        [Required]
        [MaxLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        public string Name { get; set; }

        [Required]
        //[RegularExpression(@"^[a - zA - Z0 - 9 + _.-] +@[a-zA-Z0-9.-]+$", ErrorMessage = "Invalid Email")]
        [Display(Name = "Office Email")]
        public string Email { get; set; }

        //[Required] no need to mentiona as its num
        public Dept Department { get; set; }

        public List<IFormFile> Photos { get; set; }
    }
}
