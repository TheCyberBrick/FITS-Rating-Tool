using FitsRatingTool.GuiApp.Services;
using System.Diagnostics;

namespace FitsRatingTool.GuiApp.Models
{
    public class ExampleInjectSingleton : IContainerRelations, IContainerInstantiation
    {
        public ITestVM0 TestVM { get; private set; } = null!;

        public ExampleInjectSingleton(IContainer<ITestVM0, ITestVM0.Args> container)
        {
            // Inject a single instance into TestMV0
            container.ToSingleton().Inject(new ITestVM0.Args(), vm => TestVM = vm);
        }

        public void OnInstantiated()
        {
            Debug.Assert(TestVM != null);
        }

        public void OnAdded(object dependency)
        {
        }

        public void OnRemoved(object dependency)
        {
        }

        public void OnAddedTo(object dependee)
        {
        }
    }
}
