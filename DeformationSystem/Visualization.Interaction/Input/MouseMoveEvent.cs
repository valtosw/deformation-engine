using OpenTK.Mathematics;

namespace Visualization.Interaction.Input
{
    public sealed record MouseMoveEvent(Vector2 Position) : InputEvent;
}
