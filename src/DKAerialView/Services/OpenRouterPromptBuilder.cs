using System.Text;
using DKAerialView.Models;

namespace DKAerialView.Services;

public static class OpenRouterPromptBuilder
{
    public static string BuildAerialPrompt(string basePrompt, AerialGenerationOptions options, int roadviewReferenceCount = 0)
    {
        var prompt = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(basePrompt))
        {
            prompt.AppendLine(basePrompt.Trim());
            prompt.AppendLine();
        }

        prompt.AppendLine("The first reference image is the authoritative aerial image for site layout, building footprints, roads, boundaries, open spaces, and relative positions.");
        if (roadviewReferenceCount > 0)
        {
            prompt.AppendLine($"The next {roadviewReferenceCount} reference images are Kakao Roadview photographs captured around the target site.");
            prompt.AppendLine("Use the Roadview images only to infer building height, facade appearance, roof form, materials, vertical proportions, and other real-world elevation cues.");
            prompt.AppendLine("Do not let Roadview perspective change the authoritative footprint or site layout from the aerial image.");
        }

        prompt.AppendLine("Transform the scene into a clean oblique bird's-eye view while preserving the real site layout.");
        prompt.AppendLine("Preserve buildings, roads, site boundaries, open spaces, terrain relationships, and major structures as closely as possible.");
        prompt.AppendLine("Do not invent new buildings, remove existing structures, or significantly relocate roads or boundaries.");
        prompt.AppendLine($"Camera angle: {GetAngleText(options.ViewAngle)}.");
        prompt.AppendLine($"Viewing direction: {GetDirectionText(options.Direction)}.");

        prompt.AppendLine(options.StructurePreservation switch
        {
            StructurePreservationLevel.High => "Structure preservation: high. Keep footprints, positions, alignments, and relative scale as close to the aerial reference as possible. If a detail is unclear, infer conservatively rather than redesigning it.",
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
