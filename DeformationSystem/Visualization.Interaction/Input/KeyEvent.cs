namespace Visualization.Interaction.Input
{
    public sealed record KeyEvent(Key Key, InputType InputType) : InputEvent;
}