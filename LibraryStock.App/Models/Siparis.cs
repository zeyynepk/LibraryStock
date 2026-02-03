using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LibraryStock.App.Models
{
    [Table("Siparişler")]

    public class Siparis
    {
        [Key]
        [Column("SiparisID")]
        public int Id { get; set; }

        [Required]
        [Column("ItemID")]
        public int ItemId { get; set; }

        [Required]
        [Column("RequestedById")]
        public int RequestedById { get; set; }

        [Required]
        [Column("Quantity")]
        public int Quantity { get; set; }

        [Required]
        [Column("RequestDate")]
        public DateTime RequestDate { get; set; } = DateTime.Now;

        [Required]
        [Column("Statüs")]
        public string Status { get; set; } = SiparisDurum.Bekliyor;

        [Column("OnaylayanUserID")]
        public int? OnaylayanUserId { get; set; }

        [Column("OnayTarihi")]
        public DateTime? OnayTarihi {  get; set; }

    }

    public static class SiparisDurum
    {
        public const string Bekliyor = "Bekliyor";
        public const string Onaylandı = "Onaylandı";
        public const string Reddedildi = "Reddedildi";
    }
}
