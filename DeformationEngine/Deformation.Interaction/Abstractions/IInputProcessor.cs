using Deformation.Interaction.Input;

namespace Deformation.Interaction.Abstractions
{
    public interface IInputProcessor : IController
    {
        bool ProcessInput(IInputEvent e);
    }
}
