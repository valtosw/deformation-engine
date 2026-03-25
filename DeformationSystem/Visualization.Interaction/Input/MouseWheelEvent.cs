using OpenTK.Mathematics;

namespace Visualization.Interaction.Input
{
    public sealed record MouseWheelEvent(Vector2 Position, float Delta) : InputEvent;
}
