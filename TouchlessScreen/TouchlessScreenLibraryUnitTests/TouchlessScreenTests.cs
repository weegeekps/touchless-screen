using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TouchlessScreenLibrary;

namespace TouchlessScreenLibraryUnitTests
{
    [TestClass]
    public class TouchlessScreenTests
    {
        [TestMethod]
        public void InstantiateTouchlessScreenClass()
        {
            TouchlessScreen tScreen;

            try
            {
                tScreen = new TouchlessScreen();
            }
            catch (NotImplementedException)
            {
                Assert.Inconclusive("Not yet implemented.");
            }
            catch (Exception e)
            {
                Assert.Fail("An error occured: " + e);
            }
        }
    }
}
