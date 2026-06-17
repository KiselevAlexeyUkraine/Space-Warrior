namespace Services
{
    public interface IInput
    {
        float MouseY { get; }

        bool Escape { get; }
        bool Sprint { get; }

        float Vertical { get; }
        float Horizontal { get; }

        bool Ascend { get; }
        bool Descend { get; }
    }
}