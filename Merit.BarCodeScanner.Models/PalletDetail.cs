using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Merit.BarCodeScanner.Models
{
    public class PalletDetail
    {
        [Key]
        public string PalletId { get; set; }

        [Required]
        public string EmployeeId { get; set; }

        [Required]
        public string Day { get; set; }

        [Required]
        public Guid BlockId { get; set; }

        public DateTime? PalletScanTime { get; set; }
    }
}
