namespace XMedicus.PDFViewer
{
    public static class DrawHelper
    {
        public static void DrawBox (Cairo.Context cr, double x, double y, double w, double h)
        {
            cr.MoveTo (x, y);
            cr.LineTo (x + w, y);
            cr.LineTo (x + w, y + h);
            cr.LineTo (x, y + h);
            cr.LineTo (x, y);
        }

        public static void RoundedRectangle (Cairo.Context cr, double x, double y, double w, double h, double r)
        {
            cr.MoveTo (x + r, y);
            cr.LineTo (x + w - r, y);
            cr.CurveTo (x + w, y, x + w, y, x + w, y + r);
            cr.LineTo (x + w, y + h - r);
            cr.CurveTo (x + w, y + h, x + w, y + h, x + w - r, y + h);
            cr.LineTo (x + r, y + h);
            cr.CurveTo (x, y + h, x, y + h, x, y + h - r);
            cr.LineTo (x, y + r);
            cr.CurveTo (x, y, x, y, x + r, y);
        }
    }
}
