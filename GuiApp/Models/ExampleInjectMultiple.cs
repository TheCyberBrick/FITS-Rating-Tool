using System;
using System.Diagnostics;
using System.Linq;
using FitsRatingTool.GuiApp.Services;

namespace FitsRatingTool.GuiApp.Models
{
    public class ExampleInjectMultiple : IContainerRelations, IContainerInstantiation
    {
        private readonly IContainer<ITestVM0, ITestVM0.Args> container;

        public ExampleInjectMultiple(IContainer<ITestVM0, ITestVM0.Args> container)
        {
            this.container = container;
        }

        public void OnInstantiated()
        {
            // Create a new instance
            var instance1 = container.Instantiate(new ITestVM0.Args());
            Debug.Assert(container.Count == 1);
            Debug.Assert(container.Contains(instance1));

            // Create another instance
            var instance2 = container.Instantiate(new ITestVM0.Args());
            Debug.Assert(container.Count == 2);
            Debug.Assert(container.Contains(instance2));

            // Remove the first instance
            container.Destroy(instance1);
            Debug.Assert(container.Count == 1);
            Debug.Assert(!container.Contains(instance1));
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
