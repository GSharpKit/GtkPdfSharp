using System;
using System.Collections.Generic;
using System.Text;
using Cairo;
using Gtk;
using Pango;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using Color = Cairo.Color;
using Context = Cairo.Context;
using Scale = Gtk.Scale;

namespace XMedicus.PDFViewer
{
    public class PDFViewer : DrawingArea
    {
        private int margin = 10;

        PdfDocument inputDocument;

        Color blackColor, shadowColor;

        Pango.Layout layout;

        FontDescription fontDescription;
        const double fontSize = 10.0;



        public PDFViewer()
        {
            blackColor = new Color (0, 0, 0);
            shadowColor = new Color (1, 1, 1);

            fontDescription   = FontDescription.FromString ("Sans");
            fontDescription.Size = (int)(fontSize * Pango.Scale.PangoScale);

            Drawn += OnDrawnEvent;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }

            base.Dispose(disposing);
        }

        private bool Disposed { get; set; }

        void OnDrawnEvent (object obj, DrawnArgs args)
        {
            if (Disposed) return;
            try
            {
                if (args.Cr == null)
                    return;

                Context cr = args.Cr;

                if (obj is Widget w)
                {
                    Paint (cr, w.AllocatedWidth, w.AllocatedHeight);
                }

                args.RetVal = true;
            }
            catch
            {
            }
            finally
            {
                if (args.Cr != null)
                {
                    Context cr = args.Cr;

                    Surface cs = cr.GetTarget ();
                    cs?.Dispose ();

                    cr.Dispose ();
                }
            }
        }

        void Paint(Context cr, double w, double h)
        {
            layout = Pango.CairoHelper.CreateLayout (cr);

            cr.LineWidth = 1;

            for (int idx = 0; idx < inputDocument.PageCount; idx++)
            {
                var page = inputDocument.Pages [idx];

                DrawMediaBox(cr, page);

                CObject content = ContentReader.ReadContent(page);
                DrawText(cr, content);

                /*foreach (string s in extractedText)
                {
                    Console.WriteLine(s);
                }

                var parser = new CParser(page);
                var sq = parser.ReadContent();
                foreach (CObject cObject in sq)
                {

                }*/
            }
        }

        void DrawMediaBox(Context cr, PdfPage page)
        {
            cr.SetSourceColor (blackColor);
            cr.Save ();
            DrawHelper.DrawBox(cr, margin + page.MediaBox.X1, margin + page.MediaBox.Y1, page.MediaBox.Width, page.MediaBox.Height);
            cr.Stroke ();

            cr.SetSourceColor (shadowColor);
            cr.Save ();
            DrawHelper.DrawBox(cr, margin + page.MediaBox.X1 + 2, margin + page.MediaBox.Y1 + 2, page.MediaBox.Width, page.MediaBox.Height);
            cr.Stroke ();
        }

        public void Render(string filepath)
        {
            inputDocument = PdfReader.Open (filepath, PdfDocumentOpenMode.Import);
        }

        void DrawText(Context cr, CObject cObject)
        {
            if (cObject is COperator cOperator)
            {
                Console.WriteLine(cOperator.OpCode.OpCodeName);
                switch (cOperator.OpCode.OpCodeName)
                {
                    case OpCodeName.Tj:
                    case OpCodeName.TJ:
                        foreach (var cOperand in cOperator.Operands)
                        {
                            DrawText(cr, cOperand);
                        }
                        break;
                    //case OpCodeName.:
                        //cOperator.
                      //  break;
                    default:
                        foreach (var cOperand in cOperator.Operands)
                        {
                            if (cOperand is CString cString)
                            {
                                Console.WriteLine(cString);
                            }
                        }
                        break;
                }
            }
            else if (cObject is CSequence cSequence)
            {
                foreach (var element in cSequence)
                {
                    DrawText(cr, element);
                }
            }
            else if (cObject is CString cString)
            {
                layout.FontDescription = fontDescription;
                layout.Wrap = Pango.WrapMode.WordChar;

                cr.MoveTo (50, 100 - 8);
                cr.SetSourceColor (blackColor);

                layout.Width = ((int)(240 * Pango.Scale.PangoScale));
                layout.SetText (cString.Value);

                Pango.CairoHelper.ShowLayoutLine (cr, layout.Lines [0]);
            }
        }
    }
}
