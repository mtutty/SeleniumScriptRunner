using System.Reflection;
using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Collections;

namespace SeleniumScriptRunner.Result {
    public class NUnitResultAccumulator {

        private NUnitResult result;
        private IDictionary<string, DateTime> timers;

        public const string TRUE = @"True";
        public const string FALSE = @"False";
        public const string SUCCESS = @"Success";
        public const string FAILED = @"Failed";
        public const string INCONCLUSIVE = @"Inconclusive";
        public const string IGNORED = @"Ignored";
        public const string INVALID = @"Invalid";

        public NUnitResultAccumulator() : this(Assembly.GetEntryAssembly()) { }

        public NUnitResultAccumulator(Assembly testAssy) : this(testAssy.CodeBase) { }

        public NUnitResultAccumulator(string name) {
            this.result = CreateResult(name);
            this.timers = new Dictionary<string, DateTime>();
        }

        #region Public API
        public void Begin(string namespacePath, string fixtureName, string testName) {
            this.timers.Add(FullName(namespacePath, fixtureName, testName), DateTime.Now);
        }

        public void AssertionPassed(string namespacePath, string fixtureName, string testName) {
            TestCaseType tc = GetTestCase(namespacePath, fixtureName, testName);

            int oldCount = 0;
            if (int.TryParse(tc.asserts, out oldCount) == false) {
                tc.asserts = @"1";
            } else {
                oldCount++;
                tc.asserts = oldCount.ToString();
            }

            UpdateStatuses(
                namespacePath, fixtureName, testName,
                TRUE, SUCCESS, TRUE
            );
            UpdateTimings(namespacePath, fixtureName, testName);
        }

        public void AssertionFailed(string namespacePath, string fixtureName, string testName) {
            UpdateStatuses(
                namespacePath, fixtureName, testName,
                TRUE, FAILED, FALSE
            );
            UpdateTimings(namespacePath, fixtureName, testName);
        }

        public void Ignore(string namespacePath, string fixtureName, string testName) {
            UpdateStatuses(
                namespacePath, fixtureName, testName,
                FALSE, IGNORED, null
            );
            UpdateTimings(namespacePath, fixtureName, testName);
        }

        public void Inconclusive(string namespacePath, string fixtureName, string testName) {
            UpdateStatuses(
                namespacePath, fixtureName, testName,
                FALSE, INCONCLUSIVE, null
            );
            UpdateTimings(namespacePath, fixtureName, testName);
        }

        public void Skip(string namespacePath, string fixtureName, string testName) {
            Inconclusive(namespacePath, fixtureName, testName);
        }

        public void Invalid(string namespacePath, string fixtureName, string testName) {
            UpdateStatuses(
                namespacePath, fixtureName, testName,
                FALSE, INVALID, FALSE
            );
            UpdateTimings(namespacePath, fixtureName, testName);
        }
        #endregion

        #region Internal utility methods
        internal string[] ParseMethod(string methodFullName) {
            return methodFullName.Split('.');
        }

        internal string namespaceSubset(string[] pieces, int level) {
            return string.Join(@".", pieces.Take(level));
        }

        internal string FullName(string namespacePath, string fixtureName, string testName) {
            return string.Join(@".", namespacePath, fixtureName, testName);
        }

        internal TimeSpan GetTiming(string fullPath) {
            if (!this.timers.ContainsKey(fullPath)) {
                this.timers.Add(fullPath, DateTime.Now);
            }
            return new TimeSpan(DateTime.Now.Ticks - this.timers[fullPath].Ticks);
        }

        internal string GetTimingString(string fullPath) {
            TimeSpan diff = GetTiming(fullPath);
            return diff.ToString(@"s\.fff");
        }

        internal void UpdateTimings(string namespacePath, string fixtureName, string testName) {
            string[] pieces = ParseMethod(namespacePath);

            string rollup = string.Empty;
            for (int i = 1; i < pieces.Length + 1; i++) {
                rollup = namespaceSubset(pieces, i);
                TestSuiteType suite = GetSuite(rollup);
                suite.time = GetTimingString(rollup);
            }

            if (!string.IsNullOrEmpty(fixtureName)) {
                TestSuiteType fixture = GetFixture(namespacePath, fixtureName);
                string fullPath = string.Join(@".", namespacePath, fixtureName);
                fixture.time = GetTimingString(fullPath);

                if (!string.IsNullOrEmpty(testName)) {
                    TestCaseType tc = GetTestCase(namespacePath, fixtureName, testName);
                    tc.time = GetTimingString(FullName(namespacePath, fixtureName, testName));
                }
            }
        }

        internal void UpdateStatuses(string namespacePath, string fixtureName, string testName, string executed, string result, string success) {
            string[] pieces = ParseMethod(namespacePath);

            string rollup = string.Empty;
            for (int i = 1; i < pieces.Length + 1; i++) {
                rollup = namespaceSubset(pieces, i);
                TestSuiteType suite = GetSuite(rollup);

                if (!string.IsNullOrEmpty(executed)) suite.executed = executed;
                if (!string.IsNullOrEmpty(result)) suite.result = result;
                if (!string.IsNullOrEmpty(success)) suite.success = success;
            }

            if (!string.IsNullOrEmpty(fixtureName)) {
                TestSuiteType fixture = GetFixture(namespacePath, fixtureName);
                string fullPath = string.Join(@".", namespacePath, fixtureName);

                if (!string.IsNullOrEmpty(executed)) fixture.executed = executed;
                if (!string.IsNullOrEmpty(result)) fixture.result = result;
                if (!string.IsNullOrEmpty(success)) fixture.success = success;

                if (!string.IsNullOrEmpty(testName)) {
                    TestCaseType tc = GetTestCase(namespacePath, fixtureName, testName);

                    if (!string.IsNullOrEmpty(executed)) tc.executed = executed;
                    if (!string.IsNullOrEmpty(result)) tc.result = result;
                    if (!string.IsNullOrEmpty(success)) tc.success = success;
                }
            }
        }
        #endregion

