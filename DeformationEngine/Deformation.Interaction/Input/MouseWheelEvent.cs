using OpenTK.Mathematics;

namespace Deformation.Interaction.Input
{
    public readonly record struct MouseWheelEvent(Vector2 Position, float Delta) : IInputEvent;
}
