using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NUnit.Framework;

namespace NetTopologySuite.Samples.Tests.Various
{
    // see https://code.google.com/p/nettopologysuite/issues/detail?id=172
    [TestFixture]
    public class Issue172TestFixture
    {
        [Test, Category("Issue172")]
        public void ensure_geomcollection_union_works_as_expected()
        {
            const string wkt = "GEOMETRYCOLLECTION (LINESTRING (91.8060659371673 173.20142725005167, 91.39164134738692 173.99735164258493), LINESTRING (91.8060659371673 173.20142725005167, 93.86031559446384 169.09495765102088), POLYGON ((94.99521652217337 163.95078872427842, 95.49722524801828 162.9538793247459, 95.882367193821 162.14711279527867, 93.64652028360543 161.02863678119544, 92.74726556828251 162.82625769700874, 91.58462517497041 162.24465016968549, 89.30221145595522 166.80722245307652, 86.83657677518224 171.7035803499217, 87.99767166613637 172.28826715001233, 87.09365592138087 174.08349848141063, 89.3265307116773 175.2078961738926, 89.72851923404312 174.40961097321966, 89.74417082853229 174.4174406385417, 90.243535110612 173.41920547471025, 91.39164134738692 173.99735164258493, 91.79518263019484 173.19598290690502, 91.8060659371673 173.20142725005167, 93.86031559446387 169.09495765102082, 95.75430439421692 165.33379745623483, 96.14575287181019 164.52634123841523, 94.99521652217337 163.95078872427842)), POLYGON ((96.14575287181019 164.52634123841523, 95.75430439421692 165.33379745623483, 93.86031559446384 169.09495765102088, 96.14575287181019 164.52634123841523)))";
            var reader = new WKTReader();
            var geom = reader.Read(wkt);
            Assert.That(geom, Is.Not.Null);
            Assert.That(geom.IsValid, Is.True);
            Assert.That(geom, Is.InstanceOf<GeometryCollection>());
            var res = geom.Union();
            Assert.That(res, Is.Not.Null);
            Assert.That(res.IsValid, Is.True);
            Assert.That(res, Is.InstanceOf<GeometryCollection>());
        }
    }
}