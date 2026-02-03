using System;
using System.Linq;                       // sorgu sözdizimi için
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using LibraryStock.App.Data;
using LibraryStock.App.Models;
using LibraryStock.App.Services;

namespace LibraryStock.App.Clean.Services
{
    public interface IStokService
    {
        Task<List<Stok>> GetAsync(string? search);
        Task<Stok?> GetByIdAsync(int id);
        Task<Stok?> AddAsync(Stok stok);
        Task<bool> UpdateAsync(Stok stok);
        Task<bool> DeleteAsync(int id);

        // Sipariş ekranları için
        Task<List<Stok>> GetCriticalAsync();          // Personel: sadece kritikler
        Task<List<Stok>> GetForOrder_AdminAsync();    // Admin: kritikler önce, tüm ürünler
    }

    public class StokService : IStokService
    {
        private readonly AppDbContext _db;
        private readonly IHttpContextAccessor _http;
        private readonly ILogService _logs;

        public StokService(AppDbContext db, IHttpContextAccessor http, ILogService logs)
        {
            _db = db;
            _http = http;
            _logs = logs;
        }

        // Listeleme (arama + sıralama)
        public async Task<List<Stok>> GetAsync(string? search)
        {
            // 1) başlangıç sorgusu (no tracking)
            IQueryable<Stok> q = from s in _db.Stoklar.AsNoTracking()
                                 select s;

            // 2) arama filtresi (varsa)
            if (!string.IsNullOrWhiteSpace(search))
            {
                string key = search.Trim().ToLower();
                q = from s in q
                    where ((s.ItemName ?? string.Empty).ToLower()).Contains(key)
                    select s;
            }

            // 3) sıralama
            q = from s in q
                orderby s.Id
                select s;

            return await q.ToListAsync();
        }

        public async Task<Stok?> GetByIdAsync(int id)
        {
            var q = from s in _db.Stoklar
                    where s.Id == id
                    select s;

            return await q.FirstOrDefaultAsync();
        }

        public async Task<Stok?> AddAsync(Stok stok)
        {
            if (stok == null) return null;

            var now = DateTime.Now;

            // Zorunlu alanlar
            if (stok.AddedDate == default) stok.AddedDate = now;
            if (!stok.UpdatedAt.HasValue) stok.UpdatedAt = stok.AddedDate;

            // Oturumdaki kullanıcıyı yaz (bulunamazsa 0 kalabilir)
            if (stok.OlusturanUserID == 0)
            {
                var idStr = _http?.HttpContext?.User?
                                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idStr, out var uid)) stok.OlusturanUserID = uid;
            }

            _db.Stoklar.Add(stok);
            var affected = await _db.SaveChangesAsync();

            if (affected > 0)
            {
                await _logs.AddAsync(new Log
                {
                    TabloAdi = "Stoklar",
                    IslemTuru = "Ekleme",
                    KullaniciID = stok.OlusturanUserID,
                    Tarih = now
                });
                return stok;
            }
            return null;
        }

        public async Task<bool> UpdateAsync(Stok stok)
        {
            if (stok == null) return false;

            var q = from s in _db.Stoklar
                    where s.Id == stok.Id
                    select s;

            var existing = await q.FirstOrDefaultAsync();
            if (existing == null) return false;

            // Sadece kolon alanlarını güncelle
            existing.ItemName = stok.ItemName?.Trim();
            existing.Quantity = stok.Quantity;
            existing.MinValue = stok.MinValue;
            existing.CategoryID = stok.CategoryID;

            existing.UpdatedAt = DateTime.Now;

            var affected = await _db.SaveChangesAsync();
            if (affected > 0)
            {
                await _logs.AddAsync(new Log
                {
                    TabloAdi = "Stoklar",
                    IslemTuru = "Güncelleme",
                    KullaniciID = existing.OlusturanUserID,
                    Tarih = DateTime.Now
                });
                return true;
            }
            return false;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var q = from s in _db.Stoklar
                    where s.Id == id
                    select s;

            var entity = await q.FirstOrDefaultAsync();
            if (entity == null) return false;

            _db.Stoklar.Remove(entity);
            var affected = await _db.SaveChangesAsync();
            if (affected > 0)
            {
                await _logs.AddAsync(new Log
                {
                    TabloAdi = "Stoklar",
                    IslemTuru = "Silme",
                    KullaniciID = entity.OlusturanUserID,
                    Tarih = DateTime.Now
                });
                return true;
            }
            return false;
        }

        // Kritik: Quantity <= MinValue
        public async Task<List<Stok>> GetCriticalAsync()
        {
            var q = (from s in _db.Stoklar
                     where s.Quantity <= s.MinValue
                     orderby (s.Quantity - s.MinValue) ascending, s.ItemName
                     select s).AsNoTracking();

            return await q.ToListAsync();
        }

        // Admin: tüm ürünler (kritikler önce, sonra isim)
        public async Task<List<Stok>> GetForOrder_AdminAsync()
        {
            var q = (from s in _db.Stoklar
                     orderby (s.Quantity <= s.MinValue ? 0 : 1), s.ItemName
                     select s).AsNoTracking();

            return await q.ToListAsync();
        }
    }
}
