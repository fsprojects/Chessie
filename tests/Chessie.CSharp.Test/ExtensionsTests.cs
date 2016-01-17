using System;
using Chessie.ErrorHandling;
using Chessie.ErrorHandling.CSharp;
using Microsoft.FSharp.Core;
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

        [Test]
        public void ToResultOnSomeShouldSucceed()
        {
            var opt = FSharpOption<int>.Some(42);
            var result = opt.ToResult("error");
            result.Match(
                ifSuccess: (x, msgs) =>
                {
                    Assert.AreEqual(42, x);
                    Assert.That(msgs, Is.Empty);
                },
                ifFailure: errs => Assert.Fail());
        }

        [Test]
        public void ToResultOnNoneShoulFail()
        {
            var opt = FSharpOption<int>.None;
            var result = opt.ToResult("error");
            result.Match(
                ifSuccess: (x, _) => Assert.Fail(),
                ifFailure: errs => Assert.That(errs, Is.EquivalentTo(new[] {"error"})));
        }
    }
}