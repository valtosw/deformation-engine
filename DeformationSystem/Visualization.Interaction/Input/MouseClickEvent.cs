using OpenTK.Mathematics;

namespace Visualization.Interaction.Input
{
    public sealed record MouseClickEvent(Vector2 Position, MouseButton Button, InputType InputType) : InputEvent;
}
