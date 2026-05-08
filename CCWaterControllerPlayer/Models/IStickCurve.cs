namespace CCWaterControllerPlayer.Models;

public interface IStickCurve
{
    float Apply(float rawInput);
}

public class LinearCurve : IStickCurve
{
    public float Sensitivity { get; set; } = 1.0f;

    public float Apply(float rawInput)
    {
        return rawInput * Sensitivity;
    }
}
