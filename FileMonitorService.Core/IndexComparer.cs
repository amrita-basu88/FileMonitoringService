namespace FileMonitorService.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Compares two indexes of network files to check for new, updated or deleted files.
    /// </summary>
    public static class IndexComparer
    {
        private static readonly NetworkFilePathEqualityComparer filePathComparer = new NetworkFilePathEqualityComparer();
        private static readonly NetworkFilePathAndModificationDateEqualityComparer filePathAndModificationDateComparer = new NetworkFilePathAndModificationDateEqualityComparer();

        /// <summary>
        /// Gets the files that are in storage, but not in the index.
        /// </summary>
        /// <param name="filesInStorage">The network files currently on the network storage.</param>
        /// <param name="filesInIndex">The network files currently in the network file index.</param>
        /// <exception cref="ArgumentNullException">Throws an ArgumentNullException if either filesInStorage or filesInIndex is null.</exception>
        /// <returns>The new network files.</returns>
        public static IEnumerable<NetworkFile> GetNewFiles(IEnumerable<NetworkFile> filesInStorage, IEnumerable<NetworkFile> filesInIndex)
        {
            GuardAgainstNullParameters(filesInStorage, filesInIndex);
            return filesInStorage.Except(filesInIndex, filePathComparer).ToList();
        }

        /// <summary>
        /// Gets the files that are both in storage and in the index, but for which the file modification date changed.
        /// </summary>
        /// <param name="filesInStorage">The network files currently on the network storage.</param>
        /// <param name="filesInIndex">The network files currently in the network file index.</param>
        /// <exception cref="ArgumentNullException">Throws an ArgumentNullException if either filesInStorage or filesInIndex is null.</exception>
        /// <returns>The updated network files.</returns>
        public static IEnumerable<NetworkFile> GetUpdatedFiles(IEnumerable<NetworkFile> filesInStorage, IEnumerable<NetworkFile> filesInIndex)
        {
            GuardAgainstNullParameters(filesInStorage, filesInIndex);
            return filesInStorage
                .Except(filesInIndex, filePathAndModificationDateComparer)
                .Except(GetNewFiles(filesInStorage, filesInIndex), filePathComparer)
                .ToList();
        }

        /// <summary>
        /// Gets the files that are not in storage, but still in the index.
        /// </summary>
        /// <param name="filesInStorage">The network files currently on the network storage.</param>
        /// <param name="filesInIndex">The network files currently in the network file index.</param>
        /// <exception cref="ArgumentNullException">Throws an ArgumentNullException if either filesInStorage or filesInIndex is null.</exception>
        /// <returns>The deleted network files.</returns>
        public static IEnumerable<NetworkFile> GetDeletedFiles(IEnumerable<NetworkFile> filesInStorage, IEnumerable<NetworkFile> filesInIndex)
        {
            GuardAgainstNullParameters(filesInStorage, filesInIndex);
            return filesInIndex.Except(filesInStorage, filePathComparer).ToList();
        }

        /// <summary>
        /// Throws an ArgumentNullException if either filesInStorage or filesInIndex is null.
        /// </summary>
        /// <param name="filesInStorage">Throws an ArgumentNullException with the paramName of filesInStorage if it is null.</param>
        /// <param name="filesInIndex">Throws an ArgumentNullException with the paramName of filesInIndex if it is null.</param>
        private static void GuardAgainstNullParameters(IEnumerable<NetworkFile> filesInStorage, IEnumerable<NetworkFile> filesInIndex)
        {
            if (filesInStorage == null)
            {
                throw new ArgumentNullException("filesInStorage");
            }

            if (filesInIndex == null)
            {
                throw new ArgumentNullException("filesInIndex");
            }
        }
    }
}
