namespace Visualization.Interaction.Input
{
    public readonly record struct KeyEvent(Key Key, InputType InputType) : IInputEvent;
}