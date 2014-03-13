using System;
using System.Data;

namespace Microsoft.Deployment.Samples.Inventory
{
	/// <summary>
	/// Reports the total number of items loaded so far by <see cref="IInventoryDataProvider.GetNodes"/>.
	/// </summary>
	public delegate void InventoryDataLoadStatusCallback(int itemsLoaded, string currentNode);

	/// <summary>
	/// Inventory data providers implement this interface to provide a particular type of data.
	/// Implementors must provide a parameterless constructor.
	/// </summary>
	public interface IInventoryDataProvider
	{
		/// <summary>
		/// Gets a description of the data provided.  This description allows
		/// the user to choose what type of data to gather.
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Gets the paths of all nodes for which this object provides data.
		/// </summary>
		/// <param name="statusCallback">Callback for reporting status.
		/// The callback should not necessarily be invoked for every individual
		/// node loaded, rather only every significant chunk.</param>
		/// <returns>An array of node paths. The parts of the node paths
		/// are delimited by backslashes (\).</returns>
		string[] GetNodes(InventoryDataLoadStatusCallback statusCallback);

		/// <summary>
		/// When related nodes of a tree consist of duplicate data, it's
		/// inefficient to search them all. This method indicates which
		/// nodes should be search and which should be ignored.
		/// </summary>
		/// <param name="searchRoot">Root node of the subtree-search.</param>
		/// <param name="searchNode">Node which may or may not be searched.</param>
		/// <returns>True if the node should be searched, false otherwise.</returns>
		bool IsNodeSearchable(string searchRoot, string searchNode);

		/// <summary>
		/// Gets the data for a particular node.
		/// </summary>
		/// <param name="nodePath">Path of the node for which data is requested.
		/// This is one of the paths returned by <see cref="GetNodes"/>.</param>
		/// <returns>DataView of a table filled with data, or null if data is
		/// not available.</returns>
		DataView GetData(string nodePath);

		/// <summary>
		/// Gets the path of another node which provides more details about
		/// a particular data row.
		/// </summary>
		/// <param name="nodePath">Path of the node containing the data
		/// row being queried.</param>
		/// <param name="row">Data row being queried.</param>
		/// <returns>Path to another node.  This is not necessarily
		/// one of the nodes returned by <see cref="GetNodes"/>.  If the
		/// node path is unknown, it will be ignored.  This method may
		/// return null if there is no detail node for the row.</returns>
		string GetLink(string nodePath, DataRow row);
	}
}
