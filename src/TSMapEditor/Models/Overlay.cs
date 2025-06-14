using TSMapEditor.Models.Enums;

namespace TSMapEditor.Models
{
    public class Overlay : GameObject
    {
        public override GameObjectType GetObjectType() => OverlayType;

        public override RTTIType WhatAmI() => RTTIType.Overlay;

        public OverlayType OverlayType { get; set; }
        public int FrameIndex { get; set; }


        public override int GetFrameIndex(int frameCount)
        {
            return FrameIndex;
        }

        public override int GetShadowFrameIndex(int frameCount)
        {
            return frameCount / 2 + FrameIndex;
        }

        public override bool HasShadow() => true;

        public override bool IsInvisibleInGame() => OverlayType.InvisibleInGame;

        public override int GetYDrawOffset()
        {
            // Vanilla draws Veinhole monsters separately, not as overlays, and with an offset I can't make sense of.
            if (OverlayType.IsVeinholeMonster)
                return Constants.IsRA2YR ? -58 : -49;

            int offset = 0;

            // These are hardcoded and the same between TS and YR, so seemingly unrelated to the cell size.
            if (OverlayType.Tiberium || OverlayType.Wall || OverlayType.IsVeins || OverlayType.Crate)
                offset -= 12;

            if (OverlayType.Land == LandType.Railroad)
                offset -= 1;

            if (OverlayType.IsVeins)
                offset -= 1;

            return offset;
        }
    }
}
