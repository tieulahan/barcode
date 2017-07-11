using System;
using System.ComponentModel.DataAnnotations;

namespace Merit.BarCodeScanner.Models
{
    public class DeliveryBlock
    {

        [Key]
        public int Id { get; set; }

        [Required]
        public Guid BlockId { get; set; }

        [Required]
        public string DestinationId { get; set; }

        public DateTime? BlockStartTime { get; set; }

        public DateTime? BlockEndTime { get; set; }
    }
}
