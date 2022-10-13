using CADability.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

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
        [DeploymentItem(@"Files/Stp/issue104.stp", nameof(import_step_file_should_always_returns_a_solid))]
        public void import_step_file_should_always_returns_a_solid()
        {

            // this unit test demonstrates that importing a step file sometimes
            // returns a Shell instead of a Solid
            var file = Path.Combine(this.TestContext.DeploymentDirectory, this.TestContext.TestName, "issue104.stp");
            Assert.IsTrue(File.Exists(file));

            var dict = new Dictionary<Type, int>();

            // read the same file 100 times
            // and add or update type count in dict
            for (int i = 0; i < 100; i++)
            {


                var project = Project.ReadFromFile(file, "stp");
                Assert.IsNotNull(project);
                var model = project.GetActiveModel();
                Assert.IsNotNull(model);

                var obj = Assert.That.Single(model.AllObjects);
                var type = obj.GetType();
                if (!dict.ContainsKey(type))
                {
                    dict.Add(type, 1);
                }
                else
                {
                    dict[type]++;
                }
            }

            Assert.AreEqual(1, dict.Count, "Outcome:" + String.Join(", ", dict.Select(x => $"{x.Key.Name} * {x.Value}")));
            Assert.That.IsInstanceOfType<GeoObject.Solid>(dict.ElementAt(0).Key);


        }

    }
}
