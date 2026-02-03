/* Bu sınıf kimlik doğrulamayı yapar: kullanıcı adı/şifreyi DB’de doğrular, 
 * Name/Role claim’leriyle cookie yazıp oturum açar. Ayrıca çıkış yapar ve geçerli rolü okur*/
using System.Net;
using System.Security.Claims;
using LibraryStock.App.Data;
using LibraryStock.App.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LibraryStock.App.Services
{
    // IAuthService: Giriş/çıkış işlemleri ve geçerli rol bilgisini sağlar
    public interface IAuthService
    {
        // Kullanıcı adı/şifreyi doğrular; başarılı olursa oturum açar 
        Task<bool> SignInAsync(string userName, string password);
        Task SignOutAsync();// Geçerli oturumu kapatır 
        Role? GetCurrentRole(); // Geçerli kullanıcının rolünü döndürür giriş yoksa null
    }
    public class AuthService : IAuthService
    {
        // HttpContext’e erişmek için accessor
        private readonly IHttpContextAccessor _http;

        //DbContext (Users tablosu)
        private readonly AppDbContext _db;

        // HttpContext ve veritabanı bağlantısını (DbContext) servise alır
        public AuthService(IHttpContextAccessor http, AppDbContext db)
        {
            _http = http;
            _db = db;
        }

        public async Task<bool> SignInAsync(string userName, string password)
        {
            // 1) Girişleri normalize et (null → "", ardından Trim)
            string username = userName;
            if (username == null)
            {
                username = string.Empty;
            }
            username = username.Trim();

            string Sifre = password;
            if (Sifre == null)
            {
                Sifre = string.Empty;
            }
            Sifre = Sifre.Trim();

            // 2) Kullanıcı listesini oku 
            var users = await _db.Users.AsNoTracking().ToListAsync();

            // 3) Türkçe kültürde küçük harfe çevirerek kıyaslama yapacak yardımcı fonksiyon
            var tr = new System.Globalization.CultureInfo("tr-TR");
            string ToTrLower(string s)
            {
                if (s == null)
                {
                    s = string.Empty;
                }
                return s.Trim().ToLower(tr);
            }

            string usernameKey = ToTrLower(username);

            User user = null;
            foreach (var u in users)
            {
                if (ToTrLower(u.UserName) == usernameKey)
                {
                    user = u;
                    break;
                }
            }

            // Kullanıcı yoksa başarısız
            if (user == null)
            {
                return false;
            }

            // 5) Şifre kontrolü
            string userSifreLower = ToTrLower(user.Password);
            string SifreLower = ToTrLower(Sifre);

            if (userSifreLower != SifreLower)
            {
                return false;
            }

            string nameValue = user.UserName;
            if (nameValue == null) nameValue = username;

            // Kullanıcı için kimlik bilgilerini (ad ve rol) claim listesine ekler
            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, nameValue));
            claims.Add(new Claim(ClaimTypes.Role, user.Role.ToString()));


            //Elindeki claims(ad, rol vb.) ile bir kimlik oluşturur ve kimliğin doğrulama türünü “Cookies” olarak işaretler
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            //ClaimsPrincipal(principal):bu istek hangi kullanıcıdan geliyor sorusunun cevabını temsil eden kullanıcı nesnesi.
            var principal = new ClaimsPrincipal(identity);

            // 7) HttpContext null olabilir; 
            if (_http == null)
            {
                return false;
            }
            var httpContext = _http.HttpContext;
            if (httpContext == null)
            {
                return false;
            }

            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return true;
            

        }

        public async Task SignOutAsync()
        {
            if (_http == null)
            {
                return;
            }
            var httpContext = _http.HttpContext;
            if (httpContext == null)
            {
                return;
            }
            await httpContext.SignOutAsync();
            

        }

        public Role? GetCurrentRole()
        {
            // Geçerli kullanıcının rol claim’ini oku
            if (_http == null)
            {
                return null;
            }
            var httpContext = _http.HttpContext;
            if (httpContext == null)
            {
                return null;
            }
            var principal = httpContext.User;
            if (principal == null)
            {
                return null;
            }
            var roleClaim = principal.FindFirst(ClaimTypes.Role);
            string value = null;
            if (roleClaim != null)
            {
                value = roleClaim.Value;
            }

            //Claim  kimlik kartının üzerindeki tek bilgi
            //Identity  kimlik kartının tamamı
            //Principal kimliği elinde tutan kişi.


            Role parsed;
            if (Enum.TryParse<Role>(value, out parsed))
            {
                return parsed;
            }
            else
            {
                return null;
            }
        }

        private readonly AuthenticationStateProvider _authProvider;

       

    }
}
/* parse / TryParse = “yazıyı tipe çevir”.
   Örn: "Admin" yazısını Role.Admin değerine çevirmek.
   parsed = TryParse sonucunu koyduğumuz değişken */