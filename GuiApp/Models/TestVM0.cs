using FitsRatingTool.GuiApp.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitsRatingTool.GuiApp.Models
{
    internal class TestVM0 : ReactiveObject, ITestVM0, IDisposable, IContainerRelations, IContainerInstantiation
    {
        // Designer only
        public TestVM0()
        {
        }

        public TestVM0(IRegistrar<ITestVM0, ITestVM0.Args> reg)
        {
            reg.RegisterAndReturn<TestVM0>();
        }

        public ITestVM1 TestVm1 { get; private set; }

        private readonly IContainer<ITestVM1, ITestVM1.Args> testVm1Container2;

        private TestVM0(ITestVM0.Args args, /*IContainer<ITestVM1, ITestVM1.Args> testVm1Container*/Func<IContainer<ITestVM1, ITestVM1.Args>> testVm1ContainerF/*, IContainer<ITestVM2, ITestVM2.Args> testVm2Container*/)
        {
            Debug.WriteLine("TestVM0 created");

            var a = new ITestVM1.Args();

            var testVm1Container1 = testVm1ContainerF();

            a.name = "I1";
            //testVm1Container1.Instantiate(a);

            //testVm1Container1.ToSingletonWithObservable().Subscribe(vm => MyVm = vm);

            a = new ITestVM1.Args();
            a.name = "I2";
            testVm1Container1/*.ToSingleton()*/.Inject(a, (vm) => Debug.WriteLine("Injected: " + vm));

            a = new ITestVM1.Args();
            a.name = "I4";
            testVm1Container1.Inject(a, (vm) => Debug.WriteLine("Injected: " + vm));
            testVm1Container1.Inject(a, (vm) => Debug.WriteLine("Injected: " + vm));
            //initContainers(testVm1Container1);

            //testVm1Container1.WhenChanged.Subscribe(vm => TestVm1 = vm!);

            testVm1Container2 = testVm1ContainerF();
            //testVm1Container2 = testVm1Container;

            a = new ITestVM1.Args();
            a.name = "I3";
            testVm1Container2.Inject(a);
            testVm1Container2.Inject(a);
            testVm1Container2.Inject(a);
            //testVm1Container2.Inject(a, vm => testVm1Container2.Remove(vm));

            testVm1Container2.ToSingletonWithObservable().Subscribe(vm => TestVm1 = vm!);
        }

        [MemberNotNull(nameof(TestVm1))]
        private void initContainers(IContainer<ITestVM1, ITestVM1.Args> c)
        {

        }

        public void Test()
        {
            var a = new ITestVM1.Args();
            a = new ITestVM1.Args();
            a.name = "I3";

            //testVm1Container2.Instantiate(a);

            //testVm1Container2.Remove(TestVm1);

            TestVm1?.DoSomething();

            /*testVm2Container.Instantiate(new ITestVM2.Args());
            testVm2Container.Instance.DoSomething();*/
        }

        public void Dispose()
        {
            Debug.WriteLine("TestVM0 Dispose");
        }

        void IContainerRelations.OnAdded(object dependency)
        {
            Debug.WriteLine("TestVM0 OnAdded: " + dependency);
        }

        void IContainerRelations.OnRemoved(object dependency)
        {
            Debug.WriteLine("TestVM0 OnRemoved: " + dependency);
        }

        void IContainerRelations.OnAddedTo(object dependee)
        {
            Debug.WriteLine("TestVM0 OnAddedTo: " + dependee);
        }

        void IContainerInstantiation.OnInstantiated()
        {
            Debug.WriteLine("TestVM0 OnInstantiated");
        }

        public override string ToString()
        {
            return "TestVM0";
        }
    }
}
