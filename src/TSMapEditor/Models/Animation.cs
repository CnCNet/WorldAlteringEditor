using Microsoft.Xna.Framework;
using TSMapEditor.GameMath;

namespace TSMapEditor.Models
{
    public class Animation : GameObject
    {
        public Animation(AnimType animType)
        {
            AnimType = animType;
        }

        public Animation(AnimType animType, Point2D position) : this(animType)
        {
            Position = position;
        }

        public override RTTIType WhatAmI() => RTTIType.Anim;

        public override GameObjectType GetObjectType() => AnimType;

        public AnimType AnimType { get; private set; }
        public House Owner { get; set; }
        public byte Facing { get; set; }
        public bool IsBuildingAnim { get; set; }
        public Structure ParentBuilding { get; set; }
        public bool IsTurretAnim { get; set; }
        public BuildingAnimDrawConfig BuildingAnimDrawConfig { get; set; } // Contains offsets loaded from the parent building

        public override int GetYDrawOffset()
        {
            return Constants.CellSizeY / -2 + AnimType.ArtConfig.YDrawOffset +
                   (IsBuildingAnim ? BuildingAnimDrawConfig.Y : 0);
        }

        public override int GetXDrawOffset()
        {
            return AnimType.ArtConfig.XDrawOffset +
                   (IsBuildingAnim ? BuildingAnimDrawConfig.X : 0);
        }

        // Turret anims have their facing frames reversed
        // Turret anims also only have 32 facings
        // Game uses lookup table containing frame indices (frame index 28 at array index 0,
        // decrementing and wrapping around to 31 after 0) to determine the correct frame from
        // direction normalized to range 0-31, the formula is replicated here without the LUT - Starkku
        private int GetTurretAnimFrameIndex() => (28 - Facing / 8 + 32) % 32;

        public override int GetFrameIndex(int frameCount)
        {
            if (IsTurretAnim)
                return GetTurretAnimFrameIndex();

            if (IsBuildingAnim && ParentBuilding != null)
            {
                if (frameCount > 1 && ParentBuilding.HP < Constants.ConditionYellowHP)
                    return frameCount / 4;
            }

            return 0;
        }

        public override int GetShadowFrameIndex(int frameCount)
        {
            if (IsTurretAnim)
                return GetTurretAnimFrameIndex() + frameCount / 2;

            if (IsBuildingAnim && ParentBuilding != null)
            {
                if (ParentBuilding.HP < Constants.ConditionYellowHP)
                    return frameCount / 4 * 3;
            }

            return frameCount / 2;
        }

        public override bool Remapable() => IsBuildingAnim;
        public override Color GetRemapColor() => Remapable() ? Owner.XNAColor : Color.White;
    }

    public struct BuildingAnimDrawConfig
    {
        private const int ZAdjustMult = 5; // Random value to make z-sorting work
        public int Y { get; set; }
        public int X { get; set; }
        public int YSort { get; set; }
        public int ZAdjust { get; set; }
        public readonly int SortValue => YSort - ZAdjust * ZAdjustMult;
    }
}
