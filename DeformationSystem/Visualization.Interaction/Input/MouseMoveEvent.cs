using OpenTK.Mathematics;

namespace Visualization.Interaction.Input
{
    public readonly record struct MouseMoveEvent(Vector2 Position) : IInputEvent;
}
