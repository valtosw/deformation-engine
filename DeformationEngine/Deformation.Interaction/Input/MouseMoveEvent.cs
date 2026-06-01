using OpenTK.Mathematics;

namespace Deformation.Interaction.Input
{
    public readonly record struct MouseMoveEvent(Vector2 Position, bool IsErase = false) : IInputEvent;
}
