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

using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FitsRatingTool.GuiApp.Models
{
    public struct Point
    {
        public double x, y;

        public Point(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public static Point operator +(Point p1, Point p2)
        {
            return new Point() { x = p1.x + p2.x, y = p1.y + p2.y };
        }

        public static Point operator -(Point p1, Point p2)
        {
            return new Point() { x = p1.x - p2.x, y = p1.y - p2.y };
        }

        public double Length()
        {
            return Math.Sqrt(x * x + y * y);
        }
    }

    public class ThinPlateSpline
    {
        public class Solution
        {
            private List<Point> parameterPoints;
            private double[] parameterWeights;

            public Solution(List<Point> parameterPoints, double[] parameterWeights)
            {
                this.parameterPoints = parameterPoints;
                this.parameterWeights = parameterWeights;
            }

            public double Interpolate(Point point)
            {
                int np = parameterWeights.Length - 3;

                double a1 = parameterWeights[np + 0];
                double a2 = parameterWeights[np + 1];
                double a3 = parameterWeights[np + 2];

                double v = a1 + a2 * point.x + a3 * point.y;

                for (int i = 0; i < np; ++i)
                {
                    v += parameterWeights[i] * Kernel((point - parameterPoints[i]).Length());
                }

                return v;
            }
        }

        public float Regularization { get; set; }

        public static double Kernel(double d)
        {
            return d <= 0.0f ? 0.0f : d * d * Math.Log(d);
        }

        public bool Solve(List<Point> points, List<double> values, [NotNullWhen(true)] out Solution? solution, List<double>? dynamicRegularization = null)
        {
            solution = null;

            if (points.Count != values.Count || (dynamicRegularization != null && dynamicRegularization.Count != values.Count) || points.Count < 3)
            {
                return false;
            }

            int n = points.Count;

            double[,] A = new double[(n + 3), (n + 3)];
            double[] b = new double[n + 3];

            double a = 0.0;

            for (int i = 0; i < n; ++i)
            {
                for (int j = i + 1; j < n; ++j)
                {
                    Point pi = points[i];
                    Point pj = points[j];

                    double d = (pi - pj).Length();

                    A[i, j] = A[j, i] = Kernel(d);

                    a += d * 2.0;
                }
            }

            a /= n * n;

            double a2 = a * a;

            for (int i = 0; i < n; ++i)
            {
                double r = Regularization + (dynamicRegularization != null ? dynamicRegularization[i] : 0.0);
                A[i, i] = r * a2;

                Point p = points[i];

                A[i, n + 0] = A[n + 0, i] = 1.0;
                A[i, n + 1] = A[n + 1, i] = p.x;
                A[i, n + 2] = A[n + 2, i] = p.y;
            }

            for (int i = 0; i < n; ++i)
            {
                b[i] = values[i];
            }

            var x = DenseMatrix.OfArray(A).Solve(DenseVector.OfArray(b));

            solution = new Solution(new List<Point>(points), x.AsArray());

            return true;
        }
    }
}
