namespace XUIEditor
{
    public static class RectangleFExtensions
    {
        /// <summary>
        /// Returns a new RectangleF shifted by (dx,dy) without mutating the original.
        /// </summary>
        public static RectangleF OffsetCopy(this RectangleF r, float dx, float dy)
            => new RectangleF(r.X + dx, r.Y + dy, r.Width, r.Height);
    }
}
