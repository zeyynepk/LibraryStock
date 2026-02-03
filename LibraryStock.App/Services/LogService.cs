using System.Threading.Tasks;
using LibraryStock.App.Data;
using LibraryStock.App.Models;

namespace LibraryStock.App.Services
{
    public interface ILogService
    {
        Task AddAsync(Log log); 
    }

    public class LogService : ILogService
    {
        private readonly AppDbContext _db; // Veritabanına erişim için

        // Constructor dışarıdan AppDbContext alınır
        public LogService(AppDbContext db)
        {
            _db = db; // gelen veritabanı nesnesini sakla
        }

        public async Task AddAsync(Log log)
        {
            if (log == null)
            {
                return;
            }

            _db.Logs.Add(log);         
            await _db.SaveChangesAsync(); 
        }
    }
}
