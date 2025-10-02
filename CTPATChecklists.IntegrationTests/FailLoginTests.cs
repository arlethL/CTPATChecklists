using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CTPATChecklists.IntegrationTests
{
    [TestClass]
    public class FailLoginTests
    {
        [TestMethod]
        [TestCategory("Login")]
        public void Demo_Login_Falla_Adrede()
        {
            Assert.Fail("Prueba de demo (Login) que falla a propósito");
        }
    }
}
