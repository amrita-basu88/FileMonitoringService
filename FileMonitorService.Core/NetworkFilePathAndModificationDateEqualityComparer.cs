namespace FileMonitorService.Models
{
    /// <summary>
    /// Compares network file instances based on the absolute path and the modification date of the file.
    /// </summary>
    public class NetworkFilePathAndModificationDateEqualityComparer : NetworkFilePathEqualityComparer
    {
        /// <summary>
        /// Compares network file instances based on the path and modification date.
        /// </summary>
        /// <param name="x">One network file.</param>
        /// <param name="y">The other network file.</param>
        /// <returns>True if the network file paths and modification dates are equal, false otherwise.</returns>
        public override bool Equals(NetworkFile x, NetworkFile y)
        {
            return base.Equals(x, y) && x.ModificationDate.Equals(y.ModificationDate);
        }

        /// <summary>
        /// Gets the hash code from the network file path and the modification date.
        /// </summary>
        /// <param name="obj">The network file.</param>
        /// <returns>Hash code of the network file path and the modification date.</returns>
        public override int GetHashCode(NetworkFile obj)
        {
            // "Hashes aren't meant to be unique - they're just meant to be well distributed in most situations."
            //  - John Skeet @ http://stackoverflow.com/a/1079203/4410678
            return base.GetHashCode(obj) * 31 + obj.ModificationDate.GetHashCode();
        }
    }
}
