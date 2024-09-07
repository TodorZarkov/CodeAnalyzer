namespace NullForgiving.Test
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Threading.Tasks;
    using VerifyCS = NullForgiving.Test.CSharpCodeFixVerifier<
        NullForgiving.NullForgivingAnalyzer,
        NullForgiving.NullForgivingCodeFixProvider>;

    [TestClass]
    public class NullForgivingUnitTest
    {
        [TestMethod]
        public async Task Test_No_Diagnostics_Expected_To_Show()
        {
            var test = @"
#nullable enable

using System;

internal class Program
{
    private static void Main(string[] args)
    {
        string? text = null;
        Console.WriteLine(text);
    }

}
";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task Test_For_Diagnostic_And_CodeFix()
        {
            var test = @"
#nullable enable

using System;

internal class Program
{
    private static void Main(string[] args)
    {
        string text = {|#0:null!|};
        Console.WriteLine(text);
    }

}";

            var fixTest = @"
#nullable enable

using System;

internal class Program
{
    private static void Main(string[] args)
    {
        string? text = null;
        Console.WriteLine(text);
    }

}";

            var expected = VerifyCS.Diagnostic("TZ001").WithLocation(0).WithArguments("null!");
            await VerifyCS.VerifyCodeFixAsync(test, expected, fixTest);
        }
    }
}
