# Mapping Instructions

Mod: Dawn of the Tiberium Age

## Isometric perspective

The visual perspective of a Tiberian Sun / Red Alert 2 game world is rotated to achieve an isometric perspective. What is a square area in logical map coordinates is actually a diamond for the user. Cells are twice as wide as they are tall, so it is expected that a circular area will look elliptic when looking at it in a screenshot.

## Map Shape and Valid Coordinates

Despite the isometric perspective, a TS/RA2 map appears rectangular in-game. In logical map coordinates, however, its valid cells form a diamond. The isometric perspective rotates this diamond into the rectangle seen by the player.

The map's width and height do not define independent valid ranges for the X and Y coordinates. Do not assume that a 100×100 map uses coordinates from (0, 0) through (99, 99). Some coordinate pairs inside those ranges are invalid, while some valid cells have coordinate values greater than the map's width or height.

Map dimensions can also be misleading when estimating the number of cells. Each unit of map height contains two logical rows to produce the isometric layout. A map with dimensions `width × height` therefore contains `2 × width × height` cells. For example, a 100×100 map contains 20,000 cells rather than 10,000.

When placing objects or requesting rectangular map regions, ensure that every required cell lies inside the valid diamond. A region's center can be valid while one or more of its corners are outside the map.

## LAT Terrain Placement

LAT is a system that smoothly connects basic ground terrain to other terrain.

Prefer "snake-like" LAT detail placement over large plots of the same LAT. Grass or dirt spots in nature tend to exist in a somewhat "chaotic", imperfect manner, and not as a simple circular or rectangular area.

For an example, let's denote clear space with a dash (-), and a LAT-terrain cell with X, each character representing one cell. For a LAT, the following often looks poor and artificial to the human eye looking at a map:

```
------
-XXXX-
-XXXX-
-XXXX-
-XXXX-
------
```

The following usually looks better and more natural:

```
--XXX-
--X-X-
-XX-X-
--XXX-
-X-X--
--XX--
```

## Detailing Areas

Aside from LATs, try to also use various other pieces when detailing large areas. Rocks, pebbles, trees, rough ground, debris, villages or cities, small closed lakes... there's usually a lot you can detail a map with. Of course, varying details by area also makes sense depending on user preferences - there could be a lush, thick forest spot in one area, and a desert in another part of the map. The first could feature lots of trees and grass, while the latter would use rocks as detailing. In general, unless requested by the user or fitting the setting, do not leave massive empty areas - even a 10x10 cell area of clear ground usually stands out in a bad way.

## Layouting

The Tiberian Sun and Red Alert 2 game engines and gameplay design don't work well with very tight bottlenecks. When designing layouts, ensure that each bottleneck has, at a minimum, a 3-cell row of passable ground at its tightest spot. More is generally preferred, though. Much past 10 cells it starts getting questionable whether something functions as a bottleneck anymore however.

Layouts are often planned with cliffs and shorelines. You can place these by invoking the Connected Tiles tool. Often other kinds of more complicated elements, like thick forests and cities, can also be used as "soft" layout elements because they obstruct movement of large armies.

## Connected Tile Facings

When placing connected tiles, consider their facing. For example, if you are creating a hill surrounded by cliffs, you need to consider whether to place front or back facing cliffs to give the illusion of the cliff being higher than the surrounding terrain. You can always ask the user, or use the MCP server's screen-cropping endpoint for visual verification.

## Placement Order

Prefer to design a layout first, then details. When detailing, place objects like buildings and trees first, then terrain. This is because if you are, for example, creating a city, it is easier to place dirt or pavement LAT under buildings and grass LAT under trees after they have been placed down, than it is to first place dirt/grass and then fit objects on top of them. 

## Asymmetry

While an RTS game, classic Command & Conquer maps, especially Tiberian Sun maps, were usually asymmetric. Do not treat symmetry and "perfect balance" as a requirement unless the user mentions wanting a symmetric map. Asymmetric layouts often look more beautiful and can create more varied gameplay situations, which is enjoyable especially to non-competitive players and in mission settings.

## Resource Placement

There are two types of resource fields in Command & Conquer games: regrowing and non-regrowing.

In Dawn of the Tiberium Age, regrowing fields contain a Ore Mine, Tiberium Tree (for Green Tiberium aka Riparius), or Vinifera Tree (for Blue Tiberium aka Vinifera), and a matching resource spreader on the same cell with the tree. Around the tree is resource overlay of the matching type depending on map design. Small fields are around 8 cells in diameter, while large fields can be double that. 

Never place overlay on the same cell where Tiberium Trees or Ore Mines exist.

A good baseline for economy is 2 Ore Mines or Tiberium Trees per player. Tight-money maps have less, while megawealth-style maps can have much more. Some tight-money maps only turn tight in the lategame due to featuring a lot of non-regrowing resources.

For a non-regrowing resource field, simply leave out the Tiberium Tree and respective resource spreader. These offer temporary economic boosts, forcing players to relocate and capture more of the map once a non-regrowing field has been harvested dry.

There are 5 types of resources. Ore, Scrap Metal, and Green Tiberium are all equal in value, 700 for a  full harvester load. Blue Tiberium is 1120, while Gems are 1680.

## Player Starting Waypoints

When making multiplayer maps, waypoints 0 to 7 denote player starting locations. If the map has less than 8 players, waypoints are simply left out: a 4-player map has waypoints 0, 1, 2 and 3.

Additional waypoints, with IDs greater than 7, can be used for various map triggers, like scripted unit spawns or ambient sounds.

In singleplayer missions, no waypoints have special meaning, aside from 99 which is typically the "home cell". Do not use waypoint 100 for anything.