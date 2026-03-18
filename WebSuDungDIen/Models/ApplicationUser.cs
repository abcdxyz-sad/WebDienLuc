using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace WebSuDungDIen.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsActive { get; set; } = true;
        public string? HoTen { get; set; }
        public string? MaNV { get; set; }
    }
}
