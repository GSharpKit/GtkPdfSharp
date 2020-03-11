using System;
using Cairo;
using Gtk;
using Pango;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using Color = Cairo.Color;
using Context = Cairo.Context;

namespace XMedicus.PDFViewer
{
    public class PDFViewer : DrawingArea
    {
        private int margin = 10;

        PdfDocument inputDocument;

        double pageHeight, pageWidth;

        Color blackColor, whiteColor, strokeColor, fillColor;

        Pango.Layout layout;

        FontDescription fontDescription;
        double fontSize = 10.0;

        Tuple<double, double> cursor = new Tuple<double, double>(0d, 0d);
        double lineHeight;

        public PDFViewer()
        {
            blackColor = strokeColor = fillColor = new Color (0, 0, 0);
            whiteColor = new Color (1, 1, 1);

            fontDescription = FontDescription.FromString ("Sans");
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

        public void Render(string filepath)
        {
            inputDocument = PdfReader.Open (filepath, PdfDocumentOpenMode.Import);
        }

#region Drawing
        void Paint(Context cr, double w, double h)
        {
            layout = Pango.CairoHelper.CreateLayout (cr);

            cr.LineWidth = 0.5d;
            cr.SetSourceColor (blackColor);

            for (int idx = 0; idx < inputDocument.PageCount; idx++)
            {
                var page = inputDocument.Pages [idx];

                var res = page.Resources;

                lineHeight = 10d;

                pageHeight = page.MediaBox.Height;
                pageWidth = page.MediaBox.Width;

                cursor = new Tuple<double, double>(0d, pageHeight + margin);

                DrawMediaBox(cr, page);

                CObject content = ContentReader.ReadContent(page);
                DrawContent(cr, content);
            }
        }

        void DrawMediaBox(Context cr, PdfPage page)
        {
            cr.MoveTo(margin + page.MediaBox.X1, margin + page.MediaBox.Y2);
            DrawHelper.DrawBox(cr, margin + page.MediaBox.X1, margin + page.MediaBox.Y2, pageWidth, pageHeight);
            cr.SetSourceColor(whiteColor);
            cr.FillPreserve();
            cr.SetSourceColor(blackColor);
            cr.Stroke ();

            /*cr.SetSourceColor(shadowColor);
            cr.MoveTo(margin + page.MediaBox.X1 + 2, margin + page.MediaBox.Y1 + 2);
            DrawHelper.DrawBox(cr, margin + page.MediaBox.X1 + 2, margin + page.MediaBox.Y1 + 2, pageWidth, pageHeight);
            cr.Stroke ();
            cr.SetSourceColor(blackColor);*/
        }

        void DrawContent(Context cr, CObject cObject)
        {
            if (cObject is COperator cOperator)
            {
                Color? ncolor = null;
                switch (cOperator.OpCode.OpCodeName)
                {
                    // Set the text font, Tf , to font and the text font size, T fs , to size. font shall be
                    // the name of a font resource in the Font subdictionary of the current
                    // resource dictionary; size shall be a number representing a scale factor.
                    // There is no initial value for either font or size; they shall be specified
                    // explicitly by using Tf before any text is shown.
                    case OpCodeName.Tf:
                        if (cOperator.Operands.Count == 2)
                        {
                            if (cOperator.Operands[0] is CName font)
                            {
                                //fontDescription = FontDescription.FromString ("Sans");
                            }
                            if (cOperator.Operands[1] is CInteger i)
                            {
                                fontDescription.Size = (int)((fontSize * i.Value) * Pango.Scale.PangoScale);
                            }
                        }
                        break;

                    // Move to the start of the next line, offset from the start of the current line by
                    // (tx , ty ). t x and t y shall denote numbers expressed in unscaled text space
                    // units. More precisely, this operator shall perform these assignments:
                    //            [ 1  0  0 ]
                    // Tm = Tlm = [ 0  1  1 ] x Tlm
                    //            [ tx ty 1 ]
                    //
                    // Operands: tx ty
                    case OpCodeName.Td:
                        if (cOperator.Operands.Count == 2)
                        {
                            double x = 0d, y = 0d;
                            if (cOperator.Operands[0] is CReal rx)
                                x = rx.Value;
                            else if (cOperator.Operands[0] is CInteger ix)
                                x = ix.Value;

                            if (cOperator.Operands[1] is CReal ry)
                                y = ry.Value;
                            else if (cOperator.Operands[1] is CInteger iy)
                                y = iy.Value;

                            cursor = new Tuple<double, double>(x, cursor.Item2 - y - (fontDescription.Size / Pango.Scale.PangoScale));
                        }
                        break;

                    // Move to the start of the next line, offset from the start of the current line by
                    // (t x , t y ). As a side effect, this operator shall set the leading parameter in
                    // the text state. This operator shall have the same effect as this code:
                    //    −ty TL
                    //    tx ty Td
                    //
                    // Operands: tx ty
                    case OpCodeName.TD:
                        if (cOperator.Operands.Count == 2)
                        {
                            double x = 0d, y = 0d;
                            if (cOperator.Operands[0] is CReal rx)
                                x = rx.Value;
                            else if (cOperator.Operands[0] is CInteger ix)
                                x = ix.Value;

                            if (cOperator.Operands[1] is CReal ry)
                                y = ry.Value;
                            else if (cOperator.Operands[1] is CInteger iy)
                                y = iy.Value;

                            cursor = new Tuple<double, double>(x, cursor.Item2 - y - (fontDescription.Size / Pango.Scale.PangoScale));
                        }
                        break;

                    // Set the text matrix, Tm , and the text line matrix, Tlm:
                    //               [ a b 0 ]
                    //    Tm = Tlm = [ c d 0 ]
                    //               [ e f 1 ]
                    // The operands shall all be numbers, and the initial value for Tm and Tlm
                    // shall be the identity matrix, [ 1 0 0 1 0 0 ]. Although the operands
                    // specify a matrix, they shall be passed to Tm as six separate numbers, not
                    // as an array.The matrix specified by the operands shall not be concatenated onto the current
                    // text matrix, but shall replace it.
                    //
                    // Operands: a b c d e f
                    case OpCodeName.Tm:
                        if (cOperator.Operands.Count == 6)
                        {
                            double x = 0d, y = 0d;
                            if (cOperator.Operands[4] is CReal rx)
                                x = rx.Value;
                            else if (cOperator.Operands[4] is CInteger ix)
                                x = ix.Value;

                            if (cOperator.Operands[5] is CReal ry)
                                y = ry.Value;
                            else if (cOperator.Operands[5] is CInteger iy)
                                y = iy.Value;

                            cursor = new Tuple<double, double>(cursor.Item1 + x, cursor.Item2 - y);
                        }
                        break;

                    // Move to the start of the next line. This operator has the same effect as the code
                    //  0 -T l Td
                    // where T l denotes the current leading parameter in the text state. The
                    // negative of T l is used here because T l is the text leading expressed as a
                    // positive number. Going to the next line entails decreasing the y coordinate.
                    //
                    // Operands: none
                    case OpCodeName.Tx:
                        cursor = new Tuple<double, double>(0d, cursor.Item2 - lineHeight);
                        break;

                    // Move to the next line and show a text string. This operator shall have the
                    // same effect as the code
                    //    T*
                    //    string Tj
                    //
                    // Operands: string
                    case OpCodeName.QuoteSingle:
                        cursor = new Tuple<double, double>(0d, cursor.Item2 - lineHeight);
                        foreach (var cOperand in cOperator.Operands)
                        {
                            DrawContent(cr, cOperand);
                        }
                        break;

                    // Move to the next line and show a text string, using a w as the word spacing
                    // and a c as the character spacing (setting the corresponding parameters in
                    // the text state). a w and a c shall be numbers expressed in unscaled text
                    // space units. This operator shall have the same effect as this code:
                    //    a w Tw
                    //    a c Tc
                    //    string '
                    //
                    // Opreands: a w a c string
                    case OpCodeName.QuoteDbl:
                        cursor = new Tuple<double, double>(0d, cursor.Item2 - lineHeight);
                        foreach (var cOperand in cOperator.Operands)
                        {
                            DrawContent(cr, cOperand);
                        }
                        break;

                    // Show a text string.
                    //
                    // Operands: string
                    case OpCodeName.Tj:
                    // Show one or more text strings, allowing individual glyph positioning. Each
                    // element of array shall be either a string or a number. If the element is a
                    // string, this operator shall show the string. If it is a number, the operator
                    // shall adjust the text position by that amount; that is, it shall translate the
                    // text matrix, T m . The number shall be expressed in thousandths of a unit
                    // of text space (see 9.4.4, "Text Space Details"). This amount shall be
                    // subtracted from the current horizontal or vertical coordinate, depending
                    // on the writing mode. In the default coordinate system, a positive
                    // adjustment has the effect of moving the next glyph painted either to the
                    // left or down by the given amount. Figure 46 shows an example of the
                    // effect of passing offsets to TJ.
                    //
                    // Operands: array
                    case OpCodeName.TJ:
                        foreach (var cOperand in cOperator.Operands)
                        {
                            DrawContent(cr, cOperand);
                        }
                        break;

                    // Begin a new subpath by moving the current point to coordinates (x, y), omitting any connecting
                    // line segment. If the previous path construction operator in the current pathwas also m,
                    // the new m overrides it; no vestige of the previous m operation remains in the path.
                    case OpCodeName.m:
                        if (cOperator.Operands.Count == 2)
                        {
                            double x = 0d, y = 0d;
                            if (cOperator.Operands[0] is CReal rx)
                                x = rx.Value;
                            else if (cOperator.Operands[0] is CInteger ix)
                                x = ix.Value;

                            if (cOperator.Operands[1] is CReal ry)
                                y = ry.Value;
                            else if (cOperator.Operands[1] is CInteger iy)
                                y = iy.Value;

                            cr.MoveTo (margin + x, margin + pageHeight - y);
                        }
                        break;

                    // Append a straight line segment from the current point to the point (x, y).
                    // The new current point shall be (x, y).
                    case OpCodeName.l:
                        if (cOperator.Operands.Count == 2)
                        {
                            double x = 0d, y = 0d;
                            if (cOperator.Operands[0] is CReal rx)
                                x = rx.Value;
                            else if (cOperator.Operands[0] is CInteger ix)
                                x = ix.Value;

                            if (cOperator.Operands[1] is CReal ry)
                                y = ry.Value;
                            else if (cOperator.Operands[1] is CInteger iy)
                                y = iy.Value;

                            //cr.LineTo(margin + x, margin + pageHeight - y);
                        }
                        break;

                    // Append a cubic Bézier curve to the current path. The curve
                    // shall extend from the current point to the point (x 3 , y 3 ), using
                    // (x 1 , y 1 ) and (x 2 , y 2 ) as the Bézier control points (see 8.5.2.2,
                    // "Cubic Bézier Curves"). The new current point shall be (x 3 , y 3 ).
                    case OpCodeName.c:
                        Console.WriteLine(cOperator.OpCode.OpCodeName);
                        break;

                    // Append a cubic Bézier curve to the current path. The curve shall extend from the current
                    // point to the point (x 3 , y 3 ), using the current point and (x 2 , y 2 ) as the Bézier
                    // control points (see 8.5.2.2, "Cubic Bézier Curves"). The new current point shall be (x 3 , y 3 ).
                    case OpCodeName.v:
                        Console.WriteLine(cOperator.OpCode.OpCodeName);
                        break;

                    // Append a cubic Bézier curve to the current path. The curve shall extend from the current point
                    // to the point (x 3 , y 3 ), using (x 1 , y 1 ) and (x 3 , y 3 ) as the Bézier control points
                    // (see 8.5.2.2, "Cubic Bézier Curves"). The new current point shall be (x 3 , y 3 ).
                    case OpCodeName.y:
                        Console.WriteLine(cOperator.OpCode.OpCodeName);
                        break;

                    // Close the current subpath by appending a straight line segment from the current point to the
                    // starting point of the subpath. If the current subpath is already closed, h shall do nothing.
                    // This operator terminates the current subpath. Appending another segment to the current path
                    // shall begin a new subpath, even if the new segment begins at the endpoint reached by the h
                    // operation.
                    case OpCodeName.h:
                        cr.ClosePath();
                        break;

                    // Append a rectangle to the current path as a complete subpath, with lower-left corner (x, y) and
                    // dimensions width and height in user space. The operation
                    //   x y width height re
                    // is equivalent to
                    //   x y m
                    //   ( x + width ) y l
                    //   ( x + width ) ( y + height ) l
                    //   x ( y + height ) l
                    //   h
                    case OpCodeName.re:
                        if (cOperator.Operands.Count == 4)
                        {
                            double x = 0d, y = 0d, w = 0d, h = 0d;
                            if (cOperator.Operands[0] is CReal rx)
                                x = rx.Value;
                            else if (cOperator.Operands[0] is CInteger ix)
                                x = ix.Value;

                            if (cOperator.Operands[1] is CReal ry)
                                y = ry.Value;
                            else if (cOperator.Operands[1] is CInteger iy)
                                y = iy.Value;

                            if (cOperator.Operands[2] is CReal rw)
                                w = rw.Value;
                            else if (cOperator.Operands[2] is CInteger iw)
                                w = iw.Value;

                            if (cOperator.Operands[3] is CReal rh)
                                h = rh.Value;
                            else if (cOperator.Operands[3] is CInteger ih)
                                h = ih.Value;

                            if (h > 0 && w > 0)
                            {
                                cr.Save();
                                cr.MoveTo(margin + x, margin + pageHeight - y);
                                DrawHelper.DrawBox(cr, margin + x, margin + pageHeight - y, w, h);
                                cr.SetSourceColor(fillColor);
                                cr.FillPreserve();
                                cr.SetSourceColor(strokeColor);
                                cr.Stroke();
                                cr.Restore();
                            }
                        }
                        break;

                    // The q operator shall push a copy of the entire graphics state onto the stack.
                    case OpCodeName.q:
                        cr.Save();
                        break;

                     // The Q operator shall restore the entire graphics state to its former value by popping it
                     // from the stack.
                    case OpCodeName.Q:
                        cr.Restore();
                        break;

                    // Stroke the path.
                    case OpCodeName.S:
                        cr.SetSourceColor(strokeColor);
                        cr.Stroke();
                        break;

                    // Close and stroke the path. This operator shall have the same effect as the sequence h S.
                    case OpCodeName.s:
                        cr.ClosePath();
                        cr.SetSourceColor(strokeColor);
                        cr.Stroke();
                        break;

                    // Fill the path, using the nonzero winding number rule to determine the region
                    // to fill (see 8.5.3.3.2, "Nonzero Winding Number Rule"). Any subpaths that
                    // are open shall be implicitly closed before being filled.
                    case OpCodeName.f:
                    case OpCodeName.F:
                        cr.ClosePath();
                        cr.SetSourceColor(fillColor);
                        cr.Fill();
                        break;

                    // Fill the path, using the even-odd rule to determine the region to fill (see
                    // 8.5.3.3.3, "Even-Odd Rule").
                    case OpCodeName.fx:
                        cr.SetSourceColor(fillColor);
                        cr.Fill();
                        break;

                    // Fill and then stroke the path, using the nonzero winding number rule to
                    // determine the region to fill. This operator shall produce the same result as
                    // constructing two identical path objects, painting the first with f and the
                    // second with S.
                    // NOTE
                    //   The filling and stroking portions of the operation consult
                    //   different values of several graphics state parameters, such as
                    //   the current colour. See also 11.7.4.4, "Special Path-Painting Considerations".
                    case OpCodeName.B:
                        cr.SetSourceColor(fillColor);
                        cr.FillPreserve();
                        cr.SetSourceColor(strokeColor);
                        cr.Stroke();
                        break;

                    // Fill and then stroke the path, using the even-odd rule to determine the region
                    // to fill. This operator shall produce the same result as B, except that the path
                    // is filled as if with f* instead of f. See also 11.7.4.4, "Special Path-Painting Considerations".
                    case OpCodeName.Bx:
                        cr.SetSourceColor(fillColor);
                        cr.FillPreserve();
                        cr.SetSourceColor(strokeColor);
                        cr.Stroke();
                        break;

                    // Close, fill, and then stroke the path, using the nonzero winding number rule
                    // to determine the region to fill. This operator shall have the same effect as the
                    // sequence h B. See also 11.7.4.4, "Special Path-Painting Considerations".
                    case OpCodeName.b:
                        cr.ClosePath();
                        cr.SetSourceColor(fillColor);
                        cr.FillPreserve();
                        cr.SetSourceColor(strokeColor);
                        cr.Stroke();
                        break;

                    // Close, fill, and then stroke the path, using the even-odd rule to determine the
                    // region to fill. This operator shall have the same effect as the sequence h B*.
                    // See also 11.7.4.4, "Special Path-Painting Considerations".
                    case OpCodeName.bx:
                        cr.ClosePath();
                        cr.SetSourceColor(fillColor);
                        cr.FillPreserve();
                        cr.SetSourceColor(strokeColor);
                        cr.Stroke();
                        break;

                    // End the path object without filling or stroking it. This operator shall be a path-
                    // painting no-op, used primarily for the side effect of changing the current
                    // clipping path (see 8.5.4, "Clipping Path Operators").
                    case OpCodeName.n:
                        cr.ClosePath();
                        break;

                    // (PDF 1.2) Same as SC but also supports Pattern, Separation, DeviceN and ICCBased colour spaces.
                    // If the current stroking colour space is a Separation, DeviceN, or ICCBased colour space, the
                    // operands c 1 ... c n shall be numbers. The number of operands and their interpretation depends
                    // on the colour space. If the current stroking colour space is a Pattern colour space, name shall
                    // be the name of an entry in the Pattern subdictionary of the current resource dictionary
                    // (see 7.8.3, "Resource Dictionaries"). For an uncoloured tiling pattern (PatternType = 1 and
                    // PaintType = 2), c 1 ... c n shall be component values specifying a colour in the pattern’s
                    // underlying colour space. For other types of patterns, these operands shall not be specified.
                    case OpCodeName.SCN:
                    // (PDF 1.2) Same as SCN but used for nonstroking operations.
                    case OpCodeName.scn:
                        if (cOperator.Operands.Count == 1)
                        {
                            if (cOperator.Operands[0] is CName name)
                            {
                                ncolor = new Color(0d / 255d, 172d / 255d, 140d / 255d);
                            }
                            else if (cOperator.Operands[0] is CReal rr)
                            {
                                var r = rr.Value;
                                ncolor = new Color(r, r, r);
                            }
                        }
                        else if (cOperator.Operands.Count == 3)
                        {
                            double r = 0d, g = 0d, b = 0d;
                            if (cOperator.Operands[0] is CReal rr)
                                r = rr.Value;
                            else if (cOperator.Operands[0] is CInteger ir)
                                r = ir.Value;

                            if (cOperator.Operands[1] is CReal rg)
                                g = rg.Value;
                            else if (cOperator.Operands[1] is CInteger ig)
                                g = ig.Value;

                            if (cOperator.Operands[2] is CReal rb)
                                b = rb.Value;
                            else if (cOperator.Operands[2] is CInteger ib)
                                b = ib.Value;

                            ncolor = new Color(r, g, b);
                        }

                        if (ncolor != null)
                        {
                            if (cOperator.OpCode.OpCodeName == OpCodeName.scn)
                                fillColor = ncolor.Value;
                            else
                                strokeColor = ncolor.Value;
                        }

                        break;

                    // (PDF 1.1) Set the current colour space to use for stroking operations. The
                    // operand name shall be a name object. If the colour space is one that can
                    // be specified by a name and no additional parameters (DeviceGray,
                    // DeviceRGB, DeviceCMYK, and certain cases of Pattern), the name may
                    // be specified directly. Otherwise, it shall be a name defined in the
                    // ColorSpace subdictionary of the current resource dictionary (see 7.8.3,
                    // "Resource Dictionaries"); the associated value shall be an array
                    // describing the colour space (see 8.6.3, "Colour Space Families").
                    // The names DeviceGray, DeviceRGB, DeviceCMYK, and Pattern
                    // always identify the corresponding colour spaces directly; they never refer
                    // to resources in the ColorSpace subdictionary.
                    // The CS operator shall also set the current stroking colour to its initial
                    // value, which depends on the colour space:
                    // In a DeviceGray, DeviceRGB, CalGray, or CalRGB colour space, the
                    // initial colour shall have all components equal to 0.0.
                    // In a DeviceCMYK colour space, the initial colour shall be [ 0.0 0.0 0.0 1.0 ].
                    // In a Lab or ICCBased colour space, the initial colour shall have all
                    // components equal to 0.0 unless that falls outside the intervals specified
                    // by the space’s Range entry, in which case the nearest valid value shall be substituted.
                    // In an Indexed colour space, the initial colour value shall be 0.
                    // In a Separation or DeviceN colour space, the initial tint value shall be 1.0 for all colorants.
                    // In a Pattern colour space, the initial colour shall be a pattern object that
                    // causes nothing to be painted.
                    case OpCodeName.CS:
                    // (PDF 1.1) Same as CS but used for nonstroking operations.
                    case OpCodeName.cs:
                        if (cOperator.Operands.Count == 1)
                        {
                            if (cOperator.Operands[0] is CName name)
                            {
                                ncolor = new Color(0d / 255d, 172d / 255d, 140d / 255d);
                            }
                            else if (cOperator.Operands[0] is CReal rr)
                            {
                                var r = rr.Value;
                                ncolor = new Color(r, r, r);
                            }
                        }

                        if (ncolor != null)
                        {
                            if (cOperator.OpCode.OpCodeName == OpCodeName.cs)
                                fillColor = ncolor.Value;
                            else
                                strokeColor = ncolor.Value;
                        }

                        break;

                    // (PDF 1.1) Set the colour to use for stroking operations in a device, CIE-
                    // based (other than ICCBased), or Indexed colour space. The number of
                    // operands required and their interpretation depends on the current
                    // stroking colour space:
                    // For DeviceGray, CalGray, and Indexed colour spaces, one operand
                    // shall be required (n = 1).
                    // For DeviceRGB, CalRGB, and Lab colour spaces, three operands shall be required (n = 3).
                    // For DeviceCMYK, four operands shall be required (n = 4).
                    case OpCodeName.SC:
                    // (PDF 1.1) Same as SC but used for nonstroking operations.
                    case OpCodeName.sc:
                        if (cOperator.Operands.Count == 3)
                        {
                            double r = 0d, g = 0d, b = 0d;
                            if (cOperator.Operands[0] is CReal rr)
                                r = rr.Value;
                            else if (cOperator.Operands[0] is CInteger ir)
                                r = ir.Value;

                            if (cOperator.Operands[1] is CReal rg)
                                g = rg.Value;
                            else if (cOperator.Operands[1] is CInteger ig)
                                g = ig.Value;

                            if (cOperator.Operands[2] is CReal rb)
                                b = rb.Value;
                            else if (cOperator.Operands[2] is CInteger ib)
                                b = ib.Value;

                            ncolor = new Color(r, g, b);
                            if (cOperator.OpCode.OpCodeName == OpCodeName.sc)
                                fillColor = ncolor.Value;
                            else
                                strokeColor = ncolor.Value;
                        }
                        break;

                    // Set the stroking colour space to DeviceGray (or the DefaultGray colour
                    // space; see 8.6.5.6, "Default Colour Spaces") and set the gray level to use
                    // for stroking operations. gray shall be a number between 0.0 (black) and 1.0 (white).
                    case OpCodeName.G:
                    // Same as G but used for nonstroking operations.
                    case OpCodeName.g:
                        Console.WriteLine(cOperator.OpCode.OpCodeName);
                        break;

                    // Set the stroking colour space to DeviceRGB (or the DefaultRGB colour
                    // space; see 8.6.5.6, "Default Colour Spaces") and set the colour to use for
                    // stroking operations. Each operand shall be a number between 0.0
                    // (minimum intensity) and 1.0 (maximum intensity).
                    //
                    // Operands: r g b
                    case OpCodeName.RG:
                    // Same as RG but used for nonstroking operations.
                    case OpCodeName.rg:
                        if (cOperator.Operands.Count == 3)
                        {
                            double r = 0d, g = 0d, b = 0d;
                            if (cOperator.Operands[0] is CReal rr)
                                r = rr.Value;
                            else if (cOperator.Operands[0] is CInteger ir)
                                r = ir.Value;

                            if (cOperator.Operands[1] is CReal rg)
                                g = rg.Value;
                            else if (cOperator.Operands[1] is CInteger ig)
                                g = ig.Value;

                            if (cOperator.Operands[2] is CReal rb)
                                b = rb.Value;
                            else if (cOperator.Operands[2] is CInteger ib)
                                b = ib.Value;

                            var color = new Color(r, g, b);
                            if (cOperator.OpCode.OpCodeName == OpCodeName.rg)
                                fillColor = color;
                            else
                                strokeColor = color;
                        }
                        break;

                    // Set the stroking colour space to DeviceCMYK (or the DefaultCMYK
                    // colour space; see 8.6.5.6, "Default Colour Spaces") and set the colour to
                    // use for stroking operations. Each operand shall be a number between 0.0
                    // (zero concentration) and 1.0 (maximum concentration). The behaviour of
                    // this operator is affected by the overprint mode (see 8.6.7, "Overprint Control").
                    //
                    // Operand: c m y k
                    case OpCodeName.K:
                    // Same as K but used for nonstroking operations.
                    case OpCodeName.k:
                        if (cOperator.Operands.Count == 4)
                        {
                            double c = 0d, m = 0d, y = 0d, k = 0d;
                            if (cOperator.Operands[0] is CReal cc)
                                c = cc.Value;
                            if (cOperator.Operands[1] is CReal cm)
                                m = cm.Value;
                            if (cOperator.Operands[2] is CReal cy)
                                y = cy.Value;
                            if (cOperator.Operands[3] is CReal ck)
                                k = ck.Value;

                            //cr.SetSourceColor(new Cairo.Color(r, g, b));
                        }
                        break;

                    default:
                        Console.WriteLine(cOperator.OpCode.OpCodeName);
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

                cr.MoveTo (cursor.Item1, cursor.Item2 - (fontDescription.Size / Pango.Scale.PangoScale));
                cr.SetSourceColor (strokeColor);

                layout.Width = ((int)(((fontDescription.Size / Pango.Scale.PangoScale) * cString.Value.Length) * Pango.Scale.PangoScale));
                layout.SetText (cString.Value);

                Pango.CairoHelper.ShowLayout(cr, layout);

                cursor = new Tuple<double, double>(cursor.Item1 + ((fontDescription.Size / Pango.Scale.PangoScale) * cString.Value.Length), cursor.Item2);
            }
        }
        #endregion
    }
}
