using FitsRatingTool.GuiApp.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.Models
{
    internal class TestVM1 : ITestVM1, IDisposable, IContainerRelations, IContainerInstantiation
    {
        private static long idCt = 0;

        private long id;

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
            id = idCt++;

            someValue = args.name;
            Debug.WriteLine("TestVM1 (" + id + ", " + someValue + ") created");
            this.manager = manager;
            this.svc = svc;
            this.testVm2Container = testVm2Container.ToSingleton();
        }

        public void DoSomething()
        {
            var a = new ITestVM2.Args();

            svc.Run("TestVM1 (" + id + ", " + someValue + ") 1");

            a.name = "123";
            testVm2Container.Instantiate(a);
            Debug.WriteLine("TestVM1 (" + id + ", " + someValue + ") 2: " + someValue + " " + manager);
            testVm2Container.GetAny().DoSomething();

            a.name = "456";
            testVm2Container.Instantiate(a);
            Debug.WriteLine("TestVM1 (" + id + ", " + someValue + ") 3: " + someValue + " " + manager);
            testVm2Container.GetAny().DoSomething();

            svc.Run("TestVM1 (" + id + ", " + someValue + ") 4");
        }

        public void Dispose()
        {
            Debug.WriteLine("TestVM1 (" + id + ", " + someValue + ") Dispose");
        }

        void IContainerRelations.OnAdded(object dependency)
        {
            Debug.WriteLine("TestVM1 (" + id + ", " + someValue + ") OnAdded: " + dependency);
        }

        void IContainerRelations.OnRemoved(object dependency)
        {
            Debug.WriteLine("TestVM1 (" + id + ", " + someValue + ") OnRemoved: " + dependency);
        }

        void IContainerRelations.OnAddedTo(object dependee)
        {
            Debug.WriteLine("TestVM1 (" + id + ", " + someValue + ") OnAddedTo: " + dependee);
        }

        void IContainerInstantiation.OnInstantiated()
        {
            Debug.WriteLine("TestVM1 (" + id + ", " + someValue + ") OnInstantiated");
        }

        public override string ToString()
        {
            return "TestVM1 (" + id + ", " + someValue + ")";
        }
    }
}
