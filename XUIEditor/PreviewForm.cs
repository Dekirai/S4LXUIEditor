using System.ComponentModel;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;

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
        private bool isDragging;

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
                using var font = new Font("Verdana", 9f);
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

        private void DrawTextStyled(Graphics g, XuiElement el)
        {
            var style = TextStyler.Parse(el.Text);

            var rect = new RectangleF(el.X, el.Y, el.Width, el.Height);

            var sf = new StringFormat
            {
                Alignment = style.Alignment,
                LineAlignment = StringAlignment.Center,
            };
            if (style.WordWrap)
                sf.FormatFlags &= ~StringFormatFlags.NoWrap;

            using (var sb = new SolidBrush(style.ShadowColor))
            {
                var shadowRect = rect;
                shadowRect.Offset(1, 1);
                g.DrawString(style.Text, style.Font, sb, shadowRect, sf);
            }

            using (var fb = new SolidBrush(style.ForeColor))
            {
                g.DrawString(style.Text, style.Font, fb, rect, sf);
            }
        }



        private void DrawCacheTree(Graphics g, XuiElement el)
        {
            if (_hidden.Contains(el)) return;
            if (!IgnoreShow && !el.Show) return;

            if (el.ElementImage != null)
                g.DrawImage(el.ElementImage, el.X, el.Y, el.Width, el.Height);

            if (!string.IsNullOrEmpty(el.Text))
            {
                if (el.Text.StartsWith("{") && el.Text.Contains("}"))
                {
                    DrawTextStyled(g, el);
                }
                else
                {
                    using var font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
                    var textSize = g.MeasureString(el.Text, font);
                    float tx = el.X + (el.Width - textSize.Width) / 2;
                    float ty = el.Y + (el.Height - textSize.Height) / 2;

                    using var shadowBrush = new SolidBrush(Color.Black);
                    g.DrawString(el.Text, font, shadowBrush, tx + 1, ty + 1);

                    using var mainBrush = new SolidBrush(Color.Orange);
                    g.DrawString(el.Text, font, mainBrush, tx, ty);
                }
            }

            // 4) recurse
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
                if (el.Text.StartsWith("{") && el.Text.Contains("}"))
                    DrawTextStyled(g, el);
                else
                {
                    using var font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
                    var textSize = g.MeasureString(el.Text, font);
                    float tx = el.X + (el.Width - textSize.Width) / 2;
                    float ty = el.Y + (el.Height - textSize.Height) / 2;

                    using var shadowBrush = new SolidBrush(Color.Black);
                    g.DrawString(el.Text, font, shadowBrush, tx + 1, ty + 1);
                    using var mainBrush = new SolidBrush(Color.Orange);
                    g.DrawString(el.Text, font, mainBrush, tx, ty);
                }
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

        public class TextStyle
        {
            public string Text { get; set; }
            public Color ForeColor { get; set; } = Color.White;
            public Color BackColor { get; set; } = Color.Transparent;
            public Color ShadowColor { get; set; } = Color.Black;
            public StringAlignment Alignment { get; set; } = StringAlignment.Near;
            public Font Font { get; set; } = new Font("Segoe UI", 9f);
        }

        public class TextStyler
        {
            private static readonly PrivateFontCollection _customFonts = new PrivateFontCollection();

            private static readonly Regex _fontRx = new Regex(@"\{F-(?<name>[A-Za-z0-9]+)_(?<size>\d+)\}", RegexOptions.Compiled);
            private static readonly Regex _alignRx = new Regex(@"\{A-(?<mode>L|C|R)\}", RegexOptions.Compiled);
            private static readonly Regex _shadowRx = new Regex(@"\{CS-(?<r>\d+),(?<g>\d+),(?<b>\d+),(?<a>\d+)\}", RegexOptions.Compiled);
            private static readonly Regex _foreRx = new Regex(@"\{CB-(?<r>\d+),(?<g>\d+),(?<b>\d+),(?<a>\d+)\}", RegexOptions.Compiled);
            private static readonly Regex _wrapRx = new Regex(@"\{W\}", RegexOptions.Compiled);

            public Font Font { get; private set; }
            public StringAlignment Alignment { get; private set; } = StringAlignment.Center;
            public Color ShadowColor { get; private set; } = Color.Black;
            public Color ForeColor { get; private set; } = Color.White;
            public bool WordWrap { get; private set; }
            public string Text { get; private set; }

            public static TextStyler Parse(string raw)
            {
                var style = new TextStyler();

                style.Font = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);

                var mF = _fontRx.Match(raw);
                if (mF.Success)
                {
                    var name = mF.Groups["name"].Value;
                    var size = float.Parse(mF.Groups["size"].Value, CultureInfo.InvariantCulture);

                    // attempt to load custom TTF from exe folder
                    var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    var file = Path.Combine(exeDir, name + ".ttf");
                    if (File.Exists(file))
                    {
                        if (!_customFonts.Families.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                            _customFonts.AddFontFile(file);

                        var fam = _customFonts.Families
                                   .FirstOrDefault(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                                   ?? _customFonts.Families.First();

                        style.Font = new Font(fam, size, FontStyle.Regular, GraphicsUnit.Point);
                    }
                    else
                    {
                        // fallback to system font
                        try
                        {
                            style.Font = new Font(name, size, FontStyle.Regular, GraphicsUnit.Point);
                        }
                        catch { /* ignore, keep default */ }
                    }

                    raw = raw.Remove(mF.Index, mF.Length);
                }

                var mA = _alignRx.Match(raw);
                if (mA.Success)
                {
                    switch (mA.Groups["mode"].Value)
                    {
                        case "L": style.Alignment = StringAlignment.Center; break;
                        case "C": style.Alignment = StringAlignment.Center; break;
                        case "R": style.Alignment = StringAlignment.Center; break;
                    }
                    raw = raw.Remove(mA.Index, mA.Length);
                }

                var mS = _shadowRx.Match(raw);
                if (mS.Success && TryParseColor(mS, out var sc))
                {
                    style.ShadowColor = sc;
                    raw = raw.Remove(mS.Index, mS.Length);
                }

                var mC = _foreRx.Match(raw);
                if (mC.Success && TryParseColor(mC, out var fc))
                {
                    style.ForeColor = fc;
                    raw = raw.Remove(mC.Index, mC.Length);
                }

                if (_wrapRx.IsMatch(raw))
                {
                    style.WordWrap = true;
                    raw = _wrapRx.Replace(raw, "");
                }

                style.Text = raw;

                return style;
            }

            private static bool TryParseColor(Match m, out Color c)
            {
                c = Color.Empty;
                if (m.Groups["r"].Success &&
                    m.Groups["g"].Success &&
                    m.Groups["b"].Success &&
                    m.Groups["a"].Success &&
                    byte.TryParse(m.Groups["r"].Value, out var r) &&
                    byte.TryParse(m.Groups["g"].Value, out var g) &&
                    byte.TryParse(m.Groups["b"].Value, out var b) &&
                    byte.TryParse(m.Groups["a"].Value, out var a))
                {
                    c = Color.FromArgb(a, r, g, b);
                    return true;
                }

                return false;
            }
        }

    }
}
