﻿using System;
using System.Collections.Generic;
using TSMapEditor.GameMath;
using TSMapEditor.Models;
using TSMapEditor.Models.Enums;
using TSMapEditor.Rendering;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation for creating low bridges.
    /// </summary>
    public class PlaceBridgeMutation : Mutation
    {
        public PlaceBridgeMutation(IMutationTarget mutationTarget, Point2D point1, Point2D point2, Bridge bridge) : base(mutationTarget)
        {
            if (point1.X != point2.X && point1.Y != point2.Y)
            {
                throw new ArgumentException(nameof(PlaceBridgeMutation) + 
                    ": either the X or Y coordinate must be identical between bridge start and end points.");
            }

            if (point1 == point2)
                throw new ArgumentException(nameof(PlaceBridgeMutation) + ": bridge start and end points cannot point to the same cell.");

            if (point1.X == point2.X)
            {
                bridgeDirection = BridgeDirection.NorthSouth;

                if (point1.Y > point2.Y)
                {
                    startPoint = point2;
                    endPoint = point1;
                }
                else
                {
                    startPoint = point1;
                    endPoint = point2;
                }
            }
            else
            {
                bridgeDirection = BridgeDirection.EastWest;

                if (point1.X > point2.X)
                {
                    startPoint = point2;
                    endPoint = point1;
                }
                else
                {
                    startPoint = point1;
                    endPoint = point2;
                }
            }

            this.bridge = bridge;
        }

        private readonly Point2D startPoint;
        private readonly Point2D endPoint;

        private readonly BridgeDirection bridgeDirection;
        private readonly Bridge bridge;


        private List<OriginalOverlayInfo> originalOverlayInfos = new List<OriginalOverlayInfo>();

        public override void Perform()
        {
            if (bridge.Type == BridgeType.Low)
            {
                if (bridgeDirection == BridgeDirection.EastWest)
                {
                    PlaceLowBridge(bridge.EastWest.Start, bridge.EastWest.End, bridge.EastWest.Pieces,
                        startPoint.X, endPoint.X, PlaceEastWestDirectionLowBridgePiece);
                }
                else
                {
                    PlaceLowBridge(bridge.NorthSouth.Start, bridge.NorthSouth.End, bridge.NorthSouth.Pieces,
                        startPoint.Y, endPoint.Y, PlaceNorthSouthDirectionLowBridgePiece);
                }
            }
            else if (bridge.Type == BridgeType.High)
            {
                if (bridgeDirection == BridgeDirection.EastWest)
                {
                    PlaceHighBridge(bridge.EastWest.Pieces[0], startPoint.X, endPoint.X, PlaceEastWestDirectionHighBridgePiece);
                }
                else
                {
                    PlaceHighBridge(bridge.NorthSouth.Pieces[0], startPoint.Y, endPoint.Y, PlaceNorthSouthDirectionHighBridgePiece);
                }
            }

            MutationTarget.InvalidateMap();
        }

        private void PlaceLowBridge(int bridgeStartOverlayIndex, int bridgeEndOverlayIndex, List<int> bridgePieces, int beginCoord, int endCoord, Action<int, int> piecePlacementFunction)
        {
            piecePlacementFunction(bridgeStartOverlayIndex, beginCoord);

            for (int c = beginCoord + 1; c < endCoord; c++)
            {
                int overlayIndex = bridgePieces[MutationTarget.Randomizer.GetRandomNumber(0, bridgePieces.Count - 1)];
                piecePlacementFunction(overlayIndex, c);
            }

            piecePlacementFunction(bridgeEndOverlayIndex, endCoord);
        }

        private void PlaceEastWestDirectionLowBridgePiece(int overlayIndex, int x)
        {
            PlaceLowBridgePiece(overlayIndex, x,
                (fixedCoord, variableCoord) => new Point2D(fixedCoord, startPoint.Y + variableCoord));
        }

        private void PlaceNorthSouthDirectionLowBridgePiece(int overlayIndex, int y)
        {
            PlaceLowBridgePiece(overlayIndex, y, 
                (fixedCoord, variableCoord) => new Point2D(startPoint.X + variableCoord, fixedCoord));
        }

        private void PlaceLowBridgePiece(int overlayIndex, int fixedCoordinate, Func<int, int, Point2D> coordGenerator)
        {
            for (int variableCoordinateOffset = -1; variableCoordinateOffset <= 1; variableCoordinateOffset++)
            {
                var cellCoords = coordGenerator(fixedCoordinate, variableCoordinateOffset);
                var mapCell = MutationTarget.Map.GetTile(cellCoords);

                if (mapCell == null)
                    continue;

                if (mapCell.Overlay == null || mapCell.Overlay.OverlayType == null)
                    originalOverlayInfos.Add(new OriginalOverlayInfo(-1, -1, cellCoords));
                else
                    originalOverlayInfos.Add(new OriginalOverlayInfo(mapCell.Overlay.OverlayType.Index, mapCell.Overlay.FrameIndex, cellCoords));

                mapCell.Overlay = new Overlay()
                {
                    OverlayType = MutationTarget.Map.Rules.OverlayTypes[overlayIndex],
                    FrameIndex = variableCoordinateOffset + 1,
                    Position = cellCoords
                };
            }
        }
        private void PlaceHighBridge(int bridgePiece, int beginCoord, int endCoord, Action<int, int, int> piecePlacementFunction)
        {
            var startCell = MutationTarget.Map.GetTile(startPoint);
            if (startCell == null)
                return;

            for (int c = beginCoord; c <= endCoord; c++)
            {
                piecePlacementFunction(bridgePiece, c, startCell.Level);
            }
        }

        private void PlaceEastWestDirectionHighBridgePiece(int overlayIndex, int x, int startingHeight)
        {
            const int xStartFrame = 0;
            const int xEndFrame = 3;
            int frameIndex = MutationTarget.Randomizer.GetRandomNumber(xStartFrame, xEndFrame);

            PlaceHighBridgePiece(overlayIndex, frameIndex, new Point2D(x, startPoint.Y), startingHeight);
        }

        private void PlaceNorthSouthDirectionHighBridgePiece(int overlayIndex, int y, int startingHeight)
        {
            const int yStartFrame = 9;
            const int yEndFrame = 12;
            int frameIndex = MutationTarget.Randomizer.GetRandomNumber(yStartFrame, yEndFrame);

            PlaceHighBridgePiece(overlayIndex, frameIndex, new Point2D(startPoint.X, y), startingHeight);
        }

        private void PlaceHighBridgePiece(int overlayIndex, int frameIndex, Point2D cellCoords, int startingHeight)
        {
            var mapCell = MutationTarget.Map.GetTile(cellCoords);

            if (mapCell == null)
                return;

            if (MutationTarget.Map.TheaterInstance.Theater.TryGetTileSetById(bridge.TileSetIndex).ContainsTile(mapCell.TileIndex) && mapCell.Level == startingHeight)
                return;

            if (mapCell.Overlay == null || mapCell.Overlay.OverlayType == null)
                originalOverlayInfos.Add(new OriginalOverlayInfo(-1, -1, cellCoords));
            else
                originalOverlayInfos.Add(new OriginalOverlayInfo(mapCell.Overlay.OverlayType.Index, mapCell.Overlay.FrameIndex, cellCoords));

            mapCell.Overlay = new Overlay()
            {
                OverlayType = MutationTarget.Map.Rules.OverlayTypes[overlayIndex],
                FrameIndex = frameIndex,
                Position = cellCoords
            };
        }

        public override void Undo()
        {
            foreach (var originalOverlayInfo in originalOverlayInfos)
            {
                var mapCell = MutationTarget.Map.GetTile(originalOverlayInfo.CellCoords);

                if (originalOverlayInfo.OverlayTypeIndex < 0)
                {
                    mapCell.Overlay = null;
                }
                else
                {
                    mapCell.Overlay = new Overlay()
                    {
                        OverlayType = MutationTarget.Map.Rules.OverlayTypes[originalOverlayInfo.OverlayTypeIndex],
                        FrameIndex = originalOverlayInfo.FrameIndex,
                        Position = originalOverlayInfo.CellCoords
                    };
                }
            }

            MutationTarget.InvalidateMap();
        }
    }
}
