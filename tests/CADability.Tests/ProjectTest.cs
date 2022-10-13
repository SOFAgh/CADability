using CADability.Forms;
using System.IO.Compression;

namespace CADability.Tests
{
    [TestClass]
    public class ProjectTest
    {

        public TestContext TestContext { get; set; }

        [TestMethod]
        [DeploymentItem(@"Files/Dxf/square_100x100.dxf", nameof(import_dxf_square_succeds))]
        [DeploymentItem(@"Files/Dxf/square_100x100.bmp", nameof(import_dxf_square_succeds))]
        public void import_dxf_square_succeds()
        {
            var file = Path.Combine(this.TestContext.DeploymentDirectory, this.TestContext.TestName, "square_100x100.dxf");
            Assert.IsTrue(File.Exists(file));
            var bmpFile = Path.Combine(this.TestContext.DeploymentDirectory, this.TestContext.TestName, "square_100x100.bmp");

            var project = Project.ReadFromFile(file, "dxf");
            Assert.IsNotNull(project);
            var model = project.GetActiveModel();
            Assert.IsNotNull(model);
            var obj = Assert.That.Single(model.AllObjects);
            var polyline = Assert.That.IsInstanceOfType<GeoObject.Polyline>(obj);
            Assert.AreEqual(400, polyline.Length);


            using (var expected = (Bitmap)Image.FromFile(bmpFile))
            using (var actual = PaintToOpenGL.PaintToBitmap(model.AllObjects, GeoVector.ZAxis, 100, 100))
            {
                Assert.That.BitmapsAreEqual(expected, actual);
            }
        }

        [TestMethod]
        [DeploymentItem(@"Files/Step/issue101.stp", nameof(import_step_issue101_succeds))]
        [DeploymentItem(@"Files/Step/issue101.png", nameof(import_step_issue101_succeds))]
        public void import_step_issue101_succeds()
        {
            // cylinder.OutwardOriented throws an NotImplementedException
            // because there is a concret implementation
            //   public bool OutwardOriented => toCylinder.Determinant > 0;
            // and an explicit interface implementation
            //   bool ICylinder.OutwardOriented => throw new NotImplementedException();

            var file = Path.Combine(this.TestContext.DeploymentDirectory, this.TestContext.TestName, "issue101.stp");
            Assert.IsTrue(File.Exists(file));
            var bmpFile = Path.Combine(this.TestContext.DeploymentDirectory, this.TestContext.TestName, "issue101.png");
            Assert.IsTrue(File.Exists(bmpFile));

            var project = Project.ReadFromFile(file, "stp");
            Assert.IsNotNull(project);
            var model = project.GetActiveModel();
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.AllObjects.Count);

            var solid = Assert.That.IsInstanceOfType<GeoObject.Solid>(model.AllObjects[0]);
            var shell = solid.Shells[0];
            // get a cylinder (order faces and cylinders to always get the same entity)
            var cylinder = shell
                .Faces
                .OrderBy(x => x.Area)
                .Select(x => x.Surface)
                .OfType<GeoObject.ICylinder>()
                .OrderBy(x => x.Radius)
                .FirstOrDefault();
            Assert.IsTrue(cylinder.OutwardOriented);

            using (var expected = (Bitmap)Image.FromFile(bmpFile))
            using (var actual = PaintToOpenGL.PaintToBitmap(model.AllObjects, GeoVector.NullVector, 200, 200))
            {
                Assert.That.BitmapsAreEqual(expected, actual);
            }
        }

    }
}
