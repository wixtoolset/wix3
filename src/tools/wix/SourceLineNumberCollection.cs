// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Hold information about a collection of source lines.
    /// </summary>
    public sealed class SourceLineNumberCollection : ICollection
    {
        private string encodedSourceLineNumbers;
        private SourceLineNumber[] sourceLineNumbers;

        /// <summary>
        /// Instantiate a new SourceLineNumberCollection from encoded source line numbers.
        /// </summary>
        /// <param name="encodedSourceLineNumbers">The encoded source line numbers.</param>
        public SourceLineNumberCollection(string encodedSourceLineNumbers)
        {
            if (null == encodedSourceLineNumbers)
            {
                throw new ArgumentNullException("encodedSourceLineNumbers");
            }

            this.encodedSourceLineNumbers = encodedSourceLineNumbers;
        }

        /// <summary>
        /// Instantiate a new SourceLineNumberCollection from an array of SourceLineNumber objects.
        /// </summary>
        /// <param name="sourceLineNumbers">The SourceLineNumber objects.</param>
        public SourceLineNumberCollection(SourceLineNumber[] sourceLineNumbers)
        {
            if (null == sourceLineNumbers)
            {
                throw new ArgumentNullException("sourceLineNumbers");
            }

            this.sourceLineNumbers = sourceLineNumbers;
        }

        /// <summary>
        /// Gets a 32-bit integer that represents the total number of elements in the SourceLineNumberCollection.
        /// </summary>
        /// <value>A 32-bit integer that represents the total number of elements in the SourceLineNumberCollection.</value>
        public int Count
        {
            get { return this.SourceLineNumbers.Length; }
        }

        /// <summary>
        /// Gets the SourceLineNumberCollection encoded in a string.
        /// </summary>
        /// <value>The SourceLineNumberCollection encoded in a string.</value>
        public string EncodedSourceLineNumbers
        {
            get
            {
                if (null == this.encodedSourceLineNumbers)
                {
                    StringBuilder sb = new StringBuilder();

                    for (int i = 0; i < this.SourceLineNumbers.Length; ++i)
                    {
                        if (0 < i)
                        {
                            sb.Append("|");
                        }

                        sb.Append(this.sourceLineNumbers[i].QualifiedFileName);
                    }

                    this.encodedSourceLineNumbers = sb.ToString();
                }

                return this.encodedSourceLineNumbers;
            }
        }

        /// <summary>
        /// Gets a value indicating whether access to the SourceLineNumberCollection is syncronized.
        /// </summary>
        /// <value>A value indicating whether access to the SourceLineNumberCollection is syncronized.</value>
        public bool IsSynchronized
        {
            get { return this.SourceLineNumbers.IsSynchronized; }
        }

        /// <summary>
        /// Gets an object that can be used to syncronize access to the SourceLineNumberCollection.
        /// </summary>
        /// <value>An object that can be used to syncronize access to the SourceLineNumberCollection.</value>
        public object SyncRoot
        {
            get { return this.SourceLineNumbers.SyncRoot; }
        }

        /// <summary>
        /// The (possibly generated) SourceLineNumber array.
        /// </summary>
        private SourceLineNumber[] SourceLineNumbers
        {
            get
            {
                if (null == this.sourceLineNumbers)
                {
                    string[] encodedSplit = this.encodedSourceLineNumbers.Split('|');
                    this.sourceLineNumbers = new SourceLineNumber[encodedSplit.Length];

                    for (int i = 0; i < encodedSplit.Length; ++i)
                    {
                        string[] fileLineNumber = encodedSplit[i].Split('*');
                        if (2 == fileLineNumber.Length)
                        {
                            this.sourceLineNumbers[i] = new SourceLineNumber(fileLineNumber[0], Convert.ToInt32(fileLineNumber[1], CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            this.sourceLineNumbers[i] = new SourceLineNumber(fileLineNumber[0]);
                        }
                    }
                }

                return this.sourceLineNumbers;
            }
        }

        /// <summary>
        /// Get the SourceLineNumber object at a particular index.
        /// </summary>
        /// <param name="index">Index of the SourceLineNumber.</param>
        public SourceLineNumber this[int index]
        {
            get { return this.SourceLineNumbers[index]; }
            set { this.SourceLineNumbers[index] = value; }
        }

        /// <summary>
        /// Create a new SourceLineNumberCollection from a fileName.
        /// </summary>
        /// <param name="fileName">The fileName.</param>
        /// <returns>The new SourceLineNumberCollection.</returns>
        public static SourceLineNumberCollection FromFileName(string fileName)
        {
            if (null == fileName)
            {
                throw new ArgumentNullException("fileName");
            }

            return new SourceLineNumberCollection(fileName);
        }

        /// <summary>
        /// Create a new SourceLineNumberCollection from a URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>The new SourceLineNumberCollection.</returns>
        public static SourceLineNumberCollection FromUri(string uri)
        {
            if (null == uri || 0 == uri.Length)
            {
                return null;
            }

            string localPath = new Uri(uri).LocalPath;

            // make the local path really look like a normal local path
            if (localPath.StartsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                localPath = localPath.Substring(1);
            }
            localPath = localPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            return new SourceLineNumberCollection(localPath);
        }

        /// <summary>
        /// Copies all elements of the SourceLineNumberCollection to the specified one-dimensional array starting
        /// at the specified destination index.
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the SourceLineNumberCollection.</param>
        /// <param name="index">A 32-bit integer which represents the index in the destination array at which the copying begins.</param>
        public void CopyTo(Array array, int index)
        {
            this.SourceLineNumbers.CopyTo(array, index);
        }

        /// <summary>
        /// Returns an IEnumerator for the SourceLineNumberCollection.
        /// </summary>
        /// <returns>An IEnumerator for the SourceLineNumberCollection.</returns>
        public IEnumerator GetEnumerator()
        {
            return this.SourceLineNumbers.GetEnumerator();
        }

        /// <summary>
        /// Determines if two SourceLineNumberCollections are equivalent.
        /// </summary>
        /// <param name="obj">Object to compare.</param>
        /// <returns>True if SourceLineNumberCollections are equivalent.</returns>
        public override bool Equals(object obj)
        {
            SourceLineNumberCollection otherSourceLineNumberCollection = obj as SourceLineNumberCollection;

            if (null != obj && this.SourceLineNumbers.Length == otherSourceLineNumberCollection.SourceLineNumbers.Length)
            {
                for (int i = 0; i < this.SourceLineNumbers.Length; i++)
                {
                    if (!this.SourceLineNumbers[i].Equals(otherSourceLineNumberCollection.SourceLineNumbers[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Serves as a hash code for a particular type.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return this.EncodedSourceLineNumbers.GetHashCode();
        }
    }
}
