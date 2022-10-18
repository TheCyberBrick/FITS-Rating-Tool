using System;
using System.Diagnostics;
using System.Linq;
using FitsRatingTool.GuiApp.Services;

namespace FitsRatingTool.GuiApp.Models
{
    public class ExampleInjectSingletonMultiple : IContainerRelations, IContainerInstantiation
    {
        public ITestVM0? TestVM { get; private set; }

        private readonly IContainer<ITestVM0, ITestVM0.Args> container;

        public ExampleInjectSingletonMultiple(IContainer<ITestVM0, ITestVM0.Args> container)
        {
            this.container = container;

            // Bind singleton to TestVM property
            container.ToSingletonWithObservable().Subscribe(vm => TestVM = vm);
        }

        public void OnInstantiated()
        {
            // Create a new instance
            var instance1 = container.Instantiate(new ITestVM0.Args());
            Debug.Assert(container.Count == 1);
            Debug.Assert(container.Contains(instance1));
            Debug.Assert(TestVM == instance1);

            // Create another instance and replace the previous one which will be disposed
            var instance2 = container.Instantiate(new ITestVM0.Args());
            Debug.Assert(container.Count == 1);
            Debug.Assert(container.Contains(instance2));
            Debug.Assert(TestVM == instance2);
        }

        public void OnAdded(object dependency)
        {
        }

        public void OnAddedTo(object dependee)
        {
        }

        public void OnRemoved(object dependency)
        {
        }
    }
}
