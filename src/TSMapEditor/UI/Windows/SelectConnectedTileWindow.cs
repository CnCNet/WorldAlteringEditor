using MapEditorLibrary.Models;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Linq;

namespace TSMapEditor.UI.Windows;

public class SelectConnectedTileWindow : SelectObjectWindow<ConnectedTileType>
{
    public SelectConnectedTileWindow(WindowManager windowManager, Map map) : base(windowManager)
    {
        this.map = map;
    }

    private readonly Map map;

    public override void Initialize()
    {
        Name = nameof(SelectConnectedTileWindow);
        base.Initialize();
    }

    protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (lbObjectList.SelectedItem == null)
        {
            SelectedObject = null;
            return;
        }

        SelectedObject = (ConnectedTileType)lbObjectList.SelectedItem.Tag;
    }

    public void Open()
    {
        Open(null);
    }

    protected override void ListObjects()
    {
        lbObjectList.Clear();

        foreach (ConnectedTileType connectedTileType in map.EditorConfig.ConnectedTileTypes.Where(ctt =>
                     ctt.AllowedTheaters.Exists(theaterName => theaterName.Equals(map.TheaterName, StringComparison.OrdinalIgnoreCase))))
        {
            if (!connectedTileType.IsLegal)
                continue;

            lbObjectList.AddItem(new XNAListBoxItem()
            {
                Text = connectedTileType.Name,
                Tag = connectedTileType,
                TextColor = connectedTileType.Color.GetValueOrDefault(lbObjectList.DefaultItemColor)
            });
        }
    }
}
