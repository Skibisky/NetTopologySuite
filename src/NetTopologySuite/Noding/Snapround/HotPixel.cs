using System;
using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

//using NetTopologySuite.IO;

namespace NetTopologySuite.Noding.Snapround
{
    /// <summary>
    /// Implements a "hot pixel" as used in the Snap Rounding algorithm.
    /// A hot pixel is a square region centred
    /// on the rounded valud of the coordinate given,
    /// and of width equal to the size of the scale factor.
    /// It is a partially open region, which contains
    /// the interior of the tolerance square and
    /// the boundary
    /// <b>minus</b> the top and right segments.
    /// This ensures that every point of the space lies in a unique hot pixel.
    /// It also matches the rounding semantics for numbers.
    /// <para/>
    /// The hot pixel operations are all computed in the integer domain
    /// to avoid rounding problems.
    /// </summary>
    public class HotPixel
    {
        private const double Tolerance = 0.5d;

        private readonly Coordinate _ptHot;
        private readonly Coordinate _originalPt;

        private readonly double _scaleFactor;

        private double _minx;
        private double _maxx;
        private double _miny;
        private double _maxy;

        /*
         * The corners of the hot pixel, in the order:
         *  10
         *  23
         */
        private readonly Coordinate[] _corner = new Coordinate[4];

        private Envelope _safeEnv;

        /// <summary>
        /// Initializes a new instance of the <see cref="HotPixel"/> class.
        /// </summary>
        /// <param name="pt">The coordinate at the center of the hot pixel</param>
        /// <param name="scaleFactor">The scale factor determining the pixel size</param>
        /// <param name="li">THe intersector to use for testing intersection with line segments</param>
        [Obsolete]
        public HotPixel(Coordinate pt, double scaleFactor, LineIntersector li)
            :this(pt, scaleFactor)
        {}
        public HotPixel(Coordinate pt, double scaleFactor)
        {
            _originalPt = pt;
            _ptHot = pt;
            _scaleFactor = scaleFactor;

            if (scaleFactor <= 0d)
                throw new ArgumentException("Scale factor must be non-zero");

            if (scaleFactor != 1.0)
                _ptHot = ScaleRound(pt);

            // extreme values for pixel
            _minx = _ptHot.X - Tolerance;
            _maxx = _ptHot.X + Tolerance;
            _miny = _ptHot.Y - Tolerance;
            _maxy = _ptHot.Y + Tolerance;
        }

        /// <summary>
        /// Gets the coordinate this hot pixel is based at.
        /// </summary>
        public Coordinate Coordinate => _originalPt;

        /// <summary>
        /// Gets the scale factor for the precision grid for this pixel.
        /// </summary>
        public double ScaleFactor
        {
            get => _scaleFactor;
        }

        /// <summary>
        /// Gets the width of the hot pixel in the original coordinate system.
        /// </summary>
        public double Width
        {
            get => 1.0d / _scaleFactor;
        }


        private double ScaleRound(double val)
        {
            return Math.Round(val * _scaleFactor);
        }

        private Coordinate ScaleRound(Coordinate p)
        {
            return new Coordinate(ScaleRound(p.X), ScaleRound(p.Y));
        }

        /// <summary>
        /// Scale without rounding.
        /// This ensures intersections are checked against original
        /// linework.
        /// This is required to ensure that intersections are not missed
        /// because the segment is moved by snapping.
        /// </summary>
        private double Scale(double val)
        {
            return val * _scaleFactor;
        }

        [Obsolete("Moved to MCIndexPointSnapper")]
        private const double SafeEnvelopeExpansionFactor = 0.75d;

        /// <summary>
        /// Returns a "safe" envelope that is guaranteed to contain the hot pixel.
        /// The envelope returned will be larger than the exact envelope of the pixel.
        /// </summary>
        /// <returns>An envelope which contains the pixel</returns>
        [Obsolete("Moved to MCIndexPointSnapper")]
        public Envelope GetSafeEnvelope()
        {
            if (_safeEnv == null)
            {
                double safeTolerance = SafeEnvelopeExpansionFactor / _scaleFactor;
                _safeEnv = new Envelope(_originalPt.X - safeTolerance, _originalPt.X + safeTolerance,
                                       _originalPt.Y - safeTolerance, _originalPt.Y + safeTolerance);
            }
            return _safeEnv;
        }

