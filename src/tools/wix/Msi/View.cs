// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Msi
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

    /// <summary>
    /// Enumeration of different modify modes.
    /// </summary>
    public enum ModifyView
    {
        /// <summary>
        /// Writes current data in the cursor to a table row. Updates record if the primary 
        /// keys match an existing row and inserts if they do not match. Fails with a read-only 
        /// database. This mode cannot be used with a view containing joins.
        /// </summary>
        Assign = MsiInterop.MSIMODIFYASSIGN,

        /// <summary>
        /// Remove a row from the table. You must first call the Fetch function with the same
        /// record. Fails if the row has been deleted. Works only with read-write records. This
        /// mode cannot be used with a view containing joins.
        /// </summary>
        Delete = MsiInterop.MSIMODIFYDELETE,

        /// <summary>
        /// Inserts a record. Fails if a row with the same primary keys exists. Fails with a read-only
        /// database. This mode cannot be used with a view containing joins.
        /// </summary>
        Insert = MsiInterop.MSIMODIFYINSERT,

        /// <summary>
        /// Inserts a temporary record. The information is not persistent. Fails if a row with the 
        /// same primary key exists. Works only with read-write records. This mode cannot be 
        /// used with a view containing joins.
        /// </summary>
        InsertTemporary = MsiInterop.MSIMODIFYINSERTTEMPORARY,

        /// <summary>
        /// Inserts or validates a record in a table. Inserts if primary keys do not match any row
        /// and validates if there is a match. Fails if the record does not match the data in
        /// the table. Fails if there is a record with a duplicate key that is not identical.
        /// Works only with read-write records. This mode cannot be used with a view containing joins.
        /// </summary>
        Merge = MsiInterop.MSIMODIFYMERGE,

        /// <summary>
        /// Refreshes the information in the record. Must first call Fetch with the
        /// same record. Fails for a deleted row. Works with read-write and read-only records.
        /// </summary>
        Refresh = MsiInterop.MSIMODIFYREFRESH,

        /// <summary>
        /// Updates or deletes and inserts a record into a table. Must first call Fetch with
        /// the same record. Updates record if the primary keys are unchanged. Deletes old row and
        /// inserts new if primary keys have changed. Fails with a read-only database. This mode cannot
        /// be used with a view containing joins.
        /// </summary>
        Replace = MsiInterop.MSIMODIFYREPLACE,

        /// <summary>
        /// Refreshes the information in the supplied record without changing the position in the
        /// result set and without affecting subsequent fetch operations. The record may then
        /// be used for subsequent Update, Delete, and Refresh. All primary key columns of the
        /// table must be in the query and the record must have at least as many fields as the
        /// query. Seek cannot be used with multi-table queries. This mode cannot be used with
        /// a view containing joins. See also the remarks.
        /// </summary>
        Seek = MsiInterop.MSIMODIFYSEEK,

        /// <summary>
        /// Updates an existing record. Non-primary keys only. Must first call Fetch. Fails with a
        /// deleted record. Works only with read-write records.
        /// </summary>
        Update = MsiInterop.MSIMODIFYUPDATE
    }

    /// <summary>
    /// Wrapper class for MSI API views.
    /// </summary>
    public sealed class View : MsiHandle
    {
        /// <summary>
        /// Constructor that creates a view given a database handle and a query.
        /// </summary>
        /// <param name="db">Handle to the database to run the query on.</param>
        /// <param name="query">Query to be executed.</param>
        public View(Database db, string query)
        {
            if (null == db)
            {
                throw new ArgumentNullException("db");
            }

            if (null == query)
            {
                throw new ArgumentNullException("query");
            }

            uint handle = 0;

            int error = MsiInterop.MsiDatabaseOpenView(db.Handle, query, out handle);
            if (0 != error)
            {
                throw new MsiException(error);
            }

            this.Handle = handle;
        }

        /// <summary>
        /// Executes a view with no customizable parameters.
        /// </summary>
        public void Execute()
        {
            this.Execute(null);
        }

        /// <summary>
        /// Executes a query substituing the values from the records into the customizable parameters 
        /// in the view.
        /// </summary>
        /// <param name="record">Record containing parameters to be substituded into the view.</param>
        public void Execute(Record record)
        {
            int error = MsiInterop.MsiViewExecute(this.Handle, null == record ? 0 : record.Handle);
            if (0 != error)
            {
                throw new MsiException(error);
            }
        }

        /// <summary>
        /// Fetches the next row in the view.
        /// </summary>
        /// <returns>Returns the fetched record; otherwise null.</returns>
        public Record Fetch()
        {
            uint recordHandle;

            int error = MsiInterop.MsiViewFetch(this.Handle, out recordHandle);
            if (259 == error)
            {
                return null;
            }
            else if (0 != error)
            {
                throw new MsiException(error);
            }

            return new Record(recordHandle);
        }

        /// <summary>
        /// Updates a fetched record.
        /// </summary>
        /// <param name="type">Type of modification mode.</param>
        /// <param name="record">Record to be modified.</param>
        public void Modify(ModifyView type, Record record)
        {
            int error = MsiInterop.MsiViewModify(this.Handle, Convert.ToInt32(type, CultureInfo.InvariantCulture), record.Handle);
            if (0 != error)
            {
                throw new MsiException(error);
            }
        }

        /// <summary>
        /// Returns a record containing column names or definitions.
        /// </summary>
        /// <param name="columnType">Specifies a flag indicating what type of information is needed. Either MSICOLINFO_NAMES or MSICOLINFO_TYPES.</param>
        /// <returns>The record containing information about the column.</returns>
        public Record GetColumnInfo(int columnType)
        {
            uint recordHandle;

            int error = MsiInterop.MsiViewGetColumnInfo(this.Handle, columnType, out recordHandle);
            if (0 != error)
            {
                throw new MsiException(error);
            }

            return new Record(recordHandle);
        }
    }
}
