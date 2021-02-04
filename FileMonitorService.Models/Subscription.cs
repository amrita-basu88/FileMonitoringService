namespace FileMonitorService.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Contains all information about a subscription, including its file index.
    /// </summary>
    public class Subscription
    {
        private static readonly NetworkFilePathEqualityComparer filePathComparer = new NetworkFilePathEqualityComparer();

        public virtual ICollection<NetworkFile> fileIndex { get; set; }

        /// <summary>
        /// Initializes a new instance of the Subscription class.
        /// </summary>
        public Subscription()
        {
            fileIndex = new List<NetworkFile>();
            //Id = Guid.NewGuid();
            //InvokeMethodData = new Models.InvokeMethodData { SubscriptionId = Id };
        }

        /// <summary>
        /// Gets or sets the subscription's unique identifier.
        /// </summary>
        [Key]
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets an absolute path to a directory on the network storage.
        /// For example: "\\me-looms-01.post.ubf.nl\ME-LOOMS-01\Ingest\Ingest-ZiggoCatchup"
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets whether the file monitor also checks subdirectories of the path.
        /// </summary>
        public bool IsRecursive { get; set; }

        /// <summary>
        /// </summary>
        public bool IsWatchingDirectories { get; set; }

        /// <summary>
        /// </summary>
        public bool IsWatchingFiles { get; set; }

        /// <summary>
        /// </summary>
        [Required]
        public InvokeMethodData InvokeMethodData { get; set; }

        /// <summary>
        /// Gets or sets how often the subscription should be checked, in seconds. Done on a best effort basis.
        /// </summary>
        public int IntervalInSeconds { get; set; }

        /// <summary>
        /// Gets or sets the next time the subscription should be checked. Calculated from the IntervalInSeconds.
        /// </summary>
        public DateTime? NextCheckDate { get; set; }

        /// <summary>
        /// Gets or sets the last time the subscription was monitored.
        /// </summary>
        public DateTime? LastRunDate { get; set; }

        /// <summary>
        /// Sets the LastRunDate to now and the NextCheckDate to now + IntervalInSeconds.
        /// </summary>
        /// <param name="now">The time to base the LastRunDate and NextCheckDate on.</param>
        public void UpdateLastRunAndNextCheckDate(DateTime now)
        {
            LastRunDate = now;
            NextCheckDate = now.AddSeconds(IntervalInSeconds);
        }

        /// <summary>
        /// Gets the subscription's file index. The file index is the last known state of the files on storage.
        /// </summary>
        /// <returns>An IEnumerable of NetworkFile.</returns>
        public IEnumerable<NetworkFile> GetFileIndex()
        {
            // ToList() creates a copy of the list so the index cann't be modified by users of the class.
            return fileIndex.ToList();
        }

        /// <summary>
        /// Adds a network file to the subscription's file index.
        /// </summary>
        /// <exception cref="ArgumentNullException">Throws ArgumentNullException if the file is null.</exception>
        /// <exception cref="ArgumentException">Throws ArgumentException if a file with the same path is already in the index.</exception>
        /// <param name="file">The network file to add to the index.</param>
        public void AddToFileIndex(NetworkFile file)
        {
            GuardFileIsNotNull(file);
            GuardIndexDoesntContainFile(file);
            fileIndex.Add(new NetworkFile
            {
                Path = file.Path,
                ModificationDate = file.ModificationDate
            });
        }

        /// <summary>
        /// Updates a file in the subscription's file index, based on the network file's path.
        /// </summary>
        /// <exception cref="ArgumentNullException">Throws ArgumentNullException if the file is null.</exception>
        /// <exception cref="ArgumentException">Throws ArgumentException if the file with the same path isn't in the index.</exception>
        /// <param name="file">The network file to update in the index.</param>
        public void UpdateFileIndex(NetworkFile file)
        {
            GuardFileIsNotNull(file);
            GuardIndexContainsFile(file);
            var indexFile = fileIndex.First(f => filePathComparer.Equals(f, file));
            indexFile.Path = file.Path;
            indexFile.ModificationDate = file.ModificationDate;
        }

        /// <summary>
        /// Removes a network file from the subscription's file index, based on the network file's path.
        /// </summary>
        /// <exception cref="ArgumentNullException">Throws ArgumentNullException if the file is null.</exception>
        /// <exception cref="ArgumentException">Throws ArgumentException if the file with the same path isn't in the index.</exception>
        /// <param name="file">The network file to remove from the index.</param>
        public void DeleteFromFileIndex(NetworkFile file)
        {
            GuardFileIsNotNull(file);
            GuardIndexContainsFile(file);
            var indexFile = fileIndex.First(f => filePathComparer.Equals(f, file));
            fileIndex.Remove(indexFile);
        }

        private static void GuardFileIsNotNull(NetworkFile file)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
        }

        private void GuardIndexDoesntContainFile(NetworkFile file)
        {
            if (fileIndex.Contains(file, filePathComparer))
            {
                throw new ArgumentException(string.Format("A file with the same path is already in the file index: {0}.", file.Path), "file");
            }
        }

        private void GuardIndexContainsFile(NetworkFile file)
        {
            if (!fileIndex.Contains(file, filePathComparer))
            {
                throw new ArgumentException(string.Format("A file with the path is not in the file index: {0}.", file.Path), "file");
            }
        }
    }
}
