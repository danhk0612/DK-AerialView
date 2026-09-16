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

        prompt.AppendLine("MANDATORY CAMERA TRANSFORMATION:");
        prompt.AppendLine("Create a new perspective reconstruction. Do NOT return a nadir, top-down, orthographic, or nearly top-down aerial image.");
        prompt.AppendLine("Move the virtual camera laterally away from the target site and elevate it so the result is unmistakably oblique.");
        prompt.AppendLine("Building facades, vertical walls, and height differences must be visibly exposed. Roofs alone are not sufficient.");
        prompt.AppendLine("Preserve the plan-view footprint and layout while changing the camera projection into a perspective bird's-eye view.");
        prompt.AppendLine();

        prompt.AppendLine("REFERENCE ROLES:");
        prompt.AppendLine("Reference image 1 is the authoritative Kakao aerial image for building footprints, roads, boundaries, open spaces, site geometry, and relative positions.");
        if (roadviewReferenceCount > 0)
        {
            prompt.AppendLine($"References 2 through {roadviewReferenceCount + 1} are Kakao Roadview photographs captured around the same target site.");
            prompt.AppendLine("Use the Roadview images as authoritative evidence for building height, facade appearance, roof form, materials, openings, vertical proportions, and street-level elevation cues.");
            prompt.AppendLine("Match visible real-world structures across the references. Do not use Roadview camera perspective to relocate footprints or roads.");
        }
        else
        {
            prompt.AppendLine("No Roadview reference is available. Infer vertical geometry conservatively from the aerial image, but still produce a clearly oblique perspective view.");
        }
        prompt.AppendLine();

        prompt.AppendLine("GEOMETRY CONSTRAINTS:");
        prompt.AppendLine("Keep existing buildings in their real positions and preserve their footprint shapes and relative scale.");
        prompt.AppendLine("Keep roads, intersections, site boundaries, open spaces, and terrain relationships geographically consistent with the aerial reference.");
        prompt.AppendLine("Do not invent new buildings, remove existing structures, merge separate buildings, or significantly relocate roads and boundaries.");
        prompt.AppendLine("Extrude and reconstruct the existing footprints into plausible 3D volumes rather than leaving the aerial texture flat.");
        prompt.AppendLine($"Camera angle: {GetAngleText(options.ViewAngle)}.");
        prompt.AppendLine($"Viewing direction: {GetDirectionText(options.Direction)}.");

        prompt.AppendLine(options.StructurePreservation switch
        {
            StructurePreservationLevel.High => "Structure preservation: high. Preserve planimetric geometry very closely, but do not preserve the original top-down projection. Change only the camera projection and the necessary vertical reconstruction.",
            _ => "Structure preservation: medium. Preserve the major site layout and structures while allowing limited cleanup required for a coherent oblique perspective reconstruction."
        });

        prompt.AppendLine(options.RenderStyle switch
        {
            AerialRenderStyle.Architectural => "Style: clean architectural bird's-eye visualization with clear building edges, readable facades, reduced visual noise, and realistic site geometry.",
            _ => "Style: realistic photographic bird's-eye visualization with natural materials, lighting, colors, facade detail, and real-world appearance."
        });

        prompt.AppendLine("Final check: if the result could be mistaken for the original flat satellite/aerial image, increase the perspective and expose more building facades before producing the final image.");

        return prompt.ToString().Trim();
    }

    private static string GetAngleText(AerialViewAnglePreset value) => value switch
    {
        AerialViewAnglePreset.LowOblique => "low oblique; camera looks downward about 35 degrees from horizontal, strongly exposing facades",
        AerialViewAnglePreset.StandardOblique => "standard oblique; camera looks downward about 50 degrees from horizontal, clearly exposing roofs and facades",
        AerialViewAnglePreset.HighOblique => "high oblique; camera looks downward about 65 degrees from horizontal, still visibly perspective and not nadir/top-down",
        _ => "automatic natural oblique angle; choose a perspective that clearly exposes building facades and vertical height"
    };

    private static string GetDirectionText(AerialDirectionPreset value) => value switch
    {
        AerialDirectionPreset.North => "camera views toward the north",
        AerialDirectionPreset.NorthEast => "camera views toward the north-east",
        AerialDirectionPreset.East => "camera views toward the east",
        AerialDirectionPreset.SouthEast => "camera views toward the south-east",
        AerialDirectionPreset.South => "camera views toward the south",
        AerialDirectionPreset.SouthWest => "camera views toward the south-west",
        AerialDirectionPreset.West => "camera views toward the west",
        AerialDirectionPreset.NorthWest => "camera views toward the north-west",
        _ => "automatic; choose the lateral camera direction that reveals the site and the most useful building facades"
    };
}
