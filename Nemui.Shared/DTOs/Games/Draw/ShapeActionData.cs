namespace Nemui.Shared.DTOs.Games.Draw;

public record ShapeActionData
{
    public string ShapeType { get; init; } = string.Empty;
    public ShapeProperties Properties { get; init; } = new();
}

public record ShapeProperties
{
    public double Left { get; init; }
    public double Top { get; init; }
    public double? Width { get; init; } // for rectangle
    public double? Height { get; init; } // for rectangle
    public double? Radius { get; init; } // for circle
    public double? X1 { get; init; } // for line
    public double? Y1 { get; init; } // for line
    public double? X2 { get; init; } // for line
    public double? Y2 { get; init; } // for line
    public string Color { get; init; } = string.Empty;
    public int StrokeWidth { get; init; }
}