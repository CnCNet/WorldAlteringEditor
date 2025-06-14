namespace TSMapEditor.Rendering.ObjectRenderers
{
    public readonly ref struct CommonDrawParams
    {
        public string IniName { get; init; }
        public ShapeImage ShapeImage { get; init; }
        public VoxelModel MainVoxel { get; init; }
        public VoxelModel TurretVoxel { get; init; }
        public VoxelModel BarrelVoxel { get; init; }
    }
}
