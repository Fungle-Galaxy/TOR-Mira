using MiraAPI.Utilities;
using UnityEngine;

namespace TheOtherRoles.Options;

/// <summary>
/// Mira API 的设置页用 <see cref="ColorExtensions.FindAlternateColor"/> 从组色推导"对比色"，并把它用在两处：
/// 1. 标题文字颜色（标题背景是 sprite × GroupColor，实际很暗）；
/// 2. 选项行所有 SpriteRenderer 的染色（选项文字则直接用 GroupColor）。
/// 该算法假设背景就是 GroupColor 本身，所以只有当 GroupColor 的相对亮度 ≤ 1.05/4.5 - 0.05 ≈ 0.1833 时
/// 才会返回近白色；一旦更亮就返回黑色 —— 于是出现两个毛病：
///   · 亮组色 → 黑标题贴在暗背景上，看不见；
///   · 亮组色 → 整行选项被染成该颜色，观感杂乱。
/// 这里统一把组色压暗到该阈值以下，从而得到：
///   · 标题文字 = 白色（显眼）；
///   · 选项行底色 = 近白色（不整体染色，保持原版观感，仅留极淡的组色）；
///   · 选项文字 = 压暗后的组色（保留按职业/分类的颜色区分）。
/// </summary>
public static class TorOptionColors
{
    /// <summary>留出安全余量的相对亮度上限（阈值 0.1833）。</summary>
    private const float MaxLuminance = 0.17f;

    /// <summary>把组色压暗到 <see cref="MaxLuminance"/> 以下，保持色相不变。</summary>
    public static Color Group(Color color)
    {
        if (color.GetRelativeLuminance() <= MaxLuminance)
        {
            return color;
        }

        // 沿"向黑色插值"的方向逼近：亮度对缩放约为幂函数，16 步足够收敛。
        for (var i = 1; i <= 16; i++)
        {
            var scale = 1f - i / 16f;
            var candidate = new Color(color.r * scale, color.g * scale, color.b * scale, color.a);
            if (candidate.GetRelativeLuminance() <= MaxLuminance)
            {
                return candidate;
            }
        }

        return new Color(0.06f, 0.06f, 0.06f, color.a);
    }
}
