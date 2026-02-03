using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryStock.App.Models
{
    [Table("Log")]
    public class Log
    {
        [Key] //PK
        public int LogID {  get; set; }
        [Required] //Boş bırakılamaz
        [StringLength(50)]
        public string TabloAdi { get; set; } = "";
        [Required]
        [StringLength(50)]
        public string IslemTuru { get; set; } = "";

        public int KullaniciID { get; set; }
        public DateTime Tarih {  get; set; } = DateTime.Now;
    }
}
