using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace EditorTest.Data;

public record class CssWebData(
    IReadOnlyList<PropertyData> Properties,
    IReadOnlyList<PseudoClassData> PseudoClasses,
    IReadOnlyList<PseudoElementData> PseudoElements
)
{
    public static CssWebData Load()
    {
        var type = typeof(CssWebData).Assembly;
        using var stream = type.GetManifestResourceStream($"{type.GetName().Name}.Data.css.json");
        using var reader = new JsonTextReader(new StreamReader(stream));
        var serializer = new JsonSerializer();
        var original = serializer.Deserialize<CssWebData>(reader);
        return original with
        {
            PseudoClasses = original.PseudoClasses.Select(p => p with
            {
                Name = p.Name.Substring(1)
            }).ToList(),
            PseudoElements = original.PseudoElements.Select(p => p with
            {
                Name = p.Name.Substring(2)
            }).ToList(),
        };
    }

    public static CssWebDataIndex Index = new(Load());

    public IReadOnlyList<string> Units =
        [
        "%",
        "px",
        "em",
        "rem",
        "vw",
        "vh",
        "svw",
        "svh",
        "lvw",
        "lvh",
        "dvw",
        "dvh",
        "vb",
        "vi",
        "lh",
        "rlh",
        "vmin",
        "vmax",
        "cm",
        "mm",
        "in",
        "pt",
        "pc",
        "ch",
        "q",
        "deg",
        "grad",
        "rad",
        "s",
        "ms",
        ];

    public IReadOnlyList<string> Keywords = [
        "inherit",
        "initial",
        "revert",
        "revert-layer",
        "unset",
    ];

    public IReadOnlyList<string> Functions = [
        "abs",
        "acos",
        "asin",
        "atan",
        "atan2",
        "attr",
        "blur",
        "brightness",
        "cos",
        "contrast",
        "cross-fade",
        "cubic-bezier",
        "conic-gradient",
        "circle",
        "clamp",
        "drop-shadow",
        "element",
        "ellipse",
        "env",
        "exp",
        "fit-content",
        "format",
        "grayscale",
        "hsl",
        "hsla",
        "hue-rotate",
        "hypot",
        "image",
        "image-set",
        "inset",
        "invert",
        "linear-gradient",
        "local",
        "log",
        "matrix",
        "matrix3d",
        "max",
        "min",
        "minmax",
        "mod",
        "opacity",
        "paint",
        "range",
        "ray",
        "rect",
        "rem",
        "repeat",
        "repeating-conic-gradient",
        "repeating-linear-gradient",
        "repeating-radial-gradient",
        "reverse",
        "rotate",
        "rotate3d",
        "rotateX",
        "rotateY",
        "rotateZ",
        "round",
        "rgb",
        "rgba",
        "scale",
        "scale3d",
        "scaleX",
        "scaleY",
        "scaleZ",
        "scroll",
        "sepia",
        "skew",
        "sign",
        "sin",
        "sqrt",
        "steps",
        "translate",
        "translate3d",
        "translateX",
        "translateY",
        "translateZ",
        "url",
        "var",
    ];

    public IReadOnlyList<ColorData> Colors = [
    new ColorData("aliceblue", "#F0F8FF", "240 248 255"),
new ColorData("antiquewhite", "#FAEBD7", "250 235 215"),
new ColorData("aqua", "#00FFFF", "0 255 255"),
new ColorData("aquamarine", "#7FFFD4", "127 255 212"),
new ColorData("azure", "#F0FFFF", "240 255 255"),
new ColorData("beige", "#F5F5DC", "245 245 220"),
new ColorData("bisque", "#FFE4C4", "255 228 196"),
new ColorData("black", "#000000", "0 0 0"),
new ColorData("blanchedalmond", "#FFEBCD", "255 235 205"),
new ColorData("blue", "#0000FF", "0 0 255"),
new ColorData("blueviolet", "#8A2BE2", "138 43 226"),
new ColorData("brown", "#A52A2A", "165 42 42"),
new ColorData("burlywood", "#DEB887", "222 184 135"),
new ColorData("cadetblue", "#5F9EA0", "95 158 160"),
new ColorData("chartreuse", "#7FFF00", "127 255 0"),
new ColorData("chocolate", "#D2691E", "210 105 30"),
new ColorData("coral", "#FF7F50", "255 127 80"),
new ColorData("cornflowerblue", "#6495ED", "100 149 237"),
new ColorData("cornsilk", "#FFF8DC", "255 248 220"),
new ColorData("crimson", "#DC143C", "220 20 60"),
new ColorData("cyan", "#00FFFF", "0 255 255"),
new ColorData("darkblue", "#00008B", "0 0 139"),
new ColorData("darkcyan", "#008B8B", "0 139 139"),
new ColorData("darkgoldenrod", "#B8860B", "184 134 11"),
new ColorData("darkgray", "#A9A9A9", "169 169 169"),
new ColorData("darkgreen", "#006400", "0 100 0"),
new ColorData("darkgrey", "#A9A9A9", "169 169 169"),
new ColorData("darkkhaki", "#BDB76B", "189 183 107"),
new ColorData("darkmagenta", "#8B008B", "139 0 139"),
new ColorData("darkolivegreen", "#556B2F", "85 107 47"),
new ColorData("darkorange", "#FF8C00", "255 140 0"),
new ColorData("darkorchid", "#9932CC", "153 50 204"),
new ColorData("darkred", "#8B0000", "139 0 0"),
new ColorData("darksalmon", "#E9967A", "233 150 122"),
new ColorData("darkseagreen", "#8FBC8F", "143 188 143"),
new ColorData("darkslateblue", "#483D8B", "72 61 139"),
new ColorData("darkslategray", "#2F4F4F", "47 79 79"),
new ColorData("darkslategrey", "#2F4F4F", "47 79 79"),
new ColorData("darkturquoise", "#00CED1", "0 206 209"),
new ColorData("darkviolet", "#9400D3", "148 0 211"),
new ColorData("deeppink", "#FF1493", "255 20 147"),
new ColorData("deepskyblue", "#00BFFF", "0 191 255"),
new ColorData("dimgray", "#696969", "105 105 105"),
new ColorData("dimgrey", "#696969", "105 105 105"),
new ColorData("dodgerblue", "#1E90FF", "30 144 255"),
new ColorData("firebrick", "#B22222", "178 34 34"),
new ColorData("floralwhite", "#FFFAF0", "255 250 240"),
new ColorData("forestgreen", "#228B22", "34 139 34"),
new ColorData("fuchsia", "#FF00FF", "255 0 255"),
new ColorData("gainsboro", "#DCDCDC", "220 220 220"),
new ColorData("ghostwhite", "#F8F8FF", "248 248 255"),
new ColorData("gold", "#FFD700", "255 215 0"),
new ColorData("goldenrod", "#DAA520", "218 165 32"),
new ColorData("gray", "#808080", "128 128 128"),
new ColorData("green", "#008000", "0 128 0"),
new ColorData("greenyellow", "#ADFF2F", "173 255 47"),
new ColorData("grey", "#808080", "128 128 128"),
new ColorData("honeydew", "#F0FFF0", "240 255 240"),
new ColorData("hotpink", "#FF69B4", "255 105 180"),
new ColorData("indianred", "#CD5C5C", "205 92 92"),
new ColorData("indigo", "#4B0082", "75 0 130"),
new ColorData("ivory", "#FFFFF0", "255 255 240"),
new ColorData("khaki", "#F0E68C", "240 230 140"),
new ColorData("lavender", "#E6E6FA", "230 230 250"),
new ColorData("lavenderblush", "#FFF0F5", "255 240 245"),
new ColorData("lawngreen", "#7CFC00", "124 252 0"),
new ColorData("lemonchiffon", "#FFFACD", "255 250 205"),
new ColorData("lightblue", "#ADD8E6", "173 216 230"),
new ColorData("lightcoral", "#F08080", "240 128 128"),
new ColorData("lightcyan", "#E0FFFF", "224 255 255"),
new ColorData("lightgoldenrodyellow", "#FAFAD2", "250 250 210"),
new ColorData("lightgray", "#D3D3D3", "211 211 211"),
new ColorData("lightgreen", "#90EE90", "144 238 144"),
new ColorData("lightgrey", "#D3D3D3", "211 211 211"),
new ColorData("lightpink", "#FFB6C1", "255 182 193"),
new ColorData("lightsalmon", "#FFA07A", "255 160 122"),
new ColorData("lightseagreen", "#20B2AA", "32 178 170"),
new ColorData("lightskyblue", "#87CEFA", "135 206 250"),
new ColorData("lightslategray", "#778899", "119 136 153"),
new ColorData("lightslategrey", "#778899", "119 136 153"),
new ColorData("lightsteelblue", "#B0C4DE", "176 196 222"),
new ColorData("lightyellow", "#FFFFE0", "255 255 224"),
new ColorData("lime", "#00FF00", "0 255 0"),
new ColorData("limegreen", "#32CD32", "50 205 50"),
new ColorData("linen", "#FAF0E6", "250 240 230"),
new ColorData("magenta", "#FF00FF", "255 0 255"),
new ColorData("maroon", "#800000", "128 0 0"),
new ColorData("mediumaquamarine", "#66CDAA", "102 205 170"),
new ColorData("mediumblue", "#0000CD", "0 0 205"),
new ColorData("mediumorchid", "#BA55D3", "186 85 211"),
new ColorData("mediumpurple", "#9370DB", "147 112 219"),
new ColorData("mediumseagreen", "#3CB371", "60 179 113"),
new ColorData("mediumslateblue", "#7B68EE", "123 104 238"),
new ColorData("mediumspringgreen", "#00FA9A", "0 250 154"),
new ColorData("mediumturquoise", "#48D1CC", "72 209 204"),
new ColorData("mediumvioletred", "#C71585", "199 21 133"),
new ColorData("midnightblue", "#191970", "25 25 112"),
new ColorData("mintcream", "#F5FFFA", "245 255 250"),
new ColorData("mistyrose", "#FFE4E1", "255 228 225"),
new ColorData("moccasin", "#FFE4B5", "255 228 181"),
new ColorData("navajowhite", "#FFDEAD", "255 222 173"),
new ColorData("navy", "#000080", "0 0 128"),
new ColorData("oldlace", "#FDF5E6", "253 245 230"),
new ColorData("olive", "#808000", "128 128 0"),
new ColorData("olivedrab", "#6B8E23", "107 142 35"),
new ColorData("orange", "#FFA500", "255 165 0"),
new ColorData("orangered", "#FF4500", "255 69 0"),
new ColorData("orchid", "#DA70D6", "218 112 214"),
new ColorData("palegoldenrod", "#EEE8AA", "238 232 170"),
new ColorData("palegreen", "#98FB98", "152 251 152"),
new ColorData("paleturquoise", "#AFEEEE", "175 238 238"),
new ColorData("palevioletred", "#DB7093", "219 112 147"),
new ColorData("papayawhip", "#FFEFD5", "255 239 213"),
new ColorData("peachpuff", "#FFDAB9", "255 218 185"),
new ColorData("peru", "#CD853F", "205 133 63"),
new ColorData("pink", "#FFC0CB", "255 192 203"),
new ColorData("plum", "#DDA0DD", "221 160 221"),
new ColorData("powderblue", "#B0E0E6", "176 224 230"),
new ColorData("purple", "#800080", "128 0 128"),
new ColorData("rebeccapurple", "#663399", "102 51 153"),
new ColorData("red", "#FF0000", "255 0 0"),
new ColorData("rosybrown", "#BC8F8F", "188 143 143"),
new ColorData("royalblue", "#4169E1", "65 105 225"),
new ColorData("saddlebrown", "#8B4513", "139 69 19"),
new ColorData("salmon", "#FA8072", "250 128 114"),
new ColorData("sandybrown", "#F4A460", "244 164 96"),
new ColorData("seagreen", "#2E8B57", "46 139 87"),
new ColorData("seashell", "#FFF5EE", "255 245 238"),
new ColorData("sienna", "#A0522D", "160 82 45"),
new ColorData("silver", "#C0C0C0", "192 192 192"),
new ColorData("skyblue", "#87CEEB", "135 206 235"),
new ColorData("slateblue", "#6A5ACD", "106 90 205"),
new ColorData("slategray", "#708090", "112 128 144"),
new ColorData("slategrey", "#708090", "112 128 144"),
new ColorData("snow", "#FFFAFA", "255 250 250"),
new ColorData("springgreen", "#00FF7F", "0 255 127"),
new ColorData("steelblue", "#4682B4", "70 130 180"),
new ColorData("tan", "#D2B48C", "210 180 140"),
new ColorData("teal", "#008080", "0 128 128"),
new ColorData("thistle", "#D8BFD8", "216 191 216"),
new ColorData("tomato", "#FF6347", "255 99 71"),
new ColorData("turquoise", "#40E0D0", "64 224 208"),
new ColorData("violet", "#EE82EE", "238 130 238"),
new ColorData("wheat", "#F5DEB3", "245 222 179"),
new ColorData("white", "#FFFFFF", "255 255 255"),
new ColorData("whitesmoke", "#F5F5F5", "245 245 245"),
new ColorData("yellow", "#FFFF00", "255 255 0"),
new ColorData("yellowgreen", "#9ACD32", "154 205 50"),
new ColorData("transparent", null, null),
    ];

    public readonly SystemColor[] SystemColors = new SystemColor[]
{
    new("AccentColor", "Background of accented user interface controls"),
    new("AccentColorText", "Text of accented user interface controls"),
    new("ActiveText", "Text of active links"),
    new("ButtonBorder", "Base border color of controls"),
    new("ButtonFace", "Background color of controls"),
    new("ButtonText", "Text color of controls"),
    new("Canvas", "Background of application content or documents"),
    new("CanvasText", "Text color in application content or documents"),
    new("Field", "Background of input fields"),
    new("FieldText", "Text in input fields"),
    new("GrayText", "Text color for disabled items (e.g. a disabled control)"),
    new("Highlight", "Background of selected items"),
    new("HighlightText", "Text color of selected items"),
    new("LinkText", "Text of non-active, non-visited links"),
    new("Mark", "Background of text that has been specially marked (such as by the HTML mark element)"),
    new("MarkText", "Text that has been specially marked (such as by the HTML mark element)"),
    new("SelectedItem", "Background of selected items, for example, a selected checkbox"),
    new("SelectedItemText", "Text of selected items"),
    new("VisitedText", "Text of visited links")
};
}

