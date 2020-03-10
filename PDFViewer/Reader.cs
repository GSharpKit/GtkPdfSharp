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
        
        Tuple<double, double> subpath = new Tuple<double, double>(0d, 0d);
        Tuple<double, double> current = new Tuple<double, double>(0d, 0d);

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
            subpath = new Tuple<double, double>(0d, 0d);
            current = new Tuple<double, double>(0d, 0d);
            
            layout = Pango.CairoHelper.CreateLayout (cr);

            cr.LineWidth = 1;
            cr.SetSourceColor (blackColor);

            for (int idx = 0; idx < inputDocument.PageCount; idx++)
            {
                var page = inputDocument.Pages [idx];

                DrawMediaBox(cr, page);

                CObject content = ContentReader.ReadContent(page);
                DrawContent(cr, content);

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
            //cr.Save ();
            DrawHelper.DrawBox(cr, margin + page.MediaBox.X1, margin + page.MediaBox.Y1, page.MediaBox.Width, page.MediaBox.Height);
            cr.Stroke ();

            //cr.Save ();
            DrawHelper.DrawBox(cr, margin + page.MediaBox.X1 + 2, margin + page.MediaBox.Y1 + 2, page.MediaBox.Width, page.MediaBox.Height);
            cr.Stroke ();
        }

        public void Render(string filepath)
        {
            inputDocument = PdfReader.Open (filepath, PdfDocumentOpenMode.Import);
        }

        void DrawContent(Context cr, CObject cObject)
        {
            if (cObject is COperator cOperator)
            {
                switch (cOperator.OpCode.OpCodeName)
                {
                    case OpCodeName.Tj:
                    case OpCodeName.TJ:
                        foreach (var cOperand in cOperator.Operands)
                        {
                            DrawContent(cr, cOperand);
                        }
                        break;
                    case OpCodeName.m:
                        if (cOperator.Operands.Count == 2)
                        {
                            double x = 0d, y = 0d;
                            if (cOperator.Operands[0] is CReal rx)
                                x = rx.Value;
                            if (cOperator.Operands[1] is CReal ry)
                                y = ry.Value;
                            
                            //cr.Save();
                            cr.MoveTo(current.Item1, current.Item2);
                            
                            subpath = current = new Tuple<double, double>(x, y);
                        }
                        break;
                    case OpCodeName.h:
                        if (subpath.Item1 != 0d && subpath.Item2 != 0d)
                        {
                            cr.MoveTo(current.Item1, current.Item2);
                            cr.LineTo(subpath.Item1, subpath.Item2);
                        }
                        subpath = current = new Tuple<double, double>(0, 0);
                        break;
                    case OpCodeName.l:
                        if (cOperator.Operands.Count == 2)
                        {
                            double x = 0d, y = 0d;
                            if (cOperator.Operands[0] is CReal rx)
                                x = rx.Value;
                            if (cOperator.Operands[1] is CReal ry)
                                y = ry.Value;
                            
                            //cr.MoveTo(subpath.Item1, subpath.Item2);
                            cr.LineTo(x, y);
                            
                            subpath = new Tuple<double, double>(x, y);
                        }
                        break;
                    case OpCodeName.re:
                        if (cOperator.Operands.Count == 4)
                        {
                            double x = 0d, y = 0d, w = 0d, h = 0d;
                            if (cOperator.Operands[0] is CReal rx)
                                x = rx.Value;
                            if (cOperator.Operands[1] is CReal ry)
                                y = ry.Value;
                            if (cOperator.Operands[2] is CReal rw)
                                w = rw.Value;
                            if (cOperator.Operands[3] is CReal rh)
                                h = rh.Value;
                            
                            DrawHelper.DrawBox(cr, x, y, w, h);
                            
                            subpath = new Tuple<double, double>(x, y);
                        }
                        break;
                    case OpCodeName.S:
                        cr.Stroke();
                        
                        subpath = current = new Tuple<double, double>(0, 0);
                        break;
                    case OpCodeName.s:
                        //cr.MoveTo(current.Item1, current.Item2);
                        cr.LineTo(subpath.Item1, subpath.Item2);
                        cr.Stroke();

                        subpath = current = new Tuple<double, double>(0, 0);
                        break;
                    case OpCodeName.f:
                        //cr.Fill();
                        break;
                    /*case OpCodeName.scn:
                        if (cOperator.Operands.Count == 3)
                        {
                            double x = 0d, y = 0d, w = 0d, h = 0d;
                            if (cOperator.Operands[0] is CReal rx)
                                x = rx.Value;
                        }
                        break;*/
                    default:
                        Console.WriteLine(cOperator.OpCode.OpCodeName);
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
                    DrawContent(cr, element);
                }
            }
            else if (cObject is CString cString)
            {
                layout.FontDescription = fontDescription;
                layout.Wrap = Pango.WrapMode.WordChar;

                cr.MoveTo (current.Item1, current.Item2);
                cr.SetSourceColor (blackColor);

                layout.Width = ((int)(240 * Pango.Scale.PangoScale));
                layout.SetText (cString.Value);

                Pango.CairoHelper.ShowLayout(cr, layout);
            }
        }
    }
}
