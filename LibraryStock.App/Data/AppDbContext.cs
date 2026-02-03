using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using LibraryStock.App.Clean.Components.Pages;
using LibraryStock.App.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

//Amacı: Users ve Logs tablolarına ulaşabilmek.
//Veritabanında kayıt ekleme / güncelleme / silme yapıldığında otomatik olarak bir log kaydı düşmek

namespace LibraryStock.App.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IHttpContextAccessor _http;

        // IHttpContextAccessor ekledik ki cookie'den KullanıcıID alabilelim
        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor http) : base(options)
        {
            _http = http;
        }

        public DbSet<User> Users
        {
            get { return Set<User>(); }
        }

        public DbSet<Log> Logs
        {
            get { return Set<Log>(); }
        }

        public DbSet<Stok> Stoklar
        {
            get { return Set<Stok>(); }
        }
        public DbSet<Siparis> Siparisler
        {
            get { return Set<Siparis>(); }
        }



        // Veritabanına kayıt yaparken önce log ekle (senkron çalışan sürüm)
        public override int SaveChanges()
        {
            TryAppendUserLogs();             // Kullanıcı ekle/sil/güncelle işlemine log hazırla
            return base.SaveChanges();       // Asıl değişiklikleri veritabanına yaz
        }

        //  (asenkron çalışan sürüm)
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            TryAppendUserLogs();                    
            return base.SaveChangesAsync(cancellationToken); 
        }
        private void TryAppendUserLogs()
        {
            // İşlemi yapan kullanıcının ID’si (cookie’den okunacak)
            int actorId = 0;
            try
            {
                // Kullanıcının kimlik bilgisini (ID) oku
                var idStr = _http?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

                // Eğer boş değilse sayıya çevir (ör. "5" → 5)
                if (!string.IsNullOrWhiteSpace(idStr)) int.TryParse(idStr, out actorId);
            }
            catch { }

            
            var now = DateTime.Now;


            // Users tablosunda yapılan değişikliklerin durumlarını listele (ekleme/güncelleme/silme)
            var list = new List<EntityState>();  

            // ChangeTracker → EF Core’un tuttuğu “hangi nesneler değişti” defteri
            foreach (var entry in ChangeTracker.Entries<User>()) // User tablosundaki tüm girişleri dolaş
            {
                if (entry.State == EntityState.Added ||
                    entry.State == EntityState.Modified ||
                    entry.State == EntityState.Deleted)
                {
                    list.Add(entry.State);
                }
            }
            // Artık elimizde sadece eklenen / güncellenen / silinen kayıtların durum listesi var
            var states = list;

            foreach (var st in states) 
            {
                string islem = "Bilinmiyor"; // varsayılan değer

                
                if (st == EntityState.Added)
                {
                    islem = "Ekle";
                }
                else if (st == EntityState.Modified)
                {
                    islem = "Güncelle";
                }
                else if (st == EntityState.Deleted)
                {
                    islem = "Sil";
                }

                Logs.Add(new Log
                {
                    TabloAdi = "Users",     
                    IslemTuru = islem,      
                    KullaniciID = actorId,  
                    Tarih = now             
                });
            }
        }
    }
}
    