public record class PropertyData(string Name, string? Description, IReadOnlyList<PropertyValueData>? Values, string Syntax, ISet<string> Restrictions);

public record class PropertyValueData(string Name, string? Description);

public record class ColorData(string Name, string Hex, string Rgb);

public record class SystemColor(string Name, string Description);

public record class PseudoClassData(string Name, string Description);

public record class PseudoElementData(string Name, string Description);



public record class CssWebDataIndex(CssWebData Data)
{
    public IReadOnlyList<string> VendorPrefixes { get; } = ["-moz-", "-o-", "-ms-", "-webkit-"];


    public IReadOnlyDictionary<string, PropertyData> Properties { get; } = Data.Properties.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<PropertyData> PropertiesSorted { get; } = Data.Properties.OrderBy(p => p.Name).ToList();


    public IReadOnlyDictionary<string, PseudoClassData> PseudoClasses { get; } = Data.PseudoClasses.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<PseudoClassData> PseudoClassesSorted { get; } = Data.PseudoClasses.OrderBy(p => p.Name.TrimEnd('(', ')')).ToList();


    public IReadOnlyDictionary<string, PseudoElementData> PseudoElements { get; } = Data.PseudoElements.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<PseudoElementData> PseudoElementsSorted { get; } = Data.PseudoElements.OrderBy(p => p.Name).ToList();


