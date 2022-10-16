using FitsRatingTool.GuiApp.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.Models
{
    internal class TestVM1 : ITestVM1, IDisposable, IContainerEvents
    {
        public static readonly object MY_SCOPE = "TestVM1 Scope";

        private string someValue;

        public TestVM1(IRegistrar<ITestVM1, ITestVM1.Args> reg)
        {
            //reg.RegisterAndReturn<TestVM1>(reg.ClassScope);
            reg.RegisterAndReturn<TestVM1>(MY_SCOPE);
        }

        private IContainer<ITestVM2, ITestVM2.Args> testVm2Container { get; set; }

        private readonly IFitsImageManager manager;
        private readonly IService1 svc;

        private TestVM1(ITestVM1.Args args, IFitsImageManager manager, IContainer<ITestVM2, ITestVM2.Args> testVm2Container, IService1 svc)
        {
            Debug.WriteLine("TestVM1 created");
            someValue = args.name;
            this.manager = manager;
            this.svc = svc;
            this.testVm2Container = testVm2Container;
        }

        public void DoSomething()
        {
            var a = new ITestVM2.Args();

            svc.Run("TestVM1.1");

            a.name = "123";
            testVm2Container.Instantiate(a);
            Debug.WriteLine("TestVM1.1: " + someValue + " " + manager);
            testVm2Container.Instance.DoSomething();

            a.name = "456";
            testVm2Container.Instantiate(a);
            Debug.WriteLine("TestVM1.2: " + someValue + " " + manager);
            testVm2Container.Instance.DoSomething();

            svc.Run("TestVM1.2");
        }

        public void Dispose()
        {
            Debug.WriteLine("TestVM1 Dispose");
        }

        void IContainerEvents.OnAdded(object dependency)
        {
            Debug.WriteLine("TestVM1 OnAdded: " + dependency);
        }

        void IContainerEvents.OnRemoved(object dependency)
        {
            Debug.WriteLine("TestVM1 OnRemoved: " + dependency);
        }

        void IContainerEvents.OnAddedTo(object dependee)
        {
            Debug.WriteLine("TestVM1 OnAddedTo: " + dependee);
        }

        void IContainerEvents.OnInstantiated()
        {
            Debug.WriteLine("TestVM1 OnInstantiated");
        }
    }
}
