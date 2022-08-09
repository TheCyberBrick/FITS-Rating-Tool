/*
    FITS Rating Tool
    Copyright (C) 2022 TheCyberBrick
    
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.
    
    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using FitsRatingTool.GuiApp.UI.FitsImage;

namespace FitsRatingTool.GuiApp.Services.Impl
{
    public class StarSampler : IStarSampler
    {
        private static void GetCenter(IFitsImagePhotometryViewModel p, out int x, out int y)
        {
            x = (int)((p.XMax + p.XMin) * 0.5);
            y = (int)((p.YMax + p.YMin) * 0.5);
        }

        public List<IFitsImagePhotometryViewModel> Sample(IEnumerable<IFitsImagePhotometryViewModel> photometry, int desiredMaxSamples, out bool isSubset)
        {
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            int n = 0;

            foreach (var p in photometry)
            {
                GetCenter(p, out var x, out var y);
                minX = Math.Min(minX, x);
                maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y);
                maxY = Math.Max(maxY, y);
                ++n;
            }

            if (n <= desiredMaxSamples)
            {
                isSubset = false;
                return new(photometry);
            }

            double width = maxX - minX;
            double height = maxY - minY;
            double area = width * height;

            double density = n / area;
            double desiredDensity = desiredMaxSamples / area;

            // Estimated number of samples along each axis
            double estimatedSamplesY = Math.Sqrt(n * height / width);
            double estimatedSamplesX = estimatedSamplesY * width / height;

            // Make an initial optimistic guess for the grid size based on the actual density and desired density
            double estimatedGridSize = Math.Min(width, height) / Math.Min(estimatedSamplesX, estimatedSamplesY);
            double gridSize = estimatedGridSize * Math.Sqrt(density / desiredDensity);

            List<IFitsImagePhotometryViewModel> items = new();

            double prevGridSize = 0;

            for (int i = 0; i < 5; ++i)
            {
                int cellsX = (int)Math.Ceiling(width / gridSize);
                int cellsY = (int)Math.Ceiling(height / gridSize);

                cellsX = Math.Min(cellsX, 20);
                cellsY = Math.Min(cellsY, 20);

                gridSize = Math.Max(width / cellsX, height / cellsY);

                // Exit if things are not changing much anymore
                if (Math.Abs(prevGridSize - gridSize) < 5)
                {
                    break;
                }

                prevGridSize = gridSize;

                items.Clear();

                // Assign at most one item to each cells
                for (int cy = 0; cy < cellsY; ++cy)
                {
                    for (int cx = 0; cx < cellsX; ++cx)
                    {
                        double cellMinX = cx * gridSize;
                        double cellMinY = cy * gridSize;
                        double cellMaxX = cellMinX + gridSize;
                        double cellMaxY = cellMinY + gridSize;

                        foreach (var p in photometry)
                        {
                            GetCenter(p, out var x, out var y);

                            if (x >= cellMinX && x < cellMaxX && y >= cellMinY && y < cellMaxY)
                            {
                                items.Add(p);
                                break;
                            }
                        }
                    }
                }

                int count = items.Count;

                // Exit if close enough
                if (Math.Abs(desiredMaxSamples - count) < 10)
                {
                    break;
                }

                // Estimate new grid size
                gridSize = gridSize * count / desiredMaxSamples;
            }

            int excess = items.Count - desiredMaxSamples;

            if (excess > 0)
            {
                // If there is excess, just uniformly remove items
                // until there is no more excess. The list is already
                // ordered by cells, so this should result in relatively
                // evenly spaced gaps.

                int step = (int)Math.Floor(items.Count / (float)excess);

                for (int i = items.Count - 1; i > 0 && excess > 0; i -= step)
                {
                    items.RemoveAt(i);
                    --excess;
                }
            }

            isSubset = items.Count < n;

            return items;
        }
    }
}
