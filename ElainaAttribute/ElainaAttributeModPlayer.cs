using System;

namespace 伊蕾娜.ElainaAttribute;

public class ElainaAttributeModPlayer : ModPlayer
{
    public delegate void OnMagicPointChangedHandler(float magicPoint);

    public event OnMagicPointChangedHandler MagicPointChanged;
    
    //public OnMagicPointChangedHandler MagicPointChanged;
    
    private float magicPoint = 0;
    public float MagicPoint
    {
         get => magicPoint;
         set
         {
             float oldMagicPoint = magicPoint;
             magicPoint = MathF.Max(0, value);
             if (Math.Abs(magicPoint - oldMagicPoint) > 0.001f)
             {
                 MagicPointChanged?.Invoke(magicPoint);   
             }
         }
    }
    
    private float maxMagicPoint = 1;

    public float MaxMagicPoint
    {
         get => maxMagicPoint;
         set => maxMagicPoint = value;
    }

    public override void Load()
    {
        base.Load();
    }

    public override void OnEnterWorld()
    {
        base.OnEnterWorld();
    }

    public override void ResetEffects()
    {
        base.ResetEffects();
    }

    public override void FrameEffects()
    {
        base.FrameEffects();
    }
}