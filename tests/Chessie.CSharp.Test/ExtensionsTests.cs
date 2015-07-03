using System;
using Chessie.ErrorHandling;
using Chessie.ErrorHandling.CSharp;
using NUnit.Framework;

namespace Chessie.CSharp.Test
{
    [TestFixture]
    public class ExtensionsTests
    {
        [Test]
        public void JoinToResultsOfSuccessWorks()
        {
            var result1 = Result<int, string>.Succeed(1, "added one");
            var result2 = Result<int, string>.Succeed(2, "added two");

            var result3 = result1.Join(result2, _ => 0, _ => -0, (i1, i2) => i1 + i2);

            result3.Match(
                ifSuccess: (x, msgs) =>
                {
                    Assert.AreEqual(3, x);
                    Assert.That(msgs, Is.EquivalentTo(new[] {"added one", "added two"}));
                },
                ifFailure: errs => Assert.Fail());
        }

        enum Error { Error1, Error2 }

        [Test]
        public void WithMsgTypeSucceed_ShouldInferTypeCorrectly()
        {
            var f = new Func<int, int, int>((s1, s2) => s1 + s2);
            var result = WithMsgType<Error>.Succeed(f.Curry());
            Assert.That(result, Is.TypeOf<Result<Func<int, Func<int,int>>,Error>.Ok>());
        }

        [Test]
        public void ApValidation_ShouldApplyInnerFunctionCorrectly()
        {
            var f = new Func<int, int, int>((s1, s2) => s1 + s2);
            var result = WithMsgType<Error>.Succeed(f.Curry())
                .Apply(Result<int, Error>.Succeed(1))
                .Apply(Result<int, Error>.Succeed(2));

            result.Match(
                ifSuccess: (x, msgs) => Assert.AreEqual(3,x),
                ifFailure: errs => Assert.Fail());
        }

        [Test]
        public void CurryTest()
        {
            var f1 = new Func<string, string, string>((s1, s2) => s1 + s2).Curry();
            var result1 = f1("1")("2");
            Assert.AreEqual("12", result1);

            var f2 = new Func<string, string, string, string>((s1, s2, s3) => s1 + s2 + s3).Curry();
            var result2 = f2("1")("2")("3");
            Assert.AreEqual("123", result2);

            var f3 = new Func<string, string, string, string, string>((s1, s2, s3, s4) => s1 + s2 + s3 + s4).Curry();
            var result3 = f3("1")("2")("3")("4");
            Assert.AreEqual("1234", result3);

            var f4 = new Func<string, string, string, string, string, string>((s1, s2, s3, s4, s5) => s1 + s2 + s3 + s4 + s5).Curry();
            var result4 = f4("1")("2")("3")("4")("5");
            Assert.AreEqual("12345", result4);

            var f5 = new Func<string, string, string, string, string, string, string>((s1, s2, s3, s4, s5, s6) => s1 + s2 + s3 + s4 + s5 + s6).Curry();
            var result5 = f5("1")("2")("3")("4")("5")("6");
            Assert.AreEqual("123456", result5);

            var f6 = new Func<string, string, string, string, string, string, string, string>((s1, s2, s3, s4, s5, s6, s7) => s1 + s2 + s3 + s4 + s5 + s6 + s7).Curry();
            var result6 = f6("1")("2")("3")("4")("5")("6")("7");
            Assert.AreEqual("1234567", result6);

            var f7 = new Func<string, string, string, string, string, string, string, string, string>((s1, s2, s3, s4, s5, s6, s7, s8) => s1 + s2 + s3 + s4 + s5 + s6 + s7 + s8).Curry();
            var result7 = f7("1")("2")("3")("4")("5")("6")("7")("8");
            Assert.AreEqual("12345678", result7);
        }
    }
}