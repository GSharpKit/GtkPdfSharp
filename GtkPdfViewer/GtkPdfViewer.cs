using System;
using System.IO;
using System.Net.Mime;
using System.Reflection;
using GLib;
using Gtk;
using XMedicus.PDFViewer;
using Application = Gtk.Application;

namespace GtkPdfViewer
{
    public class Viewer : Application
    {
        static string filebase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase)?.Replace("file:", string.Empty).Replace("bin" + Path.DirectorySeparatorChar + "Debug", string.Empty);
        static string filepath = filebase + "documents" + Path.DirectorySeparatorChar;

        PDFViewer pdf;

        public Viewer() : base("GtkPdfViewer", ApplicationFlags.None)
        {
            Init ();

            Window window = new Window("GtkPdfSharp")
            {
                WindowPosition = WindowPosition.Center,
            };

            window.DestroyEvent += OnDestroy;
            window.Realized += OnRealized;

            pdf = new PDFViewer();
            window.Add(pdf);
            pdf.Show();

            window.SetSizeRequest(1280, 1024);
            window.Show();

            Run();
        }

        void OnRealized(object o, EventArgs args)
        {
            pdf.Render(filepath + "la125.pdf");
        }

        void OnDestroy(object o, EventArgs args)
        {
            Quit();
        }

        public static void Main(string[] args)
        {
            var app = new Viewer();
        }
    }
}
