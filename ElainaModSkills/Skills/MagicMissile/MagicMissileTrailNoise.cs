using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.MagicMissile;

/// <summary>持有正式 PerlinX 纹理，交给拖尾 Shader 采样；不生成贴图，不回读 GPU。</summary>
public sealed class MagicMissileTrailNoise
{
    public Texture2D Texture { get; }

    public MagicMissileTrailNoise(Texture2D texture)
    {
        Texture = texture;
    }
}
