using OpenTK.Mathematics;

namespace Visualization.Interaction.Input
{
    public readonly record struct MouseWheelEvent(Vector2 Position, float Delta) : IInputEvent;
}
