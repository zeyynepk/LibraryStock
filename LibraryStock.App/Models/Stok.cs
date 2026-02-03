using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryStock.App.Models
{
    [Table("Stoklar")]
    public class Stok
    {
        [Key]
        [Column("StokID")]
        public int Id { get; set; }

        [Required, Column("ItemName")]
        public string? ItemName { get; set; }

        [Column("Quantity")]
        public int Quantity { get; set; }

        [Required, Column("MinValue")]
        public int MinValue { get; set; }

        [Required, Column("CategoryID")]
        public int CategoryID { get; set; }

        [Required, Column("OlusturanUserID")]
        public int OlusturanUserID { get; set; }

        [Required, Column("AddedDate")]
        public DateTime AddedDate { get; set; }

        [Column("UpdatedAt")]
        public DateTime? UpdatedAt { get; set; }

        // kritik durumda mı?
        [NotMapped]
        public bool IsCritical
        {
            get
            {
                return Quantity <= MinValue;
            }
        }

    }
}
