using FitsRatingTool.GuiApp.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.Models
{
    internal class TestVM2 : ITestVM2, IDisposable, IContainerEvents
    {
        private static long idCt = 0;

        private long id;

        private string someValue;

        public TestVM2(IRegistrar<ITestVM2, ITestVM2.Args> reg)
        {
            reg.RegisterAndReturn<TestVM2>();
        }

        private readonly IService1 svc;

        private TestVM2(ITestVM2.Args args, IFitsImageManager manager, IService1 svc)
        {
            id = idCt++;
            Debug.WriteLine("TestVM2 (" + id + ") created");
            someValue = args.name;
            this.svc = svc;
        }

        public void DoSomething()
        {
            svc.Run("TestVM2");

            Debug.WriteLine("TestVM2 (" + id + "): " + someValue);
        }

        public void Dispose()
        {
            Debug.WriteLine("TestVM2 (" + id + ") Dispose");
        }

        void IContainerEvents.OnAdded(object dependency)
        {
            Debug.WriteLine("TestVM2 (" + id + ") OnAdded: " + dependency);
        }

        void IContainerEvents.OnRemoved(object dependency)
        {
            Debug.WriteLine("TestVM2 (" + id + ") OnRemoved: " + dependency);
        }

        void IContainerEvents.OnAddedTo(object dependee)
        {
            Debug.WriteLine("TestVM2 (" + id + ") OnAddedTo: " + dependee);
        }

        void IContainerEvents.OnInstantiated()
        {
            Debug.WriteLine("TestVM2 (" + id + ") OnInstantiated");
        }

        public override string ToString()
        {
            return "TestVM2 (" + id + ")";
        }
    }
}
