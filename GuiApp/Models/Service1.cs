using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.Models
{
    internal class Service1 : IService1, IDisposable
    {
        private static long idCt;

        private long id;

        public Service1()
        {
            id = idCt++;
        }

        public void Run(string arg)
        {
            Debug.WriteLine("Service1 (" + id + "): " + arg);
        }

        public void Dispose()
        {
            Debug.WriteLine("Service1 (" + id + ") disposed");
        }
    }
}
