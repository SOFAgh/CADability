// https://github.com/microsoft/testfx/issues/564
// We use the Microsoft.VisualStudio.TestTools.UnitTesting namespace,
// because failed Asserts from extension methods in the namespaces
//   Microsoft.VisualStudio.TestTools.UnitTesting
//   Microsoft.VisualStudio.TestPlatform.MSTest.TestAdapter
// do not show in the test window but the calling method,
// which is what we want to achive.
namespace Microsoft.VisualStudio.TestTools.UnitTesting
{

    public static class Extensions
    {

        public static T Single<T>(this Assert assert, IEnumerable<T> collection)
        {
            Assert.AreEqual(1, collection.Count());
            return collection.Single();
        }

        public static T IsInstanceOfType<T>(this Assert assert, object value)
        {
            Assert.IsNotNull(value);
            Assert.IsInstanceOfType(value, typeof(T));
            return (T)value;
        }

        public static void BitmapsAreEqual(this Assert assert, Bitmap expected, Bitmap actual)
        {
            Assert.AreEqual(expected.Size, actual.Size);

            using (Bitmap diff = new Bitmap(expected.Width, expected.Height))
            {
                using (var gr = Graphics.FromImage(diff))
                {
                    gr.FillRectangle(Brushes.White, 0, 0, diff.Width, diff.Height);
                }

                bool fail = false;
                for (int x = 0; x < expected.Height; x++)
                {
                    for (int y = 0; y < expected.Width; y++)
                    {
                        var expectedPixel = expected.GetPixel(x, y);
                        var actualPixel = actual.GetPixel(x, y);
                        if (expectedPixel.ToArgb() != actualPixel.ToArgb())
                        {
                            diff.SetPixel(x, y, Color.Red);
                            fail = true;
                        }
                    }
                }

                if (fail)
                {
                    var fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".bmp");
                    using (var result = new Bitmap(expected.Width * 3, expected.Height))
                    using (var gr = Graphics.FromImage(result))
                    {
                        gr.DrawImageUnscaled(expected, new Point(0, 0));
                        gr.DrawImageUnscaled(actual, new Point(expected.Width, 0));
                        gr.DrawImageUnscaled(diff, new Point(expected.Width * 2, 0));
                        result.Save(fileName, System.Drawing.Imaging.ImageFormat.Bmp);
                    }
                    Assert.Fail($"result does not match expected output, see {fileName} for comparison");
                }
            }
        }

    }
}
