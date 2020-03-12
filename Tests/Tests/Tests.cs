using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Gtk;
using XMedicus.PDFViewer;

namespace Viewer
{
    [TestFixture]
    public class Tests
    {
        static string filebase = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase)?.Replace("file:", string.Empty).Replace("bin" + Path.DirectorySeparatorChar + "Debug", string.Empty);
        static string filepath = filebase + "documents" + Path.DirectorySeparatorChar;

        PDFViewer pdf;

        public Tests()
        {
            Application.Init ();

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
        }

        [Test]
        public void Test1()
        {
            pdf.Render(filepath + "la125.pdf");

            Application.Run ();

            Assert.True(true);
        }

        void OnRealized(object o, EventArgs args)
        {
        }

        void OnDestroy(object o, EventArgs args)
        {
            Console.WriteLine("Hej");
            Application.Quit();
        }
    }
}
