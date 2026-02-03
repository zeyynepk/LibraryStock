using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using LibraryStock.App.Data;
using LibraryStock.App.Models;
using LibraryStock.App.Services;

namespace LibraryStock.App.Clean.Services
{
    public interface IUsersService
    {
        //arama metniyle kullanıcıları listele. 
        Task<List<User>> GetAsync(string? search);

        
        //Kullanıcıyı ID'ye göre getirir.
        Task<User?> GetByIdAsync(int id);

      
        // Yeni kullanıcı ekler ve eklenen kullanıcıyı döndürür
        Task<User?> AddAsync(User user);

       
        // Var olan kullanıcıyı günceller 
        Task<bool> UpdateAsync(User user);

      
        // Id ile kullanıcıyı siler 
       
        Task<bool> DeleteAsync(int id);

        
        // E-posta adresiyle tek bir kullanıcıyı bulur 
        Task<User?> GetByEmailAsync(string email);

        
        // E-posta adresine sahip kullanıcının şifresini yenisiyle değiştirir.
        Task<bool> ResetPasswordAsync(string email, string newPassword);
    }
   
    public class UsersService : IUsersService
    {
        // Veritabanı erişimi için DbContext
        private readonly AppDbContext _db;

        // Log atmak için dışarıdan verilen log servisi
        private readonly ILogService _logs;

        // Oturum açmış kullanıcının bilgilerine erişmek için HttpContextAccessor
        private readonly IHttpContextAccessor _http;
        public UsersService(AppDbContext db, IHttpContextAccessor http, ILogService logs)
        {
            _db = db;
            _http = http;
            _logs = logs;
        }

       
        // Kullanıcıları listeler. Eğer "search" boş değilse
        public async Task<List<User>> GetAsync(string? search)
        {
            
            // AsNoTracking: salt-okuma listelerinde performans kazandırır.
            var list = await _db.Users.AsNoTracking().ToListAsync();

            // Arama metni yoksa doğrudan tüm liste döner.
            if (string.IsNullOrWhiteSpace(search))
            {
                return list;
            }

            var key = search.Trim().ToLower(CultureInfo.InvariantCulture);
           
            var filtered = new List<User>();

            foreach (var u in list)
            {
                // Null kontrolleri; null değilse küçük harfe çevirip "key" içeriyor mu bak.
                var hit = (!string.IsNullOrEmpty(u.UserName) && u.UserName.ToLower().Contains(key))
                       || (!string.IsNullOrEmpty(u.Email) && u.Email.ToLower().Contains(key));

                if (hit)
                {
                    filtered.Add(u);
                }
            }

            
            return filtered;
        }

      
        // Id alanına göre tek bir kullanıcıyı getirir. Bulamazsa null.
      
        public async Task<User?> GetByIdAsync(int id)
        {
            var query = from u in _db.Users
                        where u.Id == id
                        select u;

            
            return await query.FirstOrDefaultAsync();
        }

       
        public async Task<User?> AddAsync(User user)
        {
            // null ise işlem yapma.
            if (user == null) return null;

            // Ekle ve değişiklikleri yaz.
            _db.Users.Add(user);
            var affected = await _db.SaveChangesAsync(); 

            if (affected > 0)
            {
                await _logs.AddAsync(new Log
                {
                    TabloAdi = "Users",          
                    IslemTuru = "Ekleme",        
                    KullaniciID = await GetCurrentUserIdAsync(), 
                    Tarih = DateTime.Now         
                });

                return user;
            }
            return null;
        }
       