    public IReadOnlyList<ColorData> NamedColorsSorted { get; } = Data.Colors.OrderBy(c => c.Name).ToList();

    public IReadOnlyDictionary<string, ColorData> NamedColors { get; } = Data.Colors.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);


    public IReadOnlyList<SystemColor> SystemColorsSorted { get; } = Data.SystemColors.OrderBy(c => c.Name).ToList();

    public IReadOnlyDictionary<string, SystemColor> SystemColors { get; } = Data.SystemColors.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);


    public IReadOnlyList<string> ValueKeywordsSorted { get; } = Data.Keywords.OrderBy(c => c).ToList();

    public ISet<string> ValueKeywordsSet { get; } = Data.Keywords.ToHashSet(StringComparer.OrdinalIgnoreCase);


    public IReadOnlyList<string> FunctionNamesSorted { get; } = Data.Functions.OrderBy(c => c).ToList();

    public ISet<string> FunctionNamesSet { get; } = Data.Functions.ToHashSet(StringComparer.OrdinalIgnoreCase);


    public IReadOnlyList<string> ValueUnitsSorted { get; } = Data.Units.OrderBy(c => c).ToList();

    public ISet<string> ValueUnitsSet { get; } = Data.Units.ToHashSet(StringComparer.OrdinalIgnoreCase);
}