        #region Hierarchy Navigation / Factory methods
        public TestCaseType GetTestCase(string namespacePath, string fixtureName, string testName) {
            TestSuiteType fixture = GetFixture(namespacePath, fixtureName);

            foreach (TestCaseType candidate in fixture.results.Items) {
                if (candidate.name.Equals(testName, StringComparison.CurrentCultureIgnoreCase)) return candidate;
            }

            return AppendTestCase(fixture.results, string.Join(@".", namespacePath, fixtureName, testName));
        }

        public TestSuiteType GetFixture(string namespacePath, string fixtureName) {
            TestSuiteType currentSuite = GetSuite(namespacePath);

            foreach (TestSuiteType candidate in currentSuite.results.Items) {
                if (candidate.type.Equals(@"TestFixture", StringComparison.CurrentCultureIgnoreCase) &&
                    candidate.name.Equals(fixtureName, StringComparison.CurrentCultureIgnoreCase)) {
                    return candidate;
                }
            }

            return AppendTestSuite(currentSuite.results, @"TestFixture", fixtureName);
        }

        public TestSuiteType GetSuite(string namespacePath) {
            string[] pieces = ParseMethod(namespacePath);

            TestSuiteType currentSuite = this.result.testsuite;
            for (int i = 1; i < pieces.Length; i++) {
                string piece = pieces[i];
                foreach (TestSuiteType candidate in currentSuite.results.Items) {
                    if (candidate.type.Equals(@"Namespace", StringComparison.CurrentCultureIgnoreCase) &&
                        candidate.name.Equals(piece, StringComparison.CurrentCultureIgnoreCase)) {
                        currentSuite = candidate;
                        continue;  //look for next piece
                    }
                    // not found? create and continue
                    currentSuite = AppendTestSuite(currentSuite.results, @"Namespace", piece);
                }
            }
            return currentSuite;
        }

        public TestSuiteType AppendTestSuite(ResultsType container, string type, string name) {
            TestSuiteType newItem = new TestSuiteType() {
                type = type,
                name = name,
                results = new ResultsType() {
                    Items = { }
                }
            };
            // Make sure the suite's results type is set up
            if (container == null) container = new ResultsType() { Items = { } };

            AppendToResults(container, newItem);
            return newItem;
        }

        public TestCaseType AppendTestCase(ResultsType container, string fullName) {
            TestCaseType newItem = new TestCaseType() {
                name = fullName
            };

            AppendToResults(container, newItem);
            return newItem;
        }

        public void AppendToResults(ResultsType container, object child) {
            // Make sure the suite's results type is set up
            if (container == null) container = new ResultsType() { Items = { } };

            // Append new child
            List<object> results = new List<object>(container.Items);
            results.Add(child);
            container.Items = results.ToArray();
        }
        #endregion

        #region static methods
        public static NUnitResult CreateResult() {
            return CreateResult(Assembly.GetEntryAssembly().CodeBase);
        }

        public static NUnitResult CreateResult(string name) {
            NUnitResult ret = new NUnitResult();

            /*
            <test-results name="/home/charlie/Dev/NUnit/nunit-2.5/work/src/bin/Debug/tests/mock-assembly.dll" total="21" errors="1" failures="1" not-run="7" inconclusive="1" ignored="4" skipped="0" invalid="3" date="2010-10-18" time="13:23:35">
              <environment nunit-version="2.5.8.0" clr-version="2.0.50727.1433" os-version="Unix 2.6.32.25" platform="Unix" cwd="/home/charlie/Dev/NUnit/nunit-2.5/work/src/bin/Debug" machine-name="cedar" user="charlie" user-domain="cedar" />
              <culture-info current-culture="en-US" current-uiculture="en-US" />
             */
            ret.name = name;
            ret.total = 0;
            ret.errors = 0;
            ret.failures = 0;
            ret.notrun = 0;
            ret.inconclusive = 0;
            ret.ignored = 0;
            ret.skipped = 0;
            ret.invalid = 0;
            ret.date = DateTime.Now.ToString(@"yy-MM-dd");
            ret.time = DateTime.Now.ToString(@"HH-mm-ss");
            ret.environment = CreateEnvironmentType();
            ret.cultureinfo = new CultureinfoType() {
                currentculture = CultureInfo.CurrentCulture.NativeName,
                currentuiculture = CultureInfo.CurrentUICulture.NativeName
            };

            return ret;
        }

        public static EnvironmentType CreateEnvironmentType() {
            return new EnvironmentType() {
                clrversion = System.Runtime.InteropServices.RuntimeEnvironment.GetSystemVersion(),
                cwd = System.Environment.CurrentDirectory,
                machinename = System.Environment.MachineName,
                nunitversion = NUnitVersionIfLoaded(),
                osversion = System.Environment.OSVersion.VersionString,
                platform = System.Environment.OSVersion.Platform.ToString(),
                user = System.Environment.UserName,
                userdomain = System.Environment.UserDomainName
            };
        }

        public static string NUnitVersionIfLoaded() {
            foreach (Assembly assy in AppDomain.CurrentDomain.GetAssemblies()) {
                if (assy.FullName.StartsWith(@"nunit.framework", StringComparison.CurrentCultureIgnoreCase)) return assy.GetName().Version.ToString();
            }
            return string.Empty;
        }
        #endregion
    }
}
