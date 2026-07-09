using System;
using System.Collections.Generic;

namespace TSMapEditor.Initialization
{
    /// <summary>
    /// A from-scratch, minimal implementation of the "simple tiled model" variant of the
    /// Wave Function Collapse algorithm (see https://github.com/mxgmn/WaveFunctionCollapse,
    /// MIT licensed - no code from that repository is reused here, only the published
    /// algorithm description: observe the lowest-entropy cell, collapse it to a single
    /// domain value, then propagate the resulting adjacency constraints outward until
    /// either every cell is collapsed or a contradiction (an empty possibility set) is hit).
    ///
    /// This operates over an abstract rectangular grid of "cells", each of which can take on
    /// one of <c>domainCount</c> possible integer domain values, constrained by a symmetric
    /// pairwise compatibility matrix and 4-directional (N/S/E/W) adjacency. It knows nothing
    /// about tiles, maps, or WAE-specific types - see <see cref="HeadlessMapGenerator"/> for
    /// how it's used to decide which <c>TerrainGeneratorTileGroup</c> governs each block of
    /// map cells.
    /// </summary>
    internal sealed class WaveFunctionCollapseSolver
    {
        private readonly int width;
        private readonly int height;
        private readonly int domainCount;
        private readonly double[] weights;
        private readonly bool[,] compatibility;
        private readonly bool[][] possibilities;
        private readonly int[] remainingCount;

        public WaveFunctionCollapseSolver(int width, int height, double[] weights, bool[,] compatibility)
        {
            this.width = width;
            this.height = height;
            this.weights = weights;
            this.compatibility = compatibility;
            domainCount = weights.Length;

            int cellCount = width * height;
            possibilities = new bool[cellCount][];
            remainingCount = new int[cellCount];

            for (int i = 0; i < cellCount; i++)
            {
                possibilities[i] = new bool[domainCount];
                for (int d = 0; d < domainCount; d++)
                    possibilities[i][d] = true;

                remainingCount[i] = domainCount;
            }
        }

        /// <summary>
        /// Runs the observation/propagation loop to completion.
        /// Returns an array of domain indices (one per cell, row-major) on success,
        /// or null if the solver ran into a contradiction (no valid assignment found
        /// for some cell given the constraints).
        /// </summary>
        public int[] Solve(Random random)
        {
            while (true)
            {
                int cellIndex = FindLowestEntropyCell(random);
                if (cellIndex < 0)
                    break; // every cell is fully collapsed - success

                if (!CollapseCell(cellIndex, random))
                    return null; // a cell had zero remaining possibilities - contradiction

                if (!Propagate(cellIndex))
                    return null; // propagation emptied some cell's possibility set - contradiction
            }

            var result = new int[width * height];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Array.IndexOf(possibilities[i], true);
            }

            return result;
        }

        private int FindLowestEntropyCell(Random random)
        {
            int bestIndex = -1;
            double bestEntropy = double.MaxValue;

            for (int i = 0; i < possibilities.Length; i++)
            {
                if (remainingCount[i] <= 1)
                    continue; // already collapsed (or contradictory - handled by caller)

                double sumWeights = 0;
                double sumWeightLogWeight = 0;
                for (int d = 0; d < domainCount; d++)
                {
                    if (!possibilities[i][d])
                        continue;

                    double w = weights[d];
                    sumWeights += w;
                    sumWeightLogWeight += w * Math.Log(w);
                }

                if (sumWeights <= 0)
                    continue;

                // Shannon entropy of the weighted distribution, plus a small amount of
                // random jitter so ties between equal-entropy cells are broken pseudo-randomly
                // rather than always picking the same (e.g. top-left-most) cell.
                double entropy = Math.Log(sumWeights) - (sumWeightLogWeight / sumWeights);
                entropy += random.NextDouble() * 1e-6;

                if (entropy < bestEntropy)
                {
                    bestEntropy = entropy;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private bool CollapseCell(int cellIndex, Random random)
        {
            double totalWeight = 0;
            for (int d = 0; d < domainCount; d++)
            {
                if (possibilities[cellIndex][d])
                    totalWeight += weights[d];
            }

            if (totalWeight <= 0)
                return false;

            double roll = random.NextDouble() * totalWeight;
            int chosen = -1;
            for (int d = 0; d < domainCount; d++)
            {
                if (!possibilities[cellIndex][d])
                    continue;

                roll -= weights[d];
                if (roll <= 0)
                {
                    chosen = d;
                    break;
                }
            }

            if (chosen < 0)
            {
                // Floating point edge case - fall back to the last possible domain.
                for (int d = domainCount - 1; d >= 0; d--)
                {
                    if (possibilities[cellIndex][d])
                    {
                        chosen = d;
                        break;
                    }
                }
            }

            if (chosen < 0)
                return false;

            for (int d = 0; d < domainCount; d++)
                possibilities[cellIndex][d] = d == chosen;

            remainingCount[cellIndex] = 1;
            return true;
        }

        /// <summary>
        /// Breadth-first constraint propagation: whenever a cell's possibility set shrinks,
        /// each of its 4-directional neighbors has any domain removed that isn't compatible
        /// with at least one of the remaining possibilities of the cell that just changed.
        /// </summary>
        private bool Propagate(int startCellIndex)
        {
            var queue = new Queue<int>();
            queue.Enqueue(startCellIndex);

            while (queue.Count > 0)
            {
                int cellIndex = queue.Dequeue();
                int x = cellIndex % width;
                int y = cellIndex / width;

                foreach (var (nx, ny) in GetNeighbors(x, y))
                {
                    int neighborIndex = ny * width + nx;
                    if (remainingCount[neighborIndex] <= 1)
                        continue; // already collapsed - nothing to constrain further

                    bool changed = false;

                    for (int nd = 0; nd < domainCount; nd++)
                    {
                        if (!possibilities[neighborIndex][nd])
                            continue;

                        bool stillAllowed = false;
                        for (int d = 0; d < domainCount; d++)
                        {
                            if (possibilities[cellIndex][d] && compatibility[d, nd])
                            {
                                stillAllowed = true;
                                break;
                            }
                        }

                        if (!stillAllowed)
                        {
                            possibilities[neighborIndex][nd] = false;
                            remainingCount[neighborIndex]--;
                            changed = true;
                        }
                    }

                    if (remainingCount[neighborIndex] <= 0)
                        return false; // contradiction

                    if (changed)
                        queue.Enqueue(neighborIndex);
                }
            }

            return true;
        }

        private IEnumerable<(int x, int y)> GetNeighbors(int x, int y)
        {
            if (x > 0) yield return (x - 1, y);
            if (x < width - 1) yield return (x + 1, y);
            if (y > 0) yield return (x, y - 1);
            if (y < height - 1) yield return (x, y + 1);
        }
    }
}
