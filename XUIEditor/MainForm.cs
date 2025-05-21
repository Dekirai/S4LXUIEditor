using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace XUIEditor
{
    public partial class MainForm : Form
    {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

        private string _baseFolder;
        private readonly Dictionary<TreeNode, (XuiElement element, string filePath)> nodeMap = new();
        private readonly HashSet<XuiElement> _temporarilyHidden = new();

        private XuiElement selectedElement;
        private string selectedFilePath;
        private PreviewForm previewWindow;

        public MainForm()
        {
            InitializeComponent();

            // Explorer look for tree
            SetWindowTheme(treeView1.Handle, "explorer", null);

            // set up folder / catalog icons...
            var imgs = new ImageList();
            using (Icon ico = NativeMethods.GetFolderIcon(true))
                imgs.Images.Add("folder", ico.ToBitmap());
            using (Icon cat = NativeMethods.GetStockIcon(NativeMethods.SHSTOCKICONID.SIID_STACK))
                imgs.Images.Add("catalog", cat.ToBitmap());
            treeView1.ImageList = imgs;

            // hook property‐grid to repaint tree‐node on Show change
            propertyGrid1.PropertyValueChanged += propertyGrid1_PropertyValueChanged;
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() != DialogResult.OK) return;
            _baseFolder = dlg.SelectedPath;

            treeView1.Nodes.Clear();
            nodeMap.Clear();
            propertyGrid1.SelectedObject = null;
            imageList1.Images.Clear();
            selectedFilePath = null;
            btnSave.Enabled = false;

            var uiNode = treeView1.Nodes.Add("UI");
            var popNode = treeView1.Nodes.Add("Popup");
            var otherNode = treeView1.Nodes.Add("Other");
            foreach (var n in new[] { uiNode, popNode, otherNode })
                n.ImageKey = n.SelectedImageKey = "folder";

            void EnsureXuiIcon()
            {
                const string ext = ".xui";
                if (!treeView1.ImageList.Images.ContainsKey(ext))
                {
                    using Icon fico = NativeMethods.GetFileIcon(".xml", smallSize: true);
                    treeView1.ImageList.Images.Add(ext, fico.ToBitmap());
                }
            }

            Action<string, TreeNode> loadFiles = (dir, parent) =>
            {
                if (!Directory.Exists(dir)) return;
                foreach (var file in Directory.GetFiles(dir, "*.xui", SearchOption.TopDirectoryOnly))
                {
                    EnsureXuiIcon();
                    var tn = parent.Nodes.Add(Path.GetFileName(file));
                    tn.Checked = true;
                    tn.ImageKey = tn.SelectedImageKey = ".xui";

                    var root = XuiParser.Parse(file);
                    nodeMap[tn] = (root, file);

                    // paint red if Show==false
                    tn.ForeColor = root.Show ? treeView1.ForeColor : Color.Red;

                    AddChildNodes(root, tn);
                }
            };

            loadFiles(Path.Combine(_baseFolder, "xui", "ui"), uiNode);
            loadFiles(Path.Combine(_baseFolder, "xui", "ui", "popup"), popNode);
            loadFiles(Path.Combine(_baseFolder, "gui", "new"), otherNode);

            //uiNode.Expand(); popNode.Expand(); otherNode.Expand();
            btnSave.Enabled = treeView1.Nodes
                                  .Cast<TreeNode>()
                                  .SelectMany(n => n.Nodes.Cast<TreeNode>())
                                  .Any();
        }

        private void AddChildNodes(XuiElement el, TreeNode parentNode)
        {
            foreach (var c in el.Children)
            {
                var n = parentNode.Nodes.Add(c.Name);
                n.Checked = true;
                n.ImageKey = n.SelectedImageKey = "catalog";
                nodeMap[n] = (c, nodeMap[parentNode].filePath);

                // paint red if Show==false
                n.ForeColor = c.Show ? treeView1.ForeColor : Color.Red;

                AddChildNodes(c, n);
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (!nodeMap.TryGetValue(e.Node, out var entry)) return;
            (selectedElement, selectedFilePath) = entry;
            propertyGrid1.SelectedObject = selectedElement;

            if (previewWindow != null && !previewWindow.IsDisposed)
                previewWindow.IgnoreShow = chbk_ignoreShow.Checked;

            if (selectedElement.Parent == null)
            {
                if (previewWindow == null || previewWindow.IsDisposed)
                {
                    previewWindow = new PreviewForm(_baseFolder);
                    previewWindow.ElementChanged += PreviewWindow_ElementChanged;
                    previewWindow.ElementClicked += PreviewWindow_ElementClicked;
                }
                previewWindow.IgnoreShow = chbk_ignoreShow.Checked;
                previewWindow.ShowXui(selectedElement, selectedFilePath);
            }
            else
            {
                previewWindow?.HighlightElement(selectedElement);
            }
        }

        private void PreviewWindow_ElementChanged(object sender, EventArgs e)
        {
            propertyGrid1.Refresh();
        }

        private void PreviewWindow_ElementClicked(object sender, XuiElement el)
        {
            var kv = nodeMap.FirstOrDefault(x => x.Value.element == el);
            if (kv.Key != null)
            {
                treeView1.SelectedNode = kv.Key;
                kv.Key.Expand();
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (selectedElement == null || selectedElement.Parent != null)
            {
                MessageBox.Show("Select the top‐level .xui node before saving.");
                return;
            }
            try
            {
                XuiParser.SaveMinimal(selectedElement, selectedFilePath);
                MessageBox.Show("Saved changes successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving: " + ex.Message);
            }
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            // if they toggled Show, repaint that node (and subtree)
            if (e.ChangedItem.PropertyDescriptor.Name == nameof(XuiElement.Show)
                && treeView1.SelectedNode != null)
            {
                void Recolor(TreeNode n)
                {
                    var el = nodeMap[n].element;
                    n.ForeColor = el.Show ? treeView1.ForeColor : Color.Red;
                    foreach (TreeNode c in n.Nodes) Recolor(c);
                }
                Recolor(treeView1.SelectedNode);
            }

            // always refresh preview
            if (previewWindow != null && !previewWindow.IsDisposed)
                previewWindow.RefreshPreview();
        }

        private void chbk_ignoreShow_CheckedChanged(object sender, EventArgs e)
        {
            if (previewWindow != null && !previewWindow.IsDisposed)
            {
                previewWindow.IgnoreShow = chbk_ignoreShow.Checked;
                if (selectedElement != null && selectedElement.Parent == null)
                    previewWindow.ShowXui(selectedElement, selectedFilePath);
            }
        }

        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (!nodeMap.TryGetValue(e.Node, out var entry)) return;
            var (el, filePath) = entry;
            if (e.Node.Checked) _temporarilyHidden.Remove(el);
            else _temporarilyHidden.Add(el);
            previewWindow?.SetHiddenElements(_temporarilyHidden);
            if (el.Parent == null) previewWindow?.ShowXui(el, filePath);
            else previewWindow?.RefreshPreview();
        }
    }
}
