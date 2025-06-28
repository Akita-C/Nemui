using MessagePack;

namespace Nemui.Shared.DTOs.Games.Draw;

[MessagePackObject]
public record ShapeActionData
{
    [Key("shapeType")]
    public string ShapeType { get; set; } = string.Empty;
    
    [Key("properties")]
    public ShapeProperties Properties { get; set; } = new();
}

[MessagePackObject]
public record ShapeProperties
{
    [Key("left")]
    public double Left { get; init; }
    
    [Key("top")]
    public double Top { get; init; }
    
    [Key("width")]
    public double? Width { get; init; }
    
    [Key("height")]
    public double? Height { get; init; }
    
    [Key("radius")]
    public double? Radius { get; init; }
    
    [Key("x1")]
    public double? X1 { get; init; }
    
    [Key("y1")]
    public double? Y1 { get; init; }
    
    [Key("x2")]
    public double? X2 { get; init; }
    
    [Key("y2")]
    public double? Y2 { get; init; }
    
    [Key("color")]
    public string Color { get; set; } = string.Empty;
    
    [Key("strokeWidth")]
    public int StrokeWidth { get; init; }
}