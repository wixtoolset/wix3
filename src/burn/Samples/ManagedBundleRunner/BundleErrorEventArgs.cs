namespace Wix.Samples
{
    using System;

    /// <summary>
    /// Arguments provided when bundle encounters an error.
    /// </summary>
    [Serializable]
    public class BundleErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the error code.
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets the recommended display flags for an error dialog.
        /// </summary>
        public int UIHint { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Result"/> of the operation. This is passed back to the bundle.
        /// </summary>
        public BundleResult Result { get; set; }
    }
}
