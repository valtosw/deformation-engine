using OpenTK.Mathematics;

namespace Visualization.Interaction.Input
{
    public readonly record struct MouseClickEvent(Vector2 Position, MouseButton Button, InputType InputType) : IInputEvent;
}
