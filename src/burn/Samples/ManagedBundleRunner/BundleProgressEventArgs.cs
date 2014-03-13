namespace Wix.Samples
{
    using System;

    /// <summary>
    /// Arguments provided when bundle progress is updated.
    /// </summary>
    [Serializable]
    public class BundleProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the percentage from 0 to 100 completed for a bundle.
        /// </summary>
        public int Progress { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Result"/> of the operation. This is passed back to the bundle.
        /// </summary>
        public BundleResult Result { get; set; }
    }
}
