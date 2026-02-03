using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryStock.App.Models
{
    // Mevcut tablo adı: dbo.Users
    [Table("Users")]
    public class User
    {
        [Column("UserID")]
        public int Id { get; set; }

        [Column("UserName")]
        public string UserName { get; set; } = string.Empty; //string.Empty:başlangıçta null yerine güvenli boş değer vermek için kullanılır


        [Column("Email")]
        [Required(ErrorMessage = "E-posta boş bırakılamaz.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz (ör: ad@site.com).")]
        public string Email { get; set; } = string.Empty;

        [Column("Şifre")]
        public string Password { get; set; } = string.Empty;

        // Role enum'unu direkt int kolonuna mapliyoruz 
        [Column("RoleID")]
        public Role Role { get; set; }

        [Column("LastLoginDate")]
        public DateTime? LastLoginDate { get; set; }

        [Column("CreatedAdd")]
        public DateTime? CreatedAdd { get; set; }
    }
}
