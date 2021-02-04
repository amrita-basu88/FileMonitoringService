namespace FileMonitorService.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    /// <summary>
    /// Holds all information about a network file required to check if the file is new,
    /// updated or deleted, compared to the previous known information about the file.
    /// </summary>
    public class NetworkFile
    {
        /// <summary>
        /// Unique ID of the NetworkFile
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the absolute path to the network file. Includes the file name.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the (last known) modification date of the network file.
        /// </summary>
        /// <remarks>
        /// We store this as text, because SQL server rounds off the ticks,
        /// which would make it impossible to detect filemodifications based on date.
        /// </remarks>
        [NotMapped]
        public DateTime ModificationDate
        {
            get
            {
                return DateTime.Parse(ModificationDateText);
            }
            set
            {
                ModificationDateText = value.ToString("O");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string ModificationDateText { get; set; }

        /// <summary>
        /// Foreign Key to force cascade delete
        /// </summary>
        [Required]
        public virtual long SubscriptionId { get; set; }

    }
}
