namespace FileMonitorService.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Compares network file instances based on the absolute path of the file.
    /// </summary>
    public class NetworkFilePathEqualityComparer : IEqualityComparer<NetworkFile>
    {
        /// <summary>
        /// Compares network file instances based on the path.
        /// </summary>
        /// <param name="x">One network file.</param>
        /// <param name="y">The other network file.</param>
        /// <returns>True if the network file paths are equal, false otherwise.</returns>
        public virtual bool Equals(NetworkFile x, NetworkFile y)
        {
            if (Object.ReferenceEquals(x, y))
            {
                return true;
            }

            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
            {
                return false;
            }

            if (x.Path == null && y.Path == null)
            {
                return true;
            }

            if (x.Path == null || y.Path == null)
            {
                return false;
            }

            return x.Path.Equals(y.Path, System.StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Gets the hash code from the network file path.
        /// </summary>
        /// <param name="obj">The network file.</param>
        /// <returns>Hash code of the network file path.</returns>
        public virtual int GetHashCode(NetworkFile obj)
        {
            if (Object.ReferenceEquals(obj, null))
            {
                return default(int);
            }

            if (obj.Path == null)
            {
                return default(int);
            }

            return obj.Path.ToLowerInvariant().GetHashCode();
        }
    }
}
