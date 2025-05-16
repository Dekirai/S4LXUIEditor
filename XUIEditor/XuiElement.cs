using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Xml.Linq;

namespace XUIEditor
{
    public class XuiElement
    {
        // hide these
        [Browsable(false)] public string Type { get; set; }
        [Browsable(false)] public List<GuiSkin> Skins { get; } = new();
        [Browsable(false)] public List<XuiElement> Children { get; set; } = new();
        [Browsable(false)] public XuiElement Parent { get; set; }
        [Browsable(false)] public Image ElementImage { get; set; }
        [Browsable(false)] public XElement XmlNode { get; set; }

        // Layout
        [Category("Layout")] public string Name { get; set; }
        [Category("Layout")] public string TexturePath { get; set; }
        [Browsable(false)] public float U0, V0, U1 = 1, V1 = 1;
        [Category("Layout")] public int X { get; set; }
        [Category("Layout")] public int Y { get; set; }
        [Category("Layout")] public int Width { get; set; }
        [Category("Layout")] public int Height { get; set; }

        // Behavior
        [Category("Behavior")] public bool Show { get; set; } = true;
        [Category("Behavior"), DisplayName("Enable")]
        public bool Enable { get; set; } = true;
        [Category("Behavior"), DisplayName("Lock Layer")]
        public bool LockLayer { get; set; }
        [Category("Behavior"), DisplayName("Selectable")]
        public bool Selectable { get; set; } = true;

        [Category("Appearance"), DisplayName("Opacity")]
        public float Opacity { get; set; } = 1.0f;
        [Category("Appearance"), DisplayName("Tooltip Text")]
        public string TooltipText { get; set; } = string.Empty;
        [Category("Appearance"), DisplayName("String")]
        public string Text { get; set; } = string.Empty;

        public override string ToString() => Name;
    }

    public class GuiSkin
    {
        [Browsable(false)] public int Index { get; set; }
        [Browsable(false)] public string Texture { get; set; }
        [Browsable(false)] public float Left, Top, Right, Bottom;
    }
}
