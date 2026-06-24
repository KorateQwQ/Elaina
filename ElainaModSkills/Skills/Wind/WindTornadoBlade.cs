using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace 伊蕾娜.ElainaModSkills.Skills.Wind;

public class WindTornadoBlade
{
    private Vector2 startPosition;
    private readonly Vector2[] controlPoint = [new Vector2(-0.3f), new Vector2(-0.3f, 0.5f), new Vector2(1.3f, 0.8f)];
    private float bladeLength;
    private readonly List<Vector2> trailList = new List<Vector2>();
    private float lifeTime;
    private int timeLeft;
    private int randIndex;
    private float rotation;
    private Vector2 spawnOffset;

    public bool IsAlive => timeLeft > 0;

    public void Reset(Vector2 offset)
    {
        spawnOffset = offset;
        bladeLength = Main.rand.NextFloat(300, 500);
        randIndex = Main.rand.Next(0, controlPoint.Length);
        startPosition = new Vector2(1, 0);
        rotation = Main.rand.NextFloat(0, MathF.PI * 2);
        lifeTime = timeLeft = Main.rand.Next(30, 40);
        trailList.Clear();
    }

    public void Update(Vector2 center)
    {
        if (timeLeft <= 0)
        {
            return;
        }

        timeLeft--;

        float progress = 1f - (timeLeft - 10f) / (lifeTime - 10f);
        progress = MathF.Min(1f, progress);

        Vector2 origin = center + spawnOffset;
        Vector2 position = GetQuadraticBezierPoint(progress,
            origin + startPosition.RotatedBy(rotation) * bladeLength,
            origin,
            origin + controlPoint[randIndex].RotatedBy(rotation) * bladeLength);

        trailList.Add(position);
        if (trailList.Count > 30)
        {
            trailList.RemoveAt(0);
        }
    }

    public void Draw(WindTornado owner, Texture2D wind, Color color)
    {
        if (trailList.Count <= 2)
        {
            return;
        }

        float progress = timeLeft / lifeTime * 1.5f;
        TrailEffect(wind, trailList.ToArray(), color, color, maxWidth: 10, endWidth: 0, startAlpha: progress, endAlpha: progress, blendState: 1);
    }

    /*private static Vector2 GetQuadraticBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        float u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }*/
}
