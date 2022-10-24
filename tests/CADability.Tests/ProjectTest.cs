using CADability.Attribute;
using CADability.Forms;
using CADability.Shapes;

namespace CADability.Tests
{
    [TestClass]
    public class ProjectTest
    {

        public TestContext TestContext { get; set; }

        [TestMethod]
        [DeploymentItem(@"Files/Dxf/square_100x100.dxf", nameof(import_dxf_square_succeds))]
        [DeploymentItem(@"Files/Dxf/square_100x100.png", nameof(import_dxf_square_succeds))]
        public void import_dxf_square_succeds()
        {
            var file = Path.Combine(this.TestContext.DeploymentDirectory, this.TestContext.TestName, "square_100x100.dxf");
            Assert.IsTrue(File.Exists(file));
            var bmpFile = Path.Combine(this.TestContext.DeploymentDirectory, this.TestContext.TestName, "square_100x100.png");
            Assert.IsTrue(File.Exists(bmpFile));

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
        [DeploymentItem(@"Files/Dxf/square_with_reduction.dxf", nameof(import_dxf_square_with_reduction_succeds))]
        [DeploymentItem(@"Files/Dxf/square_with_reduction.png", nameof(import_dxf_square_with_reduction_succeds))]
        public void import_dxf_square_with_reduction_succeds()
        {
            // This file is s square with a reduction =>
            //   * black circle with diameter 11.0
            //   * yellow circle with diameter 11.0
            //   * yellow circle with diameter 20.0
            //   * all with the same center
            // After loading the file with CADability the circles are outside of the square (bottom right) while the circles are actually
            // inside the square.

            var file = Path.Combine(this.TestContext.DeploymentDirectory, this.TestContext.TestName, "square_with_reduction.dxf");
            Assert.IsTrue(File.Exists(file));
            var bmpFile = Path.Combine(this.TestContext.DeploymentDirectory, this.TestContext.TestName, "square_with_reduction.png");
            Assert.IsTrue(File.Exists(bmpFile));

            var project = Project.ReadFromFile(file, "dxf");
            Assert.IsNotNull(project);
            var model = project.GetActiveModel();
            Assert.IsNotNull(model);
            var circle = model.AllObjects.OfType<GeoObject.Ellipse>().First();
            Assert.IsNotNull(circle);

            // check if the center of the circle is inside the outline.
            var shape = CompoundShape.CreateFromList(model.AllObjects, Precision.eps, out var plane);
            Assert.IsNotNull(shape);
            Assert.IsNotNull(plane);
            Assert.AreEqual(1, shape.SimpleShapes.Length);
            var center = plane.ToLocal(circle.Center);
            var result = shape.SimpleShapes[0].Outline.GetPosition(center.To2D());
            Assert.AreEqual(Border.Position.Inside, result);
            using (var expected = (Bitmap)Image.FromFile(bmpFile))
            using (var actual = PaintToOpenGL.PaintToBitmap(model.AllObjects, GeoVector.ZAxis, 200, 200))
            {
                // if you're confident that the acutal result is correct,
                // save the image and use it as refercene for comparison.
                //actual.Save(Path.GetFileName(bmpFile), System.Drawing.Imaging.ImageFormat.Bmp);
                Assert.That.BitmapsAreEqual(expected, actual);
            }
        }
    }
}
