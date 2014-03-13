//#define SINGLETHREAD
//
// Define SINGLETHREAD to run all processing tasks in the
// window thread instead of in the background.  This can
// make debugging easier.


using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Security.Permissions;
using System.Data;


[assembly: AssemblyDescription("Shows a hierarchical, relational, searchable " +
	" view of all of the product, feature, component, file, and patch data managed " +
	"by MSI, for all products installed on the system.")]

[assembly: SecurityPermission(SecurityAction.RequestMinimum, UnmanagedCode=true)]


namespace Microsoft.Deployment.Samples.Inventory
{
	public class Inventory : System.Windows.Forms.Form
	{
		[STAThread]
        public static void Main()
        {
            if (Microsoft.Deployment.WindowsInstaller.Installer.Version < new Version(3, 0))
            {
                MessageBox.Show("This application requires Windows Installer version 3.0 or later.",
                    "Inventory", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new Inventory());
        }

		private IInventoryDataProvider[] dataProviders;
		private Hashtable dataProviderMap;
		private Hashtable data;
		private ArrayList tablesLoading;
		private bool searching;
		private bool stopSearch;
		private bool navigating;
		private string continueSearchRoot;
		private string continueSearchPath;
		private DataGridCell continueSearchCell;
		private DataGridCell continueSearchEndCell;
		private bool mouseOverGridLink = false;
		private Stack historyBack;
		private Stack historyForward;
		private Stack cellHistoryBack;
		private Stack cellHistoryForward;
		private static readonly DataGridCell anyCell = new DataGridCell(-1,-1);
		private static readonly DataGridCell zeroCell = new DataGridCell(0,0);
        private static object syncRoot = new object();

		private System.Windows.Forms.DataGrid dataGrid;
		private System.Windows.Forms.TreeView treeView;
		private System.Windows.Forms.Panel toolPanel;
		private System.Windows.Forms.Splitter splitter;
		private System.Windows.Forms.Panel dataPanel;
		private System.Windows.Forms.Button backButton;
		private System.Windows.Forms.Button forwardButton;
		private System.Windows.Forms.Button findButton;
		private System.Windows.Forms.TextBox findTextBox;
		private System.Windows.Forms.Button refreshButton;
		private System.Windows.Forms.Button findStopButton;
		private System.Windows.Forms.CheckBox searchTreeCheckBox;
		private System.Windows.Forms.ToolTip gridLinkTip;
		private System.ComponentModel.IContainer components;

		public Inventory()
		{
			InitializeComponent();

			this.gridLinkTip.InitialDelay = 0;
			this.gridLinkTip.ReshowDelay = 0;

			this.dataProviderMap = new Hashtable();
			this.data = new Hashtable();
			this.tablesLoading = new ArrayList();
			this.historyBack = new Stack();
			this.historyForward = new Stack();
			this.cellHistoryBack = new Stack();
			this.cellHistoryForward = new Stack();
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.dataGrid = new System.Windows.Forms.DataGrid();
			this.treeView = new System.Windows.Forms.TreeView();
			this.toolPanel = new System.Windows.Forms.Panel();
			this.findStopButton = new System.Windows.Forms.Button();
			this.findButton = new System.Windows.Forms.Button();
			this.searchTreeCheckBox = new System.Windows.Forms.CheckBox();
			this.findTextBox = new System.Windows.Forms.TextBox();
			this.refreshButton = new System.Windows.Forms.Button();
			this.forwardButton = new System.Windows.Forms.Button();
			this.backButton = new System.Windows.Forms.Button();
			this.dataPanel = new System.Windows.Forms.Panel();
			this.splitter = new System.Windows.Forms.Splitter();
			this.gridLinkTip = new System.Windows.Forms.ToolTip(this.components);
			((System.ComponentModel.ISupportInitialize)(this.dataGrid)).BeginInit();
			this.toolPanel.SuspendLayout();
			this.dataPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// dataGrid
			// 
			this.dataGrid.DataMember = "";
			this.dataGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dataGrid.HeaderForeColor = System.Drawing.SystemColors.ControlText;
			this.dataGrid.Location = new System.Drawing.Point(230, 0);
			this.dataGrid.Name = "dataGrid";
			this.dataGrid.ReadOnly = true;
			this.dataGrid.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			this.dataGrid.Size = new System.Drawing.Size(562, 432);
			this.dataGrid.TabIndex = 1;
			this.dataGrid.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dataGrid_KeyDown);
			this.dataGrid.MouseDown += new System.Windows.Forms.MouseEventHandler(this.dataGrid_MouseDown);
			this.dataGrid.KeyUp += new System.Windows.Forms.KeyEventHandler(this.dataGrid_KeyUp);
			this.dataGrid.MouseMove += new System.Windows.Forms.MouseEventHandler(this.dataGrid_MouseMove);
			this.dataGrid.MouseLeave += new System.EventHandler(this.dataGrid_MouseLeave);
			// 
			// treeView
			// 
			this.treeView.Dock = System.Windows.Forms.DockStyle.Left;
			this.treeView.HideSelection = false;
			this.treeView.ImageIndex = -1;
			this.treeView.Location = new System.Drawing.Point(0, 0);
			this.treeView.Name = "treeView";
			this.treeView.SelectedImageIndex = -1;
			this.treeView.Size = new System.Drawing.Size(224, 432);
			this.treeView.TabIndex = 0;
			this.treeView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.treeView_KeyDown);
			this.treeView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Inventory_MouseDown);
			this.treeView.KeyUp += new System.Windows.Forms.KeyEventHandler(this.treeView_KeyUp);
			this.treeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeView_AfterSelect);
			// 
			// toolPanel
			// 
			this.toolPanel.Controls.Add(this.findStopButton);
			this.toolPanel.Controls.Add(this.findButton);
			this.toolPanel.Controls.Add(this.searchTreeCheckBox);
			this.toolPanel.Controls.Add(this.findTextBox);
			this.toolPanel.Controls.Add(this.refreshButton);
			this.toolPanel.Controls.Add(this.forwardButton);
			this.toolPanel.Controls.Add(this.backButton);
			this.toolPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.toolPanel.Location = new System.Drawing.Point(0, 0);
			this.toolPanel.Name = "toolPanel";
			this.toolPanel.Size = new System.Drawing.Size(792, 40);
			this.toolPanel.TabIndex = 2;
			// 
			// findStopButton
			// 
			this.findStopButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.findStopButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.findStopButton.Location = new System.Drawing.Point(704, 8);
			this.findStopButton.Name = "findStopButton";
			this.findStopButton.Size = new System.Drawing.Size(72, 25);
			this.findStopButton.TabIndex = 6;
			this.findStopButton.Text = "Stop";
			this.findStopButton.Visible = false;
			this.findStopButton.Click += new System.EventHandler(this.findStopButton_Click);
			// 
			// findButton
			// 
			this.findButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.findButton.Enabled = false;
			this.findButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.findButton.Location = new System.Drawing.Point(624, 8);
			this.findButton.Name = "findButton";
			this.findButton.Size = new System.Drawing.Size(72, 25);
			this.findButton.TabIndex = 4;
			this.findButton.Text = "Find";
			this.findButton.Click += new System.EventHandler(this.findButton_Click);
			// 
			// searchTreeCheckBox
			// 
			this.searchTreeCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.searchTreeCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.searchTreeCheckBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.searchTreeCheckBox.Location = new System.Drawing.Point(704, 10);
			this.searchTreeCheckBox.Name = "searchTreeCheckBox";
			this.searchTreeCheckBox.Size = new System.Drawing.Size(80, 22);
			this.searchTreeCheckBox.TabIndex = 5;
			this.searchTreeCheckBox.Text = "In Subtree";
			this.searchTreeCheckBox.CheckedChanged += new System.EventHandler(this.searchTreeCheckBox_CheckedChanged);
			// 
			// findTextBox
			// 
			this.findTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.findTextBox.Location = new System.Drawing.Point(344, 10);
			this.findTextBox.Name = "findTextBox";
			this.findTextBox.Size = new System.Drawing.Size(272, 20);
			this.findTextBox.TabIndex = 3;
			this.findTextBox.Text = "";
			this.findTextBox.TextChanged += new System.EventHandler(this.findTextBox_TextChanged);
			this.findTextBox.Enter += new System.EventHandler(this.findTextBox_Enter);
			// 
			// refreshButton
			// 
			this.refreshButton.Enabled = false;
			this.refreshButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.refreshButton.Location = new System.Drawing.Point(160, 8);
			this.refreshButton.Name = "refreshButton";
			this.refreshButton.Size = new System.Drawing.Size(72, 25);
			this.refreshButton.TabIndex = 2;
			this.refreshButton.Text = "Refresh";
			this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
			// 
			// forwardButton
			// 
			this.forwardButton.Enabled = false;
			this.forwardButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.forwardButton.Location = new System.Drawing.Point(80, 8);
			this.forwardButton.Name = "forwardButton";
			this.forwardButton.Size = new System.Drawing.Size(72, 25);
			this.forwardButton.TabIndex = 1;
			this.forwardButton.Text = "Forward";
			this.forwardButton.Click += new System.EventHandler(this.forwardButton_Click);
			// 
			// backButton
			// 
			this.backButton.Enabled = false;
			this.backButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.backButton.Location = new System.Drawing.Point(8, 8);
			this.backButton.Name = "backButton";
			this.backButton.Size = new System.Drawing.Size(72, 25);
			this.backButton.TabIndex = 0;
			this.backButton.Text = "Back";
			this.backButton.Click += new System.EventHandler(this.backButton_Click);
			// 
			// dataPanel
			// 
			this.dataPanel.Controls.Add(this.dataGrid);
			this.dataPanel.Controls.Add(this.splitter);
			this.dataPanel.Controls.Add(this.treeView);
			this.dataPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dataPanel.Location = new System.Drawing.Point(0, 40);
			this.dataPanel.Name = "dataPanel";
			this.dataPanel.Size = new System.Drawing.Size(792, 432);
			this.dataPanel.TabIndex = 1;
			// 
			// splitter
			// 
			this.splitter.Location = new System.Drawing.Point(224, 0);
			this.splitter.Name = "splitter";
			this.splitter.Size = new System.Drawing.Size(6, 432);
			this.splitter.TabIndex = 2;
			this.splitter.TabStop = false;
			// 
			// Inventory
			// 
			this.AcceptButton = this.findButton;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(792, 472);
			this.Controls.Add(this.dataPanel);
			this.Controls.Add(this.toolPanel);
			this.MinimumSize = new System.Drawing.Size(700, 0);
			this.Name = "Inventory";
			this.Text = "MSI Inventory";
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Inventory_KeyDown);
			this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Inventory_MouseDown);
			this.Load += new System.EventHandler(this.Inventory_Load);
			this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.Inventory_KeyUp);
			((System.ComponentModel.ISupportInitialize)(this.dataGrid)).EndInit();
			this.toolPanel.ResumeLayout(false);
			this.dataPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion


		#region DataProviders

		private IInventoryDataProvider[] DataProviders
		{
			get
			{
				if(this.dataProviders == null)
				{
					ArrayList providerList = new ArrayList();
					providerList.AddRange(FindDataProviders(Assembly.GetExecutingAssembly()));

					Uri codebase = new Uri(Assembly.GetExecutingAssembly().CodeBase);
					if(codebase.IsFile)
					{
						foreach(string module in Directory.GetFiles(Path.GetDirectoryName(codebase.LocalPath), "*Inventory.dll"))
						{
							try
							{
								providerList.AddRange(FindDataProviders(Assembly.LoadFrom(module)));
							}
							catch(Exception) { }
						}
					}

					this.dataProviders = (IInventoryDataProvider[]) providerList.ToArray(typeof(IInventoryDataProvider));
				}
				return this.dataProviders;
			}
		}

		private static IList FindDataProviders(Assembly assembly)
		{
			ArrayList providerList = new ArrayList();
			foreach(Type type in assembly.GetTypes())
			{
				if(type.IsClass)
				{
					foreach(Type implementedInterface in type.GetInterfaces())
					{
						if(implementedInterface.Equals(typeof(IInventoryDataProvider)))
						{
							try
							{
								providerList.Add(assembly.CreateInstance(type.FullName));
							}
							catch(Exception)
							{
								// Data provider's constructor threw an exception for some reason.
								// Well, now we can't get any data from that one.
							}
						}
					}
				}
			}
			return providerList;
		}

		#endregion

		private void GoTo(string nodePath, DataGridCell cell)
		{
			lock(syncRoot)
			{
				if(this.tablesLoading == null) return;  // The tree is being loaded
				if(this.navigating) return;  // This method is already on the callstack

				DataView table = (DataView) this.data[nodePath];
				if(table != null && table == this.dataGrid.DataSource)
				{
					// Grid is already in view
					if(!cell.Equals(anyCell)) this.dataGrid.CurrentCell = cell;
					return;
				}
				if(cell.Equals(anyCell)) cell = zeroCell;

				if(this.historyBack.Count == 0 || nodePath != (string) this.historyBack.Peek())
				{
					this.historyBack.Push(nodePath);
					if(this.cellHistoryBack.Count > 0 && this.historyForward != null)
					{
						this.cellHistoryBack.Pop();
						this.cellHistoryBack.Push(this.dataGrid.CurrentCell);
					}
					this.cellHistoryBack.Push(cell);
				}
				if(this.historyForward != null)
				{
					this.historyForward.Clear();
					this.cellHistoryForward.Clear();
				}

				if(table != null || nodePath.Length == 0 || this.dataProviderMap[nodePath] == null)
				{
					this.dataGrid.CaptionText = nodePath;
					this.dataGrid.CaptionBackColor = SystemColors.ActiveCaption;
					this.dataGrid.CaptionForeColor = SystemColors.ActiveCaptionText;
					this.dataGrid.DataSource = table;
					this.dataGrid.CurrentCell = cell;
					this.dataGrid.Focus();
				}
				else
				{
					this.dataGrid.CaptionText = nodePath + " (loading...)";
					this.dataGrid.CaptionBackColor = SystemColors.InactiveCaption;
					this.dataGrid.CaptionForeColor = SystemColors.InactiveCaptionText;
					this.dataGrid.DataSource = table;
					if(!this.tablesLoading.Contains(nodePath))
					{
						this.tablesLoading.Add(nodePath);
						this.SetCursor();
						#if SINGLETHREAD
						this.LoadTable(nodePath);
						#else
						new WaitCallback(this.LoadTable).BeginInvoke(nodePath, null, null);
						#endif
					}
				}

				this.findButton.Enabled = this.findTextBox.Text.Length > 0 && !searching;

				TreeNode treeNode = this.FindNode(nodePath);
				if(treeNode != this.treeView.SelectedNode)
				{
					this.navigating = true;
					this.treeView.SelectedNode = treeNode;
					this.navigating = false;
				}
			}
		}

		private void LoadTable(object nodePathObj)
		{
			string nodePath = (string) nodePathObj;
			IInventoryDataProvider dataProvider = (IInventoryDataProvider) this.dataProviderMap[nodePath];
			DataView table = null;
			if(dataProvider != null)
			{
				try
				{
					table = dataProvider.GetData(nodePath);
				}
				catch(Exception)
				{
					// Data provider threw an exception for some reason.
					// Treat it like it returned no data.
				}
			}

			lock(syncRoot)
			{
				if(this.tablesLoading == null || !tablesLoading.Contains(nodePath)) return;
				if(table == null)
				{
					this.dataProviderMap.Remove(nodePath);
				}
				else
				{
					this.data[nodePath] = table;
				}
				this.tablesLoading.Remove(nodePath);
			}
			#if SINGLETHREAD
			this.TableLoaded(nodePath);
			#else
			this.Invoke(new WaitCallback(this.TableLoaded), new object[] { nodePath });
			#endif
		}

		private void TableLoaded(object nodePathObj)
		{
			string nodePath = (string) nodePathObj;
			lock(syncRoot)
			{
				this.LoadTableStyle(nodePath);
				if(nodePath == this.CurrentNodePath)
				{
					this.dataGrid.CaptionBackColor = SystemColors.ActiveCaption;
					this.dataGrid.CaptionForeColor = SystemColors.ActiveCaptionText;
					this.dataGrid.CaptionText = nodePath;
					this.dataGrid.DataSource = this.CurrentTable;
					this.dataGrid.CurrentCell = (DataGridCell) this.cellHistoryBack.Peek();
					this.dataGrid.Focus();
				}
				this.SetCursor();
			}
		}

		private void RefreshData()
		{
			lock(syncRoot)
			{
				this.GoTo("", zeroCell);
				this.treeView.Nodes.Clear();
				this.dataGrid.TableStyles.Clear();
				this.dataGrid.CaptionBackColor = SystemColors.InactiveCaption;
				this.dataGrid.CaptionForeColor = SystemColors.InactiveCaptionText;
				this.SetControlsEnabled(false);
				this.treeView.BeginUpdate();
				#if SINGLETHREAD
				this.LoadTree();
				#else
				new ThreadStart(this.LoadTree).BeginInvoke(null, null);
				#endif
			}
		}

		private void SetControlsEnabled(bool enabled)
		{
			this.backButton.Enabled = enabled && this.historyBack.Count > 1;
			this.forwardButton.Enabled = enabled && this.historyForward.Count > 0;
			this.refreshButton.Enabled = enabled;
			this.findButton.Enabled = enabled && this.findTextBox.Text.Length > 0 && !searching;
		}

		private WaitCallback treeStatusCallback;
		private int treeNodesLoaded;
		private int treeNodesLoadedBase;
        private string treeNodesLoading;
		private void TreeLoadDataProviderStatus(int status, string currentNode)
		{
            if (currentNode != null)
            {
                this.treeNodesLoading = currentNode;
            }

			this.treeNodesLoaded = treeNodesLoadedBase + status;
            string statusString = String.Format("Loading tree... " + this.treeNodesLoaded);
            if (!String.IsNullOrEmpty(this.treeNodesLoading))
            {
                statusString += ": " + treeNodesLoading;
            }

			#if SINGLETHREAD
			treeStatusCallback(statusString);
			#else
			this.Invoke(treeStatusCallback, new object[] { statusString });
			#endif
		}

		private void UpdateTreeLoadStatus(object status)
		{
			if(status == null)
			{
				// Loading is complete.
				this.treeView.EndUpdate();
				this.SetCursor();
				this.GoTo("Products", new DataGridCell(0, 0));
				this.SetControlsEnabled(true);
			}
			else
			{
				this.dataGrid.CaptionText = (string) status;
			}
		}

		private void LoadTree()
		{
			lock(syncRoot)
			{
				if(this.tablesLoading == null) return;
				this.tablesLoading = null;
				this.dataProviderMap.Clear();
				this.data.Clear();
				this.Invoke(new ThreadStart(this.SetCursor));
			}

			this.treeStatusCallback = new WaitCallback(UpdateTreeLoadStatus);
			this.LoadTreeNodes();
			this.RenderTreeNodes();

			lock(syncRoot)
			{
				this.tablesLoading = new ArrayList();
			}
			// Use a status of null to signal loading complete.
			#if SINGLETHREAD
			this.UpdateTreeLoadStatus(null);
			#else
			this.Invoke(new WaitCallback(this.UpdateTreeLoadStatus), new object[] { null });
			#endif
		}

		private void LoadTreeNodes()
		{
			#if SINGLETHREAD
			this.treeStatusCallback("Loading tree... ");
			#else
			this.Invoke(this.treeStatusCallback, new object[] { "Loading tree... " });
			#endif
			this.treeNodesLoaded = 0;
            this.treeNodesLoading = null;
			foreach(IInventoryDataProvider dataProvider in this.DataProviders)
			{
				this.treeNodesLoadedBase = this.treeNodesLoaded;
				string[] nodePaths = null;
				try
				{
					nodePaths = dataProvider.GetNodes(new InventoryDataLoadStatusCallback(this.TreeLoadDataProviderStatus));
				}
				catch(Exception)
				{
					// Data provider threw an exception for some reason.
					// Treat it like it returned no data.
				}
				if(nodePaths != null)
				{
					foreach(string nodePath in nodePaths)
					{
						if(!this.dataProviderMap.Contains(nodePath))
						{
							this.dataProviderMap.Add(nodePath, dataProvider);
						}
					}
				}
			}
		}

		private void RenderTreeNodes()
		{
			#if SINGLETHREAD
			this.treeStatusCallback("Rendering tree... ");
			#else
			this.Invoke(this.treeStatusCallback, new object[] { "Rendering tree... " });
			#endif
			this.treeNodesLoaded = 0;
			foreach(DictionaryEntry nodePathAndProvider in this.dataProviderMap)
			{
				string nodePath = (string) nodePathAndProvider.Key;
				#if SINGLETHREAD
				this.AddNode(nodePath);
				#else
				this.Invoke(new WaitCallback(this.AddNode), new object[] { nodePath });
				#endif
			}
		}

		private void LoadTableStyle(string nodePath)
		{
			DataView table = (DataView) this.data[nodePath];
			if(table != null)
			{
				DataGridTableStyle tableStyle = this.dataGrid.TableStyles[table.Table.TableName];
				if(tableStyle == null)
				{
					tableStyle = new DataGridTableStyle();
					tableStyle.MappingName = table.Table.TableName;
					tableStyle.RowHeadersVisible = true;
					this.dataGrid.TableStyles.Add(tableStyle);
				}
				foreach(DataColumn column in table.Table.Columns)
				{
					if(!tableStyle.GridColumnStyles.Contains(column.ColumnName))
					{
						string colStyle = (string) ColumnResources.GetObject(column.ColumnName, CultureInfo.InvariantCulture);
						if(colStyle != null)
						{
							string[] colStyleParts = colStyle.Split(',');
							DataGridColumnStyle columnStyle = (colStyleParts.Length > 2 && colStyleParts[2] == "bool"
								? (DataGridColumnStyle) new DataGridBoolColumn() : (DataGridColumnStyle) new DataGridTextBoxColumn());
							try { if(colStyleParts.Length > 1) columnStyle.Width = Int32.Parse(colStyleParts[1]); }
							catch(FormatException) { }
							columnStyle.HeaderText = colStyleParts[0];
							columnStyle.MappingName = column.ColumnName;
							tableStyle.GridColumnStyles.Add(columnStyle);
						}
					}
				}
			}
		}

		private static ResourceManager ColumnResources
		{
			get
			{
				if(columnResources == null)
				{
					columnResources = new ResourceManager(typeof(Inventory).Name + ".Columns", typeof(Inventory).Assembly);
				}
				return columnResources;
			}
		}
		private static ResourceManager columnResources;

		private void AddNode(object nodePathObj)
		{
			string nodePath = (string) nodePathObj;
			string[] path = nodePath.Split('\\');
			TreeNodeCollection nodes = this.treeView.Nodes;
			TreeNode node = null;
			foreach(string pathPart in path)
			{
				node = null;
				for(int i = 0; i < nodes.Count; i++)
				{
					int c = string.CompareOrdinal(nodes[i].Text, pathPart);
					if(c == 0)
					{
						node = nodes[i];
						break;
					}
					else if(c > 0)
					{
						node = new TreeNode(pathPart);
						nodes.Insert(i, node);
						break;
					}
				}
				if(node == null)
				{
					node = new TreeNode(pathPart);
					nodes.Add(node);
				}
				nodes = node.Nodes;
			}
			if(++this.treeNodesLoaded % 1000 == 0)
			{
				this.UpdateTreeLoadStatus("Rendering tree... " +
					(100 * this.treeNodesLoaded / this.dataProviderMap.Count) + "%");
			}
		}

		public string CurrentNodePath
		{
			get
			{
				TreeNode currentNode = this.treeView.SelectedNode;
				return currentNode != null ? currentNode.FullPath : null;
			}
		}

		public DataView CurrentTable
		{
			get
			{
				string currentNodePath = this.CurrentNodePath;
				return currentNodePath != null ? (DataView) this.data[this.CurrentNodePath] : null;
			}
		}

		private TreeNode FindNode(string nodePath)
		{
			if(nodePath == null) return null;
			string[] path = nodePath.Split('\\');
			TreeNodeCollection nodes = this.treeView.Nodes;
			TreeNode node = null;
			foreach(string pathPart in path)
			{
				node = null;
				for(int i = 0; i < nodes.Count; i++)
				{
					if(nodes[i].Text == pathPart)
					{
						node = nodes[i];
						break;
					}
				}
				if(node != null)
				{
					nodes = node.Nodes;
				}
			}
			return node;
		}

		private void dataGrid_MouseDown(object sender, MouseEventArgs e)
		{
			Keys modKeys = Control.ModifierKeys;
			if(e.Button == MouseButtons.Left && (modKeys & (Keys.Shift | Keys.Control)) == 0)
			{
				DataGrid.HitTestInfo hit = this.dataGrid.HitTest(e.X, e.Y);
				string link = this.GetLinkForGridHit(hit);
				if(link != null)
				{
					TreeNode node = this.FindNode(link);
					if(node != null)
					{
						this.treeView.SelectedNode = node;
						node.Expand();
					}
				}
			}
			this.Inventory_MouseDown(sender, e);
		}

		private void dataGrid_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			//this.gridLinkTip.SetToolTip(this.dataGrid, null);
			DataGrid.HitTestInfo hit = this.dataGrid.HitTest(e.X, e.Y);
			if(hit.Type == DataGrid.HitTestType.RowHeader)
			{
				string link = this.GetLinkForGridHit(hit);
				if(link != null)
				{
					this.mouseOverGridLink = true;
					this.SetCursor();
					return;
				}
			}
			else if(this.mouseOverGridLink)
			{
				this.mouseOverGridLink = false;
				this.SetCursor();
			}
		}
		
		private void dataGrid_MouseLeave(object sender, System.EventArgs e)
		{
			this.mouseOverGridLink = false;
			this.SetCursor();
		}

		private string GetLinkForGridHit(DataGrid.HitTestInfo hit)
		{
			if(hit.Type == DataGrid.HitTestType.RowHeader && this.tablesLoading != null)
			{
				string nodePath = this.CurrentNodePath;
				DataView table = (DataView) this.data[nodePath];
				if(table != null)
				{
					DataRow row = table[hit.Row].Row;
					IInventoryDataProvider dataProvider = (IInventoryDataProvider) this.dataProviderMap[nodePath];
					return dataProvider.GetLink(nodePath, table[hit.Row].Row);
				}
			}
			return null;
		}

		private void HistoryBack()
		{
			lock(syncRoot)
			{
				if(this.historyBack.Count > 1)
				{
					string nodePath = (string) this.historyBack.Pop();
					this.cellHistoryBack.Pop();
					DataGridCell cell = this.dataGrid.CurrentCell;
					Stack saveForward = this.historyForward;
					this.historyForward = null;
					this.GoTo((string) this.historyBack.Pop(), (DataGridCell) this.cellHistoryBack.Pop());
					this.historyForward = saveForward;
					this.historyForward.Push(nodePath);
					this.cellHistoryForward.Push(cell);
					this.backButton.Enabled = this.historyBack.Count > 1;
					this.forwardButton.Enabled = this.historyForward.Count > 0;
				}
			}
		}

		private void HistoryForward()
		{
			lock(syncRoot)
			{
				if(this.historyForward.Count > 0)
				{
					string nodePath = (string) this.historyForward.Pop();
					DataGridCell cell = (DataGridCell) this.cellHistoryForward.Pop();
					Stack saveForward = this.historyForward;
					this.historyForward = null;
					this.GoTo(nodePath, cell);
					this.historyForward = saveForward;
					this.backButton.Enabled = this.historyBack.Count > 1;
					this.forwardButton.Enabled = this.historyForward.Count > 0;
				}
			}
		}

		#region Find

		private void Find()
		{
			this.BeginFind();
            object[] findNextArgs = new object[] { this.CurrentNodePath, this.dataGrid.CurrentCell, this.treeView.SelectedNode };
			#if SINGLETHREAD
			this.FindNext(findNextArgs);
			#else
            new WaitCallback(this.FindNext).BeginInvoke(findNextArgs, null, null);
			#endif
		}

        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
		private void FindNext(object start)
		{
			string nodePath = (string) ((object[]) start)[0];
			DataGridCell startCell = (DataGridCell) ((object[]) start)[1];
            TreeNode searchNode = (TreeNode) ((object[]) start)[2];
			DataGridCell endCell = startCell;

			string searchString = this.findTextBox.Text;
			if(searchString.Length == 0) return;

			bool ignoreCase = true; // TODO: make this a configurable option?
			if(ignoreCase) searchString = searchString.ToLowerInvariant();
		
			if(!this.searchTreeCheckBox.Checked)
			{
				DataGridCell foundCell;
				startCell.ColumnNumber++;
				if(FindInTable((DataView) this.data[nodePath], searchString, ignoreCase,
					startCell, startCell, true, out foundCell))
				{
					#if SINGLETHREAD
					this.EndFind(new object[] { nodePath, foundCell });
					#else
					this.Invoke(new WaitCallback(this.EndFind), new object[] { new object[] { nodePath, foundCell } });
					#endif
					return;
				}
			}
			else
			{
				if(this.continueSearchRoot != null)
				{
					searchNode = this.FindNode(this.continueSearchRoot);
					startCell = this.continueSearchCell;
					endCell = this.continueSearchEndCell;
				}
				else
				{
					this.continueSearchRoot = searchNode.FullPath;
					this.continueSearchPath = this.continueSearchRoot;
					this.continueSearchEndCell = endCell;
				}
				//if(searchNode == null) return;
				ArrayList nodesList = new ArrayList();
				nodesList.Add(searchNode);
				this.GetFlatTreeNodes(searchNode.Nodes, nodesList, true, this.continueSearchRoot);
				TreeNode[] nodes = (TreeNode[]) nodesList.ToArray(typeof(TreeNode));
				int startNode = nodesList.IndexOf(this.FindNode(this.continueSearchPath));
				DataGridCell foundCell;
				startCell.ColumnNumber++;
				for(int i = startNode; i < nodes.Length; i++)
				{
					if(this.stopSearch) break;
					DataGridCell startCellOnThisNode = zeroCell;
					if(i == startNode) startCellOnThisNode = startCell;
					DataView table = this.GetTableForSearch(nodes[i].FullPath);
					if(table != null)
					{
						if(FindInTable(table, searchString, ignoreCase, startCellOnThisNode, zeroCell, false, out foundCell))
						{
							#if SINGLETHREAD
							this.EndFind(new object[] { nodes[i].FullPath, foundCell });
							#else
							this.Invoke(new WaitCallback(this.EndFind), new object[] { new object[] { nodes[i].FullPath, foundCell } });
							#endif
							return;
						}
					}
				}
				if(!this.stopSearch)
				{
					DataView table = this.GetTableForSearch(searchNode.FullPath);
					if(table != null)
					{
						if(FindInTable(table, searchString, ignoreCase, zeroCell, endCell, false, out foundCell))
						{
							#if SINGLETHREAD
							this.EndFind(new object[] { searchNode.FullPath, foundCell });
							#else
							this.Invoke(new WaitCallback(this.EndFind), new object[] { new object[] { searchNode.FullPath, foundCell } });
							#endif
							return;
						}
					}
				}
			}
			#if SINGLETHREAD
			this.EndFind(null);
			#else
			this.Invoke(new WaitCallback(this.EndFind), new object[] { null });
			#endif
		}

		private DataView GetTableForSearch(string nodePath)
		{
			DataView table = (DataView) this.data[nodePath];
			string status = nodePath;
			if(table == null) status = status + " (loading)";
			#if SINGLETHREAD
			this.FindStatus(nodePath);
			#else
			this.Invoke(new WaitCallback(this.FindStatus), new object[] { status });
			#endif
			if(table == null)
			{
				this.tablesLoading.Add(nodePath);
				this.Invoke(new ThreadStart(this.SetCursor));
				this.LoadTable(nodePath);
				table = (DataView) this.data[nodePath];
			}
			return table;
		}

		private void GetFlatTreeNodes(TreeNodeCollection nodes, IList resultsList, bool searchable, string searchRoot)
		{
			foreach(TreeNode node in nodes)
			{
				string nodePath = node.FullPath;
				IInventoryDataProvider dataProvider = (IInventoryDataProvider) this.dataProviderMap[nodePath];
				if(!searchable || (dataProvider != null && dataProvider.IsNodeSearchable(searchRoot, nodePath)))
				{
					resultsList.Add(node);
				}
				GetFlatTreeNodes(node.Nodes, resultsList, searchable, searchRoot);
			}
		}

        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
		private bool FindInTable(DataView table, string searchString, bool lowerCase,
			DataGridCell startCell, DataGridCell endCell, bool wrap, out DataGridCell foundCell)
		{
			foundCell = new DataGridCell(-1, -1);
			if(table == null) return false;
			if(startCell.RowNumber < 0) startCell.RowNumber = 0;
			if(startCell.ColumnNumber < 0) startCell.ColumnNumber = 0;
			for(int searchRow = startCell.RowNumber; searchRow < table.Count; searchRow++)
			{
				if(this.stopSearch) break;
				if(endCell.RowNumber > startCell.RowNumber && searchRow > endCell.RowNumber) break;

				DataRowView tableRow = table[searchRow];
				for(int searchCol = (searchRow == startCell.RowNumber
					? startCell.ColumnNumber : 0); searchCol < table.Table.Columns.Count; searchCol++)
				{
					if(this.stopSearch) break;
					if(endCell.RowNumber > startCell.RowNumber && searchRow == endCell.RowNumber
						&& searchCol >= endCell.ColumnNumber) break;

					string value = tableRow[searchCol].ToString();
					if(lowerCase) value = value.ToLowerInvariant();
					if(value.IndexOf(searchString, StringComparison.Ordinal) >= 0)
					{
						foundCell.RowNumber = searchRow;
						foundCell.ColumnNumber = searchCol;
						return true;
					}
				}
			}
			if(wrap)
			{
				for(int searchRow = 0; searchRow <= endCell.RowNumber; searchRow++)
				{
					if(this.stopSearch) break;
					DataRowView tableRow = table[searchRow];
					for(int searchCol = 0; searchCol < (searchRow == endCell.RowNumber
						? endCell.ColumnNumber : table.Table.Columns.Count); searchCol++)
					{
						if(this.stopSearch) break;
						string value = tableRow[searchCol].ToString();
						if(lowerCase) value = value.ToLowerInvariant();
						if(value.IndexOf(searchString, StringComparison.Ordinal) >= 0)
						{
							foundCell.RowNumber = searchRow;
							foundCell.ColumnNumber = searchCol;
							return true;
						}
					}
				}
			}
			return false;
		}

		private void BeginFind()
		{
			lock(syncRoot)
			{
				this.findButton.Enabled = false;
				this.findButton.Text = "Searching...";
				this.findTextBox.Enabled = false;
				this.searchTreeCheckBox.Visible = false;
				this.findStopButton.Visible = true;
				this.refreshButton.Enabled = false;
				this.searching = true;
				this.stopSearch = false;
				this.SetCursor();
			}
		}

		private void FindStatus(object status)
		{
			lock(syncRoot)
			{
				this.dataGrid.CaptionText = "Searching... " + (string) status;
				this.dataGrid.CaptionBackColor = SystemColors.InactiveCaption;
				this.dataGrid.CaptionForeColor = SystemColors.InactiveCaptionText;
			}
		}

		private void EndFind(object result)
		{
			lock(syncRoot)
			{
				this.searching = false;
				this.refreshButton.Enabled = true;
				this.findStopButton.Visible = false;
				this.searchTreeCheckBox.Visible = true;
				this.findTextBox.Enabled = true;
				this.findButton.Text = "Find";
				this.findButton.Enabled = true;
				this.dataGrid.CaptionBackColor = SystemColors.ActiveCaption;
				this.dataGrid.CaptionForeColor = SystemColors.ActiveCaptionText;
				this.dataGrid.CaptionText = this.CurrentNodePath;
				if(result != null)
				{
					string nodePath = (string) ((object[]) result)[0];
					DataGridCell foundCell = (DataGridCell) ((object[]) result)[1];
					this.GoTo(nodePath, foundCell);
					this.dataGrid.Focus();
					this.continueSearchPath = nodePath;
					this.continueSearchCell = foundCell;
					if(this.searchTreeCheckBox.Checked) this.searchTreeCheckBox.Text = "Continue";
				}
				else
				{
					this.continueSearchRoot = null;
					this.continueSearchPath = null;
					this.searchTreeCheckBox.Text = "In Subtree";
				}
				this.SetCursor();
			}
		}

		private void SetCursor()
		{
			if(this.mouseOverGridLink)
			{
				Keys modKeys = Control.ModifierKeys;
				if((modKeys & (Keys.Shift | Keys.Control)) == 0)
				{
					this.Cursor = Cursors.Hand;
					return;
				}
			}
			if(this.tablesLoading == null || this.tablesLoading.Count > 0 || this.searching)
			{
				this.Cursor = Cursors.AppStarting;
				return;
			}
			this.Cursor = Cursors.Arrow;
		}

		#endregion

		#region EventHandlers

		private void Inventory_Load(object sender, System.EventArgs e)
		{
			this.RefreshData();
		}
		private void refreshButton_Click(object sender, System.EventArgs e)
		{
			this.RefreshData();
		}
		private void Inventory_MouseDown(object sender, MouseEventArgs e)
		{
			if(e.Button == MouseButtons.XButton1) this.HistoryBack();
			else if(e.Button == MouseButtons.XButton2) this.HistoryForward();
		}
		private void Inventory_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			this.SetCursor();
			if(e.KeyCode == Keys.F3) this.Find();
			else if(e.KeyCode == Keys.F && (e.Modifiers | Keys.Control) != 0) this.findTextBox.Focus();
			else if(e.KeyCode == Keys.BrowserBack) this.HistoryBack();
			else if(e.KeyCode == Keys.BrowserForward) this.HistoryForward();
			else return;
			e.Handled = true;
		}
		private void treeView_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			this.Inventory_KeyDown(sender, e);
		}
		private void dataGrid_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			this.Inventory_KeyDown(sender, e);
		}
		private void Inventory_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			this.SetCursor();
		}
		private void treeView_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			this.Inventory_KeyDown(sender, e);
		}
		private void dataGrid_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			this.Inventory_KeyDown(sender, e);
		}
		private void treeView_AfterSelect(object sender, System.Windows.Forms.TreeViewEventArgs e)
		{
			this.GoTo(e.Node.FullPath, anyCell);
		}
		private void backButton_Click(object sender, System.EventArgs e)
		{
			this.HistoryBack();
		}
		private void forwardButton_Click(object sender, System.EventArgs e)
		{
			this.HistoryForward();		
		}
		private void findTextBox_TextChanged(object sender, System.EventArgs e)
		{
			this.findButton.Enabled = this.findTextBox.Text.Length > 0 &&
				this.tablesLoading != null && this.treeView.SelectedNode != null && !searching;
			this.searchTreeCheckBox.Text = "In Subtree";
			this.continueSearchRoot = null;
		}
		private void findButton_Click(object sender, System.EventArgs e)
		{
			this.Find();
		}
		private void findTextBox_Enter(object sender, System.EventArgs e)
		{
			findTextBox.SelectAll();
		}
		private void findStopButton_Click(object sender, System.EventArgs e)
		{
			this.stopSearch = true;
		}

		private void searchTreeCheckBox_CheckedChanged(object sender, System.EventArgs e)
		{
			if(!searchTreeCheckBox.Checked && searchTreeCheckBox.Text == "Continue")
			{
				this.searchTreeCheckBox.Text = "In Subtree";
			}
		}

		#endregion

	}
}
