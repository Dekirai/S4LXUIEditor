using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace XUIEditor
{
    public partial class PreviewForm : Form
    {
        private readonly string _gameResourcesRoot;

        private XuiElement rootElement;
        private string xuiFilePath;
        private readonly Dictionary<string, Image> textures = new();
        private readonly Dictionary<string, Image> _rawImageCache = new();

        private HashSet<XuiElement> _hidden = new();

        private XuiElement dragEl;
        private Point dragOffset;
        private XuiElement hoverEl;
        private XuiElement selectedEl;

        private Bitmap fullCache;
        private Bitmap cacheWithoutDrag;
        private bool isDragging;

        private int _minX, _minY;

        private const int DRAW_OFFSET_X = 170;
        private Point originOffset;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IgnoreShow { get; set; }

        public event EventHandler ElementChanged;
        public event EventHandler<XuiElement> ElementClicked;

        public PreviewForm(string gameResourcesRoot)
        {
            InitializeComponent();

            _gameResourcesRoot = gameResourcesRoot;

            typeof(Panel).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic)
              .SetValue(previewPanel, true, null);

            KeyPreview = true;
        }

        public void SetHiddenElements(HashSet<XuiElement> hidden)
        {
            _hidden = hidden;
        }

        public void ShowXui(XuiElement root, string filePath)
        {
            rootElement = root;
            xuiFilePath = filePath;
            dragEl = hoverEl = selectedEl = null;
            txtLog.Clear();

            LoadTextures();

            var all = GetAllElements(rootElement)
                .Where(el => IgnoreShow || el.Show)
                .ToList();
            int minX = all.Min(e => e.X);
            int minY = all.Min(e => e.Y);
            originOffset = new Point(minX < 0 ? -minX : 0, minY < 0 ? -minY : 0);

            UpdateScrollArea();
            isDragging = false;
            previewPanel.Invalidate();

            if (!Visible) Show();
        }

        public void HighlightElement(XuiElement el)
        {
            selectedEl = el;
            previewPanel.Invalidate();
        }

        public void RefreshPreview()
        {
            RebuildFullCache();
            previewPanel.Invalidate();
        }

        private void LoadTextures()
        {
            txtLog.Clear();
            textures.Clear();

            var xuiFolder = Path.GetDirectoryName(xuiFilePath)!;
            var resourcesRoot = _gameResourcesRoot;

            var imageDir = Path.Combine(resourcesRoot, "xui", "image");
            var guiNewDir = Path.Combine(resourcesRoot, "gui", "new");
            var exts = new[] { ".tga", ".dds", ".png" };

            foreach (var el in GetAllElements(rootElement).Reverse())
            {
                el.ElementImage = null;
                if (!IgnoreShow && !el.Show) continue;
                if (string.IsNullOrEmpty(el.TexturePath)) continue;

                string candidate = Path.IsPathRooted(el.TexturePath)
                    ? el.TexturePath
                    : Path.Combine(xuiFolder, el.TexturePath);

                if (!File.Exists(candidate))
                {
                    var baseName = Path.GetFileNameWithoutExtension(el.TexturePath);
                    string found = null;

                    if (Directory.Exists(imageDir))
                    {
                        foreach (var ext in exts)
                        {
                            var flat = Path.Combine(imageDir, baseName + ext);
                            if (File.Exists(flat)) { found = flat; break; }
                        }
                        if (found == null)
                            found = Directory.EnumerateFiles(imageDir, baseName + ".*", SearchOption.AllDirectories)
                                             .FirstOrDefault(f => exts.Contains(Path.GetExtension(f)));
                    }

                    if (found == null && Directory.Exists(guiNewDir))
                    {
                        foreach (var ext in exts)
                        {
                            var flat = Path.Combine(guiNewDir, baseName + ext);
                            if (File.Exists(flat)) { found = flat; break; }
                        }
                        if (found == null)
                            found = Directory.EnumerateFiles(guiNewDir, baseName + ".*", SearchOption.AllDirectories)
                                             .FirstOrDefault(f => exts.Contains(Path.GetExtension(f)));
                    }

                    if (found == null && baseName.Contains("_rc_"))
                    {
                        var alt = baseName.Replace("_rc_", "_eu_");
                        if (Directory.Exists(imageDir))
                        {
                            foreach (var ext in exts)
                            {
                                var flat = Path.Combine(imageDir, alt + ext);
                                if (File.Exists(flat)) { found = flat; break; }
                            }
                            if (found == null)
                                found = Directory.EnumerateFiles(imageDir, alt + ".*", SearchOption.AllDirectories)
                                                 .FirstOrDefault(f => exts.Contains(Path.GetExtension(f)));
                        }

                        if (found == null && Directory.Exists(guiNewDir))
                        {
                            foreach (var ext in exts)
                            {
                                var flat = Path.Combine(guiNewDir, alt + ext);
                                if (File.Exists(flat)) { found = flat; break; }
                            }
                            if (found == null)
                                found = Directory.EnumerateFiles(guiNewDir, alt + ".*", SearchOption.AllDirectories)
                                                 .FirstOrDefault(f => exts.Contains(Path.GetExtension(f)));
                        }
                    }

                    if (found == null && baseName.Contains("_lac_"))
                    {
                        var alt = baseName.Replace("_lac_", "_eng_");
                        if (Directory.Exists(imageDir))
                        {
                            foreach (var ext in exts)
                            {
                                var flat = Path.Combine(imageDir, alt + ext);
                                if (File.Exists(flat)) { found = flat; break; }
                            }
                            if (found == null)
                                found = Directory.EnumerateFiles(imageDir, alt + ".*", SearchOption.AllDirectories)
                                                 .FirstOrDefault(f => exts.Contains(Path.GetExtension(f)));
                        }
                        if (found == null && Directory.Exists(guiNewDir))
                        {
                            foreach (var ext in exts)
                            {
                                var flat = Path.Combine(guiNewDir, alt + ext);
                                if (File.Exists(flat)) { found = flat; break; }
                            }
                            if (found == null)
                                found = Directory.EnumerateFiles(guiNewDir, alt + ".*", SearchOption.AllDirectories)
                                                 .FirstOrDefault(f => exts.Contains(Path.GetExtension(f)));
                        }
                    }

                    if (found != null)
                        candidate = found;
                }

                if (!File.Exists(candidate))
                {
                    txtLog.AppendText($"Couldn't find file:    {candidate}\r\n");
                    continue;
                }

                if (!_rawImageCache.TryGetValue(candidate, out var atlas))
                {
                    try
                    {
                        var data = File.ReadAllBytes(candidate);
                        atlas = ImageLoader.Load(data, Path.GetExtension(candidate))
                             ?? throw new Exception("loader returned null");
                        _rawImageCache[candidate] = atlas;
                        txtLog.AppendText($"Loaded file:    {candidate}\r\n");
                    }
                    catch
                    {
                        txtLog.AppendText($"Failed to load: {candidate}\r\n");
                        continue;
                    }
                }

                if (el.U1 > el.U0 && atlas is Bitmap bmp)
                {
                    int aw = bmp.Width, ah = bmp.Height;
                    var crop = Rectangle.Intersect(
                        new Rectangle(
                            (int)(el.U0 * aw),
                            (int)(el.V0 * ah),
                            (int)((el.U1 - el.U0) * aw),
                            (int)((el.V1 - el.V0) * ah)
                        ),
                        new Rectangle(0, 0, aw, ah)
                    );
                    el.ElementImage = (crop.Width > 0 && crop.Height > 0)
                        ? (Bitmap)bmp.Clone(crop, bmp.PixelFormat)
                        : atlas;
                }
                else
                {
                    el.ElementImage = atlas;
                }

                textures[candidate] = el.ElementImage;
            }
        }

        private void UpdateScrollArea()
        {
            var all = GetAllElements(rootElement)
                .Where(el => IgnoreShow || el.Show);
            int maxX = all.Max(e => e.X + e.Width);
            int maxY = all.Max(e => e.Y + e.Height);

            previewPanel.AutoScrollMinSize = new Size(
                originOffset.X + maxX,
                originOffset.Y + maxY);
        }

        private void RebuildFullCache()
        {
            fullCache?.Dispose();
            var size = previewPanel.AutoScrollMinSize;
            if (size.Width <= 0 || size.Height <= 0) return;

            fullCache = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(fullCache);
            DrawCacheTree(g, rootElement);
        }

        private void PreviewPanel_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.TranslateTransform(
                previewPanel.AutoScrollPosition.X + originOffset.X,
                previewPanel.AutoScrollPosition.Y + originOffset.Y);

            DrawCacheTree(e.Graphics, rootElement);

            if (isDragging && dragEl != null)
            {
                DrawCacheTreeExcept(e.Graphics, rootElement, dragEl);
                DrawCacheTree(e.Graphics, dragEl);
            }

            if (hoverEl != null)
            {
                using var blue = new Pen(Color.Blue, 2);
                e.Graphics.DrawRectangle(blue,
                    hoverEl.X, hoverEl.Y,
                    hoverEl.Width, hoverEl.Height);

                var name = hoverEl.Name;
                using var font = new Font("Segoe UI", 9f);
                var sz = e.Graphics.MeasureString(name, font);
                float tx = hoverEl.X + (hoverEl.Width - sz.Width) / 2;
                float ty = hoverEl.Y + hoverEl.Height + 4;
                var bg = new RectangleF(tx - 2, ty - 2, sz.Width + 4, sz.Height + 4);
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(160, 0, 0, 0)), bg);
                e.Graphics.DrawString(name, font, Brushes.Yellow, tx, ty);
            }

            if (selectedEl != null)
            {
                using var red = new Pen(Color.Red, 2);
                e.Graphics.DrawRectangle(red,
                    selectedEl.X, selectedEl.Y,
                    selectedEl.Width, selectedEl.Height);
            }

            e.Graphics.ResetTransform();
        }

        private void PreviewPanel_MouseDown(object sender, MouseEventArgs e)
        {
            var pt = new Point(
                e.X - previewPanel.AutoScrollPosition.X - originOffset.X,
                e.Y - previewPanel.AutoScrollPosition.Y - originOffset.Y);

            var hit = FindAt(rootElement, pt);
            if (hit != null && (IgnoreShow || hit.Show))
            {
                selectedEl = hit;
                previewPanel.Invalidate();
                ElementClicked?.Invoke(this, hit);
            }

            if (hit == null || (!IgnoreShow && !hit.Show))
            {
                dragEl = null;
                return;
            }

            dragEl = hit;
            dragOffset = new Point(pt.X - dragEl.X, pt.Y - dragEl.Y);
            isDragging = true;
        }

        private void PreviewPanel_MouseMove(object sender, MouseEventArgs e)
        {
            var pt = new Point(
                e.X - previewPanel.AutoScrollPosition.X - originOffset.X,
                e.Y - previewPanel.AutoScrollPosition.Y - originOffset.Y);

            if (isDragging && dragEl != null)
            {
                dragEl.X = pt.X - dragOffset.X;
                dragEl.Y = pt.Y - dragOffset.Y;
                previewPanel.Invalidate();
                ElementChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                var prev = hoverEl;
                hoverEl = FindAt(rootElement, pt);
                if (prev != hoverEl)
                    previewPanel.Invalidate();
            }
        }

        private void PreviewPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                RebuildFullCache();
                ElementChanged?.Invoke(this, EventArgs.Empty);
                previewPanel.Invalidate();
            }
        }

        private void PreviewPanel_MouseLeave(object sender, EventArgs e)
        {
            hoverEl = null;
            previewPanel.Invalidate();
        }

        private void DrawCacheTree(Graphics g, XuiElement el)
        {
            if (_hidden.Contains(el)) return;
            if (!IgnoreShow && !el.Show) return;
            if (el.ElementImage != null)
                g.DrawImage(el.ElementImage, el.X, el.Y, el.Width, el.Height);

            if (!string.IsNullOrEmpty(el.Text))
            {
                using var font = new Font("Segoe UI", 9f);
                var textSize = g.MeasureString(el.Text, font);
                float tx = el.X + (el.Width - textSize.Width) / 2;
                float ty = el.Y + (el.Height - textSize.Height) / 2;

                var bgRect = new RectangleF(tx - 2, ty - 2, textSize.Width + 4, textSize.Height + 4);
                g.FillRectangle(new SolidBrush(Color.FromArgb(160, 0, 0, 0)), bgRect);
                g.DrawString(el.Text, font, Brushes.Black, tx + 1, ty + 1);
                g.DrawString(el.Text, font, Brushes.Orange, tx, ty);
            }

            for (int i = el.Children.Count - 1; i >= 0; i--)
                DrawCacheTree(g, el.Children[i]);
        }

        private void DrawCacheTreeExcept(Graphics g, XuiElement el, XuiElement skip)
        {
            if (_hidden.Contains(el)) return;
            if (!IgnoreShow && !el.Show) return;
            if (el == skip || IsDescendantOf(el, skip)) return;

            if (el.ElementImage != null)
                g.DrawImage(el.ElementImage, el.X, el.Y, el.Width, el.Height);

            if (!string.IsNullOrEmpty(el.Text))
            {
                using var font = new Font("Segoe UI", 9f);
                var textSize = g.MeasureString(el.Text, font);
                float tx = el.X + (el.Width - textSize.Width) / 2;
                float ty = el.Y + (el.Height - textSize.Height) / 2;

                var bgRect = new RectangleF(tx - 2, ty - 2, textSize.Width + 4, textSize.Height + 4);
                g.FillRectangle(new SolidBrush(Color.FromArgb(160, 0, 0, 0)), bgRect);
                g.DrawString(el.Text, font, Brushes.Black, tx + 1, ty + 1);
                g.DrawString(el.Text, font, Brushes.Orange, tx, ty);
            }

            for (int i = el.Children.Count - 1; i >= 0; i--)
                DrawCacheTreeExcept(g, el.Children[i], skip);
        }

        private bool IsDescendantOf(XuiElement el, XuiElement ancestor)
        {
            while (el.Parent != null)
            {
                if (el.Parent == ancestor) return true;
                el = el.Parent;
            }
            return false;
        }

        private XuiElement FindAt(XuiElement root, Point p)
        {
            if (!IgnoreShow && !root.Show) return null;
            foreach (var child in root.Children)
            {
                var hit = FindAt(child, p);
                if (hit != null) return hit;
            }
            return (p.X >= root.X && p.X <= root.X + root.Width &&
                    p.Y >= root.Y && p.Y <= root.Y + root.Height)
                ? root : null;
        }

        private IEnumerable<XuiElement> GetAllElements(XuiElement root)
        {
            yield return root;
            foreach (var c in root.Children)
                foreach (var cc in GetAllElements(c))
                    yield return cc;
        }

        private void PreviewForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (selectedEl == null) return;

            bool moved = true;
            switch (e.KeyCode)
            {
                case Keys.Left: selectedEl.X -= 1; break;
                case Keys.Right: selectedEl.X += 1; break;
                case Keys.Up: selectedEl.Y -= 1; break;
                case Keys.Down: selectedEl.Y += 1; break;
                default:
                    moved = false;
                    break;
            }

            if (moved)
            {
                RebuildFullCache();
                previewPanel.Invalidate();
                ElementChanged?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
        }
    }
}
