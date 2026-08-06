using MapEditorLibrary.Models;

namespace MapEditorLibrary.Graphics
{
    public class GraphicalBaseNode
    {
        public GraphicalBaseNode(BaseNode baseNode, BuildingType buildingType, House owner)
        {
            BaseNode = baseNode;
            BuildingType = buildingType;
            Owner = owner;
        }

        public BaseNode BaseNode { get; }
        public BuildingType BuildingType { get; set; }
        // public Structure Structure { get; set; }
        public House Owner { get; set; }
    }
}
