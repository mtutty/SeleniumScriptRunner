
namespace SeleniumScriptRunner.Script {
    public class TestRunDescriptor {

        public TestRunDescriptor(string suite, string fixture, string test) {
            this.SuiteName = suite;
            this.FixtureName = fixture;
            this.TestName = test;
        }

        public string SuiteName { get; set; }
        public string FixtureName { get; set; }
        public string TestName { get; set; }
    }
}
