using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace XUIEditor
{
    public static class XuiParser
    {
        public static XuiElement Parse(string filePath)
            => ParseXuiFile(filePath);

        public static List<XuiElement> LoadFolder(string folderPath)
        {
            var roots = new List<XuiElement>();
            foreach (var file in Directory.GetFiles(folderPath, "*.xui"))
            {
                try { roots.Add(ParseXuiFile(file)); }
                catch { /* ignore */ }
            }
            return roots;
        }

        public static void SaveMinimal(XuiElement root, string filePath)
        {
            var doc = XDocument.Load(filePath);
            var guiSection = doc.Root.Element("gui")
                          ?? doc.Root.Descendants("gui").FirstOrDefault();
            if (guiSection == null)
                throw new Exception("XUI missing <gui> section");

            var windowNode = guiSection.Elements().FirstOrDefault();
            if (windowNode == null)
                throw new Exception("XUI <gui> has no child element");

            AttachXmlNodes(windowNode, root);

            UpdateNode(root);

            doc.Save(filePath);
        }

        private static void AttachXmlNodes(XElement xmlEl, XuiElement el)
        {
            el.XmlNode = xmlEl;
            var xmlKids = xmlEl.Element("child")?.Elements().ToList() ?? new List<XElement>();
            for (int i = 0; i < el.Children.Count && i < xmlKids.Count; i++)
                AttachXmlNodes(xmlKids[i], el.Children[i]);
        }

        private static void UpdateNode(XuiElement el)
        {
            var xml = el.XmlNode;
            if (xml == null) return;

            var g = xml.Element("global");
            if (g != null)
            {
                UpdateInt(g, "left", el.X);
                UpdateInt(g, "top", el.Y);
                UpdateInt(g, "right", el.X + el.Width);
                UpdateInt(g, "bottom", el.Y + el.Height);
            }

            var s = xml.Element("show");
            if (s != null)
                UpdateString(s, "value", el.Show ? "true" : "false");

            var op = xml.Element("opacity");
            if (op != null)
                UpdateFloat(op, "value", el.Opacity);

            var en = xml.Element("enable");
            if (en != null)
                UpdateString(en, "value", el.Enable ? "true" : "false");

            var lo = xml.Element("lock_layer");
            if (lo != null)
                UpdateString(lo, "value", el.LockLayer ? "true" : "false");

            var sel = xml.Element("selectable");
            if (sel != null)
                UpdateString(sel, "value", el.Selectable ? "true" : "false");

            var tteg = xml.Element("tooltip")?.Element("eng");
            if (tteg != null)
                UpdateString(tteg, "value", el.TooltipText);

            foreach (var skinNode in xml.Elements()
                                        .Where(e => e.Name.LocalName.StartsWith("gui_skin_")))
            {
                int idx = int.Parse(skinNode.Name.LocalName["gui_skin_".Length..]);
                UpdateFloat(skinNode, "left", el.Skins[idx].Left);
                UpdateFloat(skinNode, "top", el.Skins[idx].Top);
                UpdateFloat(skinNode, "right", el.Skins[idx].Right);
                UpdateFloat(skinNode, "bottom", el.Skins[idx].Bottom);
            }

            var eng = xml.Element("string")?.Element("eng");
            if (eng != null)
                UpdateString(eng, "value", el.Text);

            foreach (var c in el.Children)
                UpdateNode(c);
        }

        private static void UpdateInt(XElement xml, string attrName, int newVal)
        {
            var a = xml.Attribute(attrName);
            if (a == null) return;
            if (int.TryParse(a.Value, out var oldVal) && oldVal == newVal) return;
            a.Value = newVal.ToString();
        }

        private static void UpdateFloat(XElement xml, string attrName, float newVal)
        {
            var a = xml.Attribute(attrName);
            if (a == null) return;

            if (float.TryParse(
                    a.Value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var oldVal)
                && Math.Abs(oldVal - newVal) < 1e-6f)
            {
                return;
            }

            var orig = a.Value;
            int decimals = 6;
            var dot = orig.IndexOf('.');
            if (dot >= 0) decimals = orig.Length - dot - 1;

            a.Value = newVal.ToString($"F{decimals}", CultureInfo.InvariantCulture);
        }

        private static void UpdateString(XElement xml, string attrName, string newVal)
        {
            var a = xml.Attribute(attrName);
            if (a == null) return;
            if (a.Value == newVal) return;
            a.Value = newVal;
        }


        private static XuiElement ParseXuiFile(string filePath)
        {
            var raw = File.ReadAllText(filePath);

            raw = Regex.Replace(raw,
                @"\bvalue=""(?<txt>[^""]*)""",
                m =>
                {
                    var txt = m.Groups["txt"].Value
                               .Replace("&", "&amp;")
                               .Replace("<", "&lt;")
                               .Replace(">", "&gt;");
                    return $@"value=""{txt}""";
                });

            var doc = XDocument.Parse(raw);

            var guiSection = doc.Root.Element("gui")
                          ?? doc.Root.Descendants("gui").FirstOrDefault();
            if (guiSection == null)
                throw new Exception("XUI missing <gui> section");

            var windowNode = guiSection.Elements().FirstOrDefault();
            if (windowNode == null)
                throw new Exception("XUI <gui> has no child element");

            var root = ParseElement(windowNode);
            root.Parent = null;
            return root;
        }


        private static XuiElement ParseElement(XElement node)
        {
            var el = new XuiElement
            {
                XmlNode = node,
                Type = node.Name.LocalName,
                Name = (string)node.Attribute("name") ?? "",
                Show = true,
                Text = string.Empty,
                TexturePath = null,
                Enable = true,
                Opacity = 1f,
                TooltipText = "",
                LockLayer = false,
                Selectable = true,
            };

            var g = node.Element("global");
            if (g != null)
            {
                int l = (int?)g.Attribute("left") ?? 0;
                int t = (int?)g.Attribute("top") ?? 0;
                int r = (int?)g.Attribute("right") ?? l;
                int b = (int?)g.Attribute("bottom") ?? t;
                el.X = l; el.Y = t;
                el.Width = r - l;
                el.Height = b - t;
            }

            var s = node.Element("show");
            if (s != null)
            {
                var v = ((string)s.Attribute("value") ?? "true")
                          .Equals("true", StringComparison.OrdinalIgnoreCase);
                el.Show = v;
            }

            var op = node.Element("opacity");
            if (op != null)
                el.Opacity = (float?)op.Attribute("value") ?? el.Opacity;

            var en = node.Element("enable");
            if (en != null)
                el.Enable = (((string)en.Attribute("value") ?? "true")
                             .Equals("true", StringComparison.OrdinalIgnoreCase));

            var lo = node.Element("lock_layer");
            if (lo != null)
                el.LockLayer = (((string)lo.Attribute("value") ?? "false")
                                .Equals("true", StringComparison.OrdinalIgnoreCase));

            var sel = node.Element("selectable");
            if (sel != null)
                el.Selectable = (((string)sel.Attribute("value") ?? "true")
                                 .Equals("true", StringComparison.OrdinalIgnoreCase));

            var tt = node.Element("tooltip")?.Element("eng");
            if (tt != null)
                el.TooltipText = (string)tt.Attribute("value") ?? "";

            el.U0 = el.V0 = 0;
            el.U1 = el.V1 = 1;
            foreach (var skinNode in node.Elements()
                                         .Where(e => e.Name.LocalName.StartsWith("gui_skin_")))
            {
                var idxTxt = skinNode.Name.LocalName["gui_skin_".Length..];
                if (!int.TryParse(idxTxt, out var idx)) continue;

                var skin = new GuiSkin
                {
                    Index = idx,
                    Texture = (string)skinNode.Attribute("texture") ?? "",
                    Left = (float?)skinNode.Attribute("left") ?? 0f,
                    Top = (float?)skinNode.Attribute("top") ?? 0f,
                    Right = (float?)skinNode.Attribute("right") ?? 1f,
                    Bottom = (float?)skinNode.Attribute("bottom") ?? 1f
                };
                el.Skins.Add(skin);

                if (idx == 0)
                {
                    el.TexturePath = skin.Texture;
                    el.U0 = skin.Left;
                    el.V0 = skin.Top;
                    el.U1 = skin.Right;
                    el.V1 = skin.Bottom;
                }
            }

            var eng = node.Element("string")?.Element("eng");
            if (eng != null)
                el.Text = (string)eng.Attribute("value") ?? "";

            var childCt = node.Element("child");
            if (childCt != null)
                foreach (var c in childCt.Elements())
                {
                    var ce = ParseElement(c);
                    ce.Parent = el;
                    el.Children.Add(ce);
                }

            return el;
        }
    }
}