        /// <summary>
        /// Tests whether the line segment (p0-p1)
        /// intersects this hot pixel.
        /// </summary>
        /// <param name="p0">The first coordinate of the line segment to test</param>
        /// <param name="p1">The second coordinate of the line segment to test</param>
        /// <returns>true if the line segment intersects this hot pixel.</returns>
        public bool Intersects(Coordinate p0, Coordinate p1)
        {
            if (_scaleFactor == 1.0)
                return IntersectsScaled(p0.X, p0.Y, p1.X, p1.Y);

            double sp0x = Scale(p0.X);
            double sp0y = Scale(p0.Y);
            double sp1x = Scale(p1.X);
            double sp1y = Scale(p1.Y);
            return IntersectsScaled(sp0x, sp0y, sp1x, sp1y);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <returns></returns>
        [Obsolete]
        public bool IntersectsScaled(Coordinate p0, Coordinate p1)
        {
            return IntersectsScaled(p0.X, p0.Y, p1.X, p1.Y);
        }

        private bool IntersectsScaled(double p0x, double p0y,
            double p1x, double p1y)
        {
            double px = p0x;
            double py = p0y;
            double qx = p1x;
            double qy = p1y;

            if (px > qx)
            {
                px = p1x;
                py = p1y;
                qx = p0x;
                qy = p0y;
            }
            /*
            * Report false if segment env does not intersect pixel env.
            * This check reflects the fact that the pixel Top and Right sides
            * are open (not part of the pixel).
            */
            // check Right side
            double segMinx = Math.Min(px, qx);
            if (segMinx >= _maxx) return false;
            // check Left side
            double segMaxx = Math.Max(px, qx);
            if (segMaxx < _minx) return false;
            // check Top side
            double segMiny = Math.Min(py, qy);
            if (segMiny >= _maxy) return false;
            // check Bottom side
            double segMaxy = Math.Max(py, qy);
            if (segMaxy < _miny) return false;

            /*
             * Vertical or horizontal segments must now intersect
             * the segment interior or Left or Bottom sides.
             */
            //---- check vertical segment
            if (px == qx)
            {
                return true;
            }
            //---- check horizontal segment
            if (py == qy)
            {
                return true;
            }

            /*
             * Now know segment is not horizontal or vertical.
             * 
             * Compute orientation WRT each pixel corner.
             * If corner orientation == 0, 
             * segment intersects the corner.  
             * From the corner and whether segment is heading up or down,
             * can determine intersection or not.
             * 
             * Otherwise, check whether segment crosses interior of pixel side
             * This is the case if the orientations for each corner of the side are different.
             */

            int orientUL = CGAlgorithmsDD.OrientationIndex(px, py, qx, qy, _minx, _maxy);
            if (orientUL == 0)
            {
                if (py < qy) return false;
                return true;
            }

            int orientUR = CGAlgorithmsDD.OrientationIndex(px, py, qx, qy, _maxx, _maxy);
            if (orientUR == 0)
            {
                if (py > qy) return false;
                return true;
            }
            //--- check crossing Top side
            if (orientUL != orientUR)
            {
                return true;
            }

            int orientLL = CGAlgorithmsDD.OrientationIndex(px, py, qx, qy, _minx, _miny);
            if (orientUL == 0)
            {
                // LL corner is the only one in pixel interior
                return true;
            }
            //--- check crossing Left side
            if (orientLL != orientUL)
            {
                return true;
            }

            int orientLR = CGAlgorithmsDD.OrientationIndex(px, py, qx, qy, _maxx, _miny);
            if (orientLR == 0)
            {
                if (py < qy) return false;
                return true;
            }

            //--- check crossing Bottom side
            if (orientLL != orientLR)
            {
                return true;
            }
            //--- check crossing Right side
            if (orientLR != orientUR)
            {
                return true;
            }

            // segment does not intersect pixel
            return false;
        }


        /// <summary>
        /// Test whether the given segment intersects
        /// the closure of this hot pixel.
        /// This is NOT the test used in the standard snap-rounding
        /// algorithm, which uses the partially closed tolerance square instead.
        /// This routine is provided for testing purposes only.
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <returns></returns>
        private bool IntersectsPixelClosure(Coordinate p0, Coordinate p1)
        {
            const int upperRight = 0;
            const int upperLeft = 1;
            const int lowerLeft = 2;
            const int lowerRight = 3;

            var corner = new Coordinate[4];
            corner[upperRight] = new Coordinate(_maxx, _maxy);
            corner[upperLeft] = new Coordinate(_minx, _maxy);
            corner[lowerLeft] = new Coordinate(_minx, _miny);
            corner[lowerRight] = new Coordinate(_maxx, _miny);

            LineIntersector li = new RobustLineIntersector();
            li.ComputeIntersection(p0, p1, corner[0], corner[1]);
            if (li.HasIntersection) return true;
            li.ComputeIntersection(p0, p1, corner[1], corner[2]);
            if (li.HasIntersection) return true;
            li.ComputeIntersection(p0, p1, corner[2], corner[3]);
            if (li.HasIntersection) return true;
            li.ComputeIntersection(p0, p1, corner[3], corner[0]);
            if (li.HasIntersection) return true;  

            return false;
        }

        /// <summary>
        /// Adds a new node (equal to the snap pt) to the specified segment
        /// if the segment passes through the hot pixel
        /// </summary>
        /// <param name="segStr"></param>
        /// <param name="segIndex"></param>
        /// <returns><c>true</c> if a node was added to the segment</returns>
        [Obsolete("Moved to MCIndexPointSnapper", true)]
        public bool AddSnappedNode(INodableSegmentString segStr, int segIndex)
        {
            var coords = segStr.Coordinates;
            var p0 = coords[segIndex];
            var p1 = coords[segIndex + 1];

            if (Intersects(p0, p1))
            {
                //System.out.println("snapped: " + snapPt);
                //System.out.println("POINT (" + snapPt.x + " " + snapPt.y + ")");
                segStr.AddIntersection(Coordinate, segIndex);

                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return $"HP({WKTWriter.Format(_ptHot)})";
        }
    }
}