        public async Task<bool> UpdateAsync(User user)
        {
            if (user == null) return false;

            // güncellenecek kaydı bul (
            var query = from u in _db.Users
                        where u.Id == user.Id
                        select u;

            var existing = await query.FirstOrDefaultAsync();

            // Böyle bir kullanıcı yoksa false.
            if (existing == null)
            {
                return false;
            }

            // Alanları temizleyip (Trim) güncelle.
            existing.UserName = user.UserName?.Trim();
            existing.Email = user.Email?.Trim();
            existing.Role = user.Role;

            // Şifre boş değilse güncelle 
            if (!string.IsNullOrWhiteSpace(user.Password))
            {
                existing.Password = user.Password.Trim();
            }

            var affected = await _db.SaveChangesAsync();

            if (affected > 0)
            {
                await _logs.AddAsync(new Log
                {
                    TabloAdi = "Users",
                    IslemTuru = "Güncelleme",
                    KullaniciID = await GetCurrentUserIdAsync(),
                    Tarih = DateTime.Now
                });
                return true;
            }
            return false;
        }

    
        public async Task<bool> DeleteAsync(int id)
        {
          
            var entity = await _db.Users.FindAsync(id);

            if (entity == null) return false; // Zaten yoksa silinecek bir şey de yok.

            _db.Users.Remove(entity);
            var affected = await _db.SaveChangesAsync();

            if (affected > 0)
            {
                await _logs.AddAsync(new Log
                {
                    TabloAdi = "Users",
                    IslemTuru = "Silme",
                    KullaniciID = await GetCurrentUserIdAsync(),
                    Tarih = DateTime.Now
                });
                return true;
            }

            return false;
        }

       
        // E-posta adresi ile kullanıcıyı getirir.
        public async Task<User?> GetByEmailAsync(string email)
        {
            // E-posta boşsa hiç uğraşma.
            if (string.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var norm = email.Trim().ToLower(CultureInfo.InvariantCulture);

            // Veritabanında e-posta alanı null olmayan ve küçük harfi norm'a eşit olan ilk kaydı getir.
            // Dikkat: burada u.Email.ToLower() kültür belirtilmeden çağrılıyor; veritabanı tarafında
            // SQL diline çevrildiği için genelde doğru çalışır. Kodu BOZMAMAK için aynen bırakıldı.
            var query = from u in _db.Users
                        where u.Email != null && u.Email.ToLower() == norm
                        select u;

            return await query.FirstOrDefaultAsync();
        }

        /// <summary>
        /// Verilen e-posta adresine sahip kullanıcının şifresini yeni değerle değiştirir.
        /// Başarılı ise true döner; kullanıcı bulunamazsa veya hata olursa false.
        /// </summary>
        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            // Girdi doğrulaması: ikisinden biri boşsa iptal.
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(newPassword))
            {
                return false;
            }

            // E-postayı normalize et.
            var norm = email.Trim().ToLower(CultureInfo.InvariantCulture);

            // İlgili kullanıcıyı sorgula (lambda yok).
            var query = from u in _db.Users
                        where u.Email != null && u.Email.ToLower() == norm
                        select u;

            var user = await query.FirstOrDefaultAsync();

            // Kullanıcı bulunamadıysa şifre değiştiremeyiz.
            if (user == null)
            {
                return false;
            }

            // Yeni şifreyi boşluklardan arındırıp at.
            user.Password = newPassword.Trim();

            // Hata yakalayıp başarısız olursa false döneriz.
            try
            {
                await _db.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// HttpContext içindeki Claims’den NameIdentifier (genelde kullanıcı Id) bilgisini almaya çalışır.
        /// Bulamazsa 0 döner. Try/catch ile güvenli hale getirildi.
        /// </summary>
        private async Task<int> GetCurrentUserIdAsync()
        {
            try
            {
                // NameIdentifier claim'ini string olarak al (ör: "42")
                var idStr = _http?.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                // Sayıya çevrilebiliyorsa numeric Id'yi döndür.
                if (int.TryParse(idStr, out var id))
                {
                    return id;
                }
            }
            catch
            {
                // Claims okunamadı veya HttpContext yok → 0 döneriz.
            }

            // Asenkron metot imzasına uygun kalsın diye "await" yok ama "async" duruyor.
            // Gerekirse ileride asenkron bir işlem eklenebilir.
            return 0;
        }
    }
}
