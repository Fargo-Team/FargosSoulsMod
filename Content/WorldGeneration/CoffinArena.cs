using FargowiltasSouls.Content.Tiles;
using FargowiltasSouls.Core.Systems;
using Microsoft.Xna.Framework;
using StructureHelper;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargowiltasSouls.Content.WorldGeneration
{
    public static class CoffinArena
    {
        public static int Width = 60; // Width of internal room
        public static int Height = 35; // Height of internal room

        public static int VectorWidth => Width * 16;
        public static int VectorHeight => Height * 16;

        public static Point16 StructureSize = Point16.Zero;
        private static int PaddedWidth => StructureSize.X + 2;
        public static Point Center => WorldSavingSystem.CoffinArenaCenter;
        public static Vector2 FightCenter => Center.ToWorldCoordinates() + Vector2.UnitX * 8;
        public static Rectangle Rectangle = new();
        public static Rectangle PaddedRectangle => Rectangle.Modified(-2, -2, 4, 4);

        public static bool TileIsPyramid(Tile tile) => (tile.TileType == TileID.SandstoneBrick || tile.WallType == WallID.SandstoneBrick);

        public static bool CheckSpot(int x, int y)
        {
            for (int xOff = -PaddedWidth / 2; xOff < PaddedWidth / 2; xOff++)
            {
                for (int yOff = 0; yOff < StructureSize.Y; yOff++)
                {
                    Tile tile = Main.tile[x + xOff, y + yOff];
                    if (tile.WallType == WallID.SandstoneBrick && !tile.HasTile) // invalid position if hallway
                        return false;
                }
            }
            return true;
        }

        public static void Generate(Point pyramidTop)
        {
            string arenaPath = "Content/WorldGeneration/CoffinArena";
            Mod mod = ModLoader.GetMod("FargowiltasSouls");
            if (StructureSize == Point16.Zero)
                if (!Generator.GetDimensions(arenaPath, mod, ref StructureSize))
                    throw new Exception("Fargo's Souls: Cursed Coffin arena generation could not retrieve dimensions for the arena.");

            // Find bottom of pyramid
            int curYSearch = 0;
            while (++curYSearch < 300) // max bound
            {
                int xSearchWidth = curYSearch - (PaddedWidth / 2);
                if (xSearchWidth < 0) // too thin
                    continue;
                for (int curXSearch = -xSearchWidth; curXSearch <= xSearchWidth; curXSearch++)
                {
                    bool validSpot = CheckSpot(pyramidTop.X + curXSearch, pyramidTop.Y + curYSearch);
                    if (!validSpot) 
                        continue;
                    Point arenaTopCenter = new(pyramidTop.X + curXSearch, pyramidTop.Y + curYSearch);
                    ExtendPyramid(pyramidTop, arenaTopCenter.Y + StructureSize.Y + 4);
                    Place(arenaTopCenter);
                    PlacePassage(arenaTopCenter);
                    return;
                }
            }
        }
        public static void ExtendPyramid(Point pyramidTop, int yToExtendTo)
        {
            for (int y = pyramidTop.Y; y <= yToExtendTo; y++)
            {
                int i = y - pyramidTop.Y;
                if (i < 30)
                    continue;
                int halfWidth = i + 1;
                for (int x = pyramidTop.X - halfWidth - 2; x <= pyramidTop.X + halfWidth; x++)
                {
                    Tile tile = Main.tile[x, y];
                    if (!TileIsPyramid(tile))
                    {
                        WorldGen.KillTile(x, y);
                        WorldGen.KillWall(x, y);
                        WorldGen.PlaceTile(x, y, TileID.SandstoneBrick, mute: true, forced: true);
                    }
                }
            }
        }
        public static int RandomSandstoneBrick() => (WorldGen.generatingWorld ? WorldGen.genRand.NextFloat() : Main.rand.NextFloat()) > 0.7f ? ModContent.TileType<CrackedSandstoneBricks>() : TileID.SandstoneBrick;
        public static void Place(Point arenaTopCenter)
        {
            string arenaPath = "Content/WorldGeneration/CoffinArena";
            Mod mod = ModLoader.GetMod("FargowiltasSouls");
            if (StructureSize == Point16.Zero)
                if (!Generator.GetDimensions(arenaPath, mod, ref StructureSize))
                    throw new Exception("Fargo's Souls: Cursed Coffin arena generation could not retrieve dimensions for the arena.");
            Point16 arenaTopLeft = new(arenaTopCenter.X - (StructureSize.X / 2) + 1, arenaTopCenter.Y);
            StructureHelper.Generator.GenerateStructure(arenaPath, arenaTopLeft, mod);
            SetArenaPosition(new(arenaTopCenter.X, arenaTopCenter.Y + Height / 2));

        }
        public static void SetArenaPosition(Point arenaCenter)
        {
            WorldSavingSystem.CoffinArenaCenter = arenaCenter;
            Rectangle = new(arenaCenter.X - Width / 2, arenaCenter.Y - Height / 2, Width, Height);
            string command = "AddIndestructibleRectangle";
            FargowiltasSouls.MutantMod.Call(command, PaddedRectangle.ToWorldCoords());
        }

        public static void PlacePassage(Point arenaTopCenter) // Separated because the manual item doesn't do this
        {
            int arenaLeft = arenaTopCenter.X - (Width / 2);
            int arenaRight = arenaTopCenter.X + (Width / 2);
            int arenaBottom = arenaTopCenter.Y + Height - 2;
            int dir = 0;
            // Search for passage, horizontally
            for (int xOff = 1; xOff < 150; xOff++) // Start at 2 because otherwise it detects the wall inside the arena and breaks
            {
                Tile left = Main.tile[arenaLeft - xOff,  arenaBottom];
                Tile right = Main.tile[arenaRight + xOff, arenaBottom];

                if (!left.HasTile && left.WallType == WallID.SandstoneBrick) // Search left for passage
                    dir = -1;
                if (!right.HasTile && right.WallType == WallID.SandstoneBrick) // Search right for passage
                    dir = 1;
                if (dir != 0)
                    break;
            }
            if (dir == 0)
                dir = Math.Sign(PyramidGenSystem.PyramidLocation.X - arenaTopCenter.X);
            for (int pY = -1; pY < 5; pY++)
            {
                for (int pX = -1; pX < 150; pX++) // Start at 1 to drill the wall and not stop inside the arena
                {
                    int extraX = dir == 1 ? 1 : 0; // Needed because it's uncentered! Width is even.

                    Point point = new(arenaTopCenter.X + dir * ((Width / 2) + pX + extraX), arenaBottom - pY);
                    Tile tile = Main.tile[point.X, point.Y];
                    if (!tile.HasTile && tile.WallType == WallID.SandstoneBrick)
                        break;
                    WorldGen.KillTile(point.X, point.Y);
                    WorldGen.KillWall(point.X, point.Y);
                    if (pY == -1 || pY == 4)
                        WorldGen.PlaceTile(point.X, point.Y, TileID.SandstoneBrick, mute: true, forced: true);
                    else
                        WorldGen.PlaceWall(point.X, point.Y, WallID.SandstoneBrick, mute: true);
                }
            }
        }
        public static void PlaceOpening(Point arenaTopCenter) // Manual item only
        {
            int arenaLeft = arenaTopCenter.X - (Width / 2);
            int arenaRight = arenaTopCenter.X + (Width / 2);
            int arenaBottom = arenaTopCenter.Y + Height - 2;
            int dir = 0;
            // Search for passage, horizontally
            for (int xOff = 1; xOff < 150; xOff++) // Start at 2 because otherwise it detects the wall inside the arena and breaks
            {
                Tile left = Main.tile[arenaLeft - xOff, arenaBottom];
                Tile right = Main.tile[arenaRight + xOff, arenaBottom];

                if (!left.HasTile && left.WallType == WallID.SandstoneBrick) // Search left for passage
                    dir = -1;
                if (!right.HasTile && right.WallType == WallID.SandstoneBrick) // Search right for passage
                    dir = 1;
                if (dir != 0)
                    break;
            }
            if (dir == 0)
                dir = Math.Sign(PyramidGenSystem.PyramidLocation.X - arenaTopCenter.X);
            for (int pY = -1; pY < 5; pY++)
            {
                for (int pX = -1; pX < 150; pX++) // Start at 1 to drill the wall and not stop inside the arena
                {
                    int extraX = dir == 1 ? 1 : 0; // Needed because it's uncentered! Width is even.

                    Point point = new(arenaTopCenter.X + dir * ((Width / 2) + pX + extraX), arenaBottom - pY);
                    Tile tile = Main.tile[point.X, point.Y];
                    if (!tile.HasTile || tile.TileType != TileID.SandstoneBrick)
                        break;
                    WorldGen.KillTile(point.X, point.Y);
                    //WorldGen.KillWall(point.X, point.Y);
                }
            }
        }

        public static Vector2 ClampWithinArena(Vector2 vector, Entity entityToPadBasedOn)
        {
            Vector2 center = FightCenter;
            float xBound = (Width * 8) - entityToPadBasedOn.width * 0.6f;
            float yBound = (Height * 8) - entityToPadBasedOn.height * 0.6f;
            vector.X = Math.Clamp(vector.X, center.X - xBound, center.X + xBound);
            vector.Y = Math.Clamp(vector.Y, center.Y - yBound, center.Y + yBound);
            return vector;
        }
        public static List<Vector2> ArenaCorners(Entity entityToPadBasedOn)
        {
            Vector2 center = FightCenter;
            Vector2 xBound = Vector2.UnitX * ((Width * 8) - entityToPadBasedOn.width * 0.6f);
            Vector2 yBound = Vector2.UnitY * ((Height * 8) - entityToPadBasedOn.height * 0.6f);
            return [center - xBound - yBound, center - xBound + yBound, center + xBound - yBound, center + xBound + yBound];
        }
        public static List<Vector2> TopArenaCorners(Entity entityToPadBasedOn)
        {
            Vector2 center = FightCenter;
            Vector2 xBound = Vector2.UnitX * ((Width * 8) - entityToPadBasedOn.width * 0.6f);
            Vector2 yBound = Vector2.UnitY * ((Height * 8) - entityToPadBasedOn.height * 0.6f);
            return [center - xBound - yBound, center + xBound - yBound];
        }
    }
}
