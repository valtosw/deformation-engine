using Visualization.Interaction.Input;

namespace Visualization.Interaction.Abstractions
{
    public interface IController
    {
        bool ProcessInput(InputEvent e);
        void Update(float deltaTime);
    }
}
