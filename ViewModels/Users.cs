using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace mypos_api.ViewModels
{
    public partial class UsersViewModels
    {
        // Data Annotation validate
        [Required]
        public string Username { get; set; }
        [MinLength(8, ErrorMessage = "พาสเวิร์ดต้องมากกว่่า 8 ตัว")]
        [MaxLength(20)]
        public string Password { get; set; }
    }
}
