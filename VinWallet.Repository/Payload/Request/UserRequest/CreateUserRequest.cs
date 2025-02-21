using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VinWallet.Repository.Payload.Request.UserRequest
{
    public class CreateUserRequest
    {
        [Required(ErrorMessage = "Fullname is required")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Username is required")]
        public string? Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "RoomCode is required")]
        public string? BuildingCode { get; set; }

        [Required(ErrorMessage = "HouseId is required")]
        public string? HouseCode { get; set; }
    }
}
