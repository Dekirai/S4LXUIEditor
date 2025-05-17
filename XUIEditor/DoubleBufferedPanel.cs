using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XUIEditor
{
    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            // Enable double‐buffering to eliminate flicker
            this.DoubleBuffered = true;
            this.SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,
                true);
            this.UpdateStyles();
        }
    }
}
