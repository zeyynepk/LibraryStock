using System;                                   
using System.Collections.Generic;               
using System.Linq;                              
using Microsoft.EntityFrameworkCore;           
using LibraryStock.App.Data;                   
using LibraryStock.App.Models;                 

namespace LibraryStock.App.Services           
{
    // Bu arayüz, siparişlerle ilgili dışarıya açılan sözleşmeyi belirtir.
    public interface IOrdersService
    {
        Task<int> CreateAsync(int itemId, int quantity, int requestedById);

        // Durumu "Bekliyor" olan tüm siparişleri (en yeni en üstte) döndürür.
        Task<List<Siparis>> GetWaitingAsync();

        // Belirli bir kullanıcıya ait tüm siparişleri döndürür.
        Task<List<Siparis>> GetMyAsync(int userId);

        Task<int> CountWaitingAsync();
        Task ApproveAsync(int orderId, int approvedById);

    }

    // Bu sınıf, IOrdersService'in gerçek  uygulamasıdır.
    public class OrdersService : IOrdersService
    {
        private readonly ILogService _logs;
        private readonly AppDbContext _db;     // Veritabanı bağlamı 
        
        public OrdersService(AppDbContext db, ILogService logs) 
        {
            _db = db;
            _logs = logs; 
        }

        public async Task<int> CreateAsync(int itemId, int quantity, int requestedById)
        {
            if (itemId <= 0)
            {
                throw new ArgumentException("Geçersiz ürün.");
            }

            if (quantity <= 0)
            {
                throw new ArgumentException("Adet en az 1 olmalı.");
            }

            if (requestedById <= 0)
            {
                throw new ArgumentException("Kullanıcı doğrulanamadı.");
            }

            // Ürünün gerçekten var olup olmadığını kontrol ederiz.
            
            // Sorgu: ilgili Id'ye sahip en az bir stok satırı var mı?
            var stokSorgu =
                from s in _db.Stoklar             
                where s.Id == itemId                
                select s.Id;                       

            var stokVarMi = await stokSorgu.AnyAsync();  // En az bir kayıt varsa true döner.

            if (!stokVarMi)
            {   
                // Hatalı durumda özel istisna fırlatır

                throw new InvalidOperationException("Ürün bulunamadı.");
            }

           
            var o = new Siparis
            {
                ItemId = itemId,                      
                Quantity = quantity,                  
                RequestedById = requestedById,        
                RequestDate = DateTime.Now,        
                Status = SiparisDurum.Bekliyor       
            };

            _db.Siparisler.Add(o);                    // henüz veritabanına yazmadık
            await _db.SaveChangesAsync();             // Değişiklikleri veritabanına uygula 

            await _logs.AddAsync(new Log               
            {                                           
                TabloAdi = "Siparisler",             
                IslemTuru = "Ekleme",                 
                KullaniciID = requestedById,            
                Tarih = DateTime.Now              
            });

            return o.Id;                              // Oluşan siparişin birincil anahtarı (Id) artık set edilmiş halde.
        }

        
        public async Task<List<Siparis>> GetWaitingAsync()
        {
           
            // Sorgu: Siparisler içinden Status'u Bekliyor olanları RequestDate'e göre azalan sırala.
            var q =
                from x in _db.Siparisler
                where x.Status == SiparisDurum.Bekliyor
                orderby x.RequestDate descending
                select x;

            var liste = await q.AsNoTracking().ToListAsync(); 
            return liste;                                      
        }

  
        public async Task<List<Siparis>> GetMyAsync(int userId)
        {
            // Sorgu: Siparisler içinden RequestedById'si verilen kullanıcı olanlar
            var q =
                from x in _db.Siparisler
                where x.RequestedById == userId
                orderby x.RequestDate descending
                select x;

            var liste = await q.AsNoTracking().ToListAsync();  
            return liste;                                    
        }

        public async Task<int> CountWaitingAsync()
        {
            
            // Sorgu: Durumu Bekliyor olan siparişlerin Id'lerini seç 
            var q =
                from x in _db.Siparisler
                where x.Status == SiparisDurum.Bekliyor
                select x.Id;

            var adet = await q.CountAsync();   
            return adet;                      
        }

        public async Task ApproveAsync(int orderId , int approvedById)
        {
            if(orderId <= 0)
            {
                throw new ArgumentException("Geçersiz sipariş.");
            }

            if(approvedById <= 0)
            {
                throw new ArgumentException("Onaylayan kullanıcı doğrulanamadi.");
            }

            //Tüm adımlar birlikte başarılı olmalı: transaksiyon başlat
            using (var tx = await _db.Database.BeginTransactionAsync())
            {
                //Sipariş yükle

                var siparisSorgu = from o in _db.Siparisler
                                   where o.Id == orderId
                                   select o;

                var order = await siparisSorgu.FirstOrDefaultAsync();

                if(order == null)
                {
                    throw new InvalidOperationException("Sipariş bulunamadı");
                }

                //Sadece 'Bekliyor' olan sipariş onaylanabilir

                if(order.Status != SiparisDurum.Bekliyor)
                {
                    throw new InvalidOperationException("Sipariş bekleniyor durumunda değil");
                }

                //İlgili stok kaydını yükle
                var stokSorgu = from s in _db.Stoklar
                                where s.Id == order.ItemId
                                select s;
                
                var stok = await stokSorgu.FirstOrDefaultAsync();

                if(stok == null)
                {
                    throw new InvalidOperationException("Ürün (stok) bulunamadı.");
                }

                //Stoğu artır 

                var yeniAdet = stok.Quantity + order.Quantity;
                stok.Quantity = yeniAdet;

                order.Status = SiparisDurum.Onaylandı;
                order.OnaylayanUserId = approvedById;
                order.OnayTarihi = DateTime.Now;

                //Veritabanına uygula ve transaksiyonu bitir
                await _db.SaveChangesAsync();

                await _logs.AddAsync(new Log            
                {                                       
                    TabloAdi = "Siparisler",         
                    IslemTuru = "Sipariş Onaylama",         
                    KullaniciID = approvedById,        
                    Tarih = DateTime.Now          
                });

                await tx.CommitAsync();
            }
        }
    }
}
