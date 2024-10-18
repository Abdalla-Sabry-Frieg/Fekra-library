using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace MyShop_Entities.Models
{
    public class ApplicationUser : IdentityUser
    {

        [Required(ErrorMessage = "ضع عنوانك بالتفصيل ")]
        public string Address { get; set; }
        [Required(ErrorMessage = "اكتب اسمك ")]
        public string Name { get; set; }
        [Required(ErrorMessage = "ضع عنوان الشارع و المكان بالتحديد")]
        public string City { get; set; }
        [Required(ErrorMessage = "ضع رقم الهاتف المربوط ب الواتساب")]
        public string PhoneNumber { get; set; }
    }
}
