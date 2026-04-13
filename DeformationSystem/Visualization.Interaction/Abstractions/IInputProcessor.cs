using Visualization.Interaction.Input;

namespace Visualization.Interaction.Abstractions
{
    public interface IInputProcessor : IController
    {
        bool ProcessInput(IInputEvent e);
    }
}
