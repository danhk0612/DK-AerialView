using System.Text;
using DKAerialView.Models;

namespace DKAerialView.Services;

public static class OpenRouterPromptBuilder
{
    public static string BuildAerialPrompt(string basePrompt, AerialGenerationOptions options)
    {
        var prompt = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(basePrompt))
        {
            prompt.AppendLine(basePrompt.Trim());
            prompt.AppendLine();
        }

        prompt.AppendLine("Use the provided aerial image as the structural reference.");
        prompt.AppendLine("Transform the scene into a clean oblique bird's-eye view while preserving the real site layout.");
        prompt.AppendLine("Preserve buildings, roads, site boundaries, open spaces, terrain relationships, and major structures as closely as possible.");
        prompt.AppendLine("Do not invent new buildings, remove existing structures, or significantly relocate roads or boundaries.");

        prompt.AppendLine($"Camera angle: {GetAngleText(options.ViewAngle)}.");
        prompt.AppendLine($"Viewing direction: {GetDirectionText(options.Direction)}.");

        prompt.AppendLine(options.StructurePreservation switch
        {
            StructurePreservationLevel.High => "Structure preservation: high. Keep footprints, positions, alignments, and relative scale as close to the reference as possible. If a detail is unclear, infer conservatively rather than redesigning it.",
            _ => "Structure preservation: medium. Keep the major site layout and structures, allowing limited cleanup only where needed for readability."
        });

        prompt.AppendLine(options.RenderStyle switch
        {
            AerialRenderStyle.Architectural => "Style: clean architectural aerial presentation, clearer edges, reduced visual noise, and refined readability while remaining geographically faithful.",
            _ => "Style: realistic aerial visualization with natural materials, lighting, colors, and real-world appearance."
        });

        return prompt.ToString().Trim();
    }

    private static string GetAngleText(AerialViewAnglePreset value) => value switch
    {
        AerialViewAnglePreset.LowOblique => "low oblique, about 35 degrees",
        AerialViewAnglePreset.StandardOblique => "standard oblique, about 50 degrees",
        AerialViewAnglePreset.HighOblique => "high oblique, about 65 degrees",
        _ => "automatic natural oblique angle"
    };

    private static string GetDirectionText(AerialDirectionPreset value) => value switch
    {
        AerialDirectionPreset.North => "north",
        AerialDirectionPreset.NorthEast => "north-east",
        AerialDirectionPreset.East => "east",
        AerialDirectionPreset.SouthEast => "south-east",
        AerialDirectionPreset.South => "south",
        AerialDirectionPreset.SouthWest => "south-west",
        AerialDirectionPreset.West => "west",
        AerialDirectionPreset.NorthWest => "north-west",
        _ => "automatic; choose the direction that best reveals the site layout"
    };
}
