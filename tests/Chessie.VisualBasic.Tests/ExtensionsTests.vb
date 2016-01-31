#If Not __MonoCS__ Then
Imports Chessie.ErrorHandling
Imports Chessie.ErrorHandling.CSharp
Imports Microsoft.FSharp.Core
Imports NUnit.Framework

<TestFixture>
Public Class ExtensionsTests
    <Test>
    Public Sub JoinToResultsOfSuccessWorks()
        Dim result1 = Result(Of Integer, String).Succeed(1, "added one")
        Dim result2 = Result(Of Integer, String).Succeed(2, "added two")

        Dim result3 = result1.Join(result2, Function(i) 0, Function(i) (-0), Function(i1, i2) i1 + i2)

        result3.Match(Sub(x, msgs)
                          Assert.AreEqual(3, x)
                          Assert.That(msgs, [Is].EquivalentTo({"added one", "added two"}))
                      End Sub,
                      Sub(errs) Assert.Fail())
    End Sub

    <Test>
    Public Sub Test()
        Dim f As Func(Of Result(Of String, String), Result(Of String, String), Result(Of String, String), Result(Of String, String)) = Function(r1, r2, r3) From a In r1 From b In r2 From c In r3 Select a + b + c

        f(Result(Of String, String).Succeed("1"), Result(Of String, String).Succeed("2"), Result(Of String, String).Succeed("3")).Match(Sub(s, list) Assert.That(s, [Is].EqualTo("123")),
                                                                                                                                        Sub(list) Assert.Fail("should not fail"))

        f(Result(Of String, String).Succeed("1", "msg1"), Result(Of String, String).Succeed("2", "msg2"), Result(Of String, String).Succeed("3", "msg3")).Match(Sub(s, list)
                                                                                                                                                                    Assert.That(s, [Is].EqualTo("123"))
                                                                                                                                                                    Assert.That(list, [Is].EquivalentTo({"msg1", "msg2", "msg3"}))
                                                                                                                                                                End Sub,
                                                                                                                                                                Sub(list) Assert.Fail("should not fail"))

        f(Result(Of String, String).FailWith("fail"), Result(Of String, String).Succeed("2"), Result(Of String, String).Succeed("3")).Match(Sub(s, list) Assert.Fail("should fail"),
                                                                                                                                            Sub(list) Assert.That(list, [Is].EquivalentTo({"fail"})))

        f(Result(Of String, String).Succeed("1"), Result(Of String, String).FailWith("fail"), Result(Of String, String).Succeed("3")).Match(Sub(s, list) Assert.Fail("should fail"),
                                                                                                                                            Sub(list) Assert.That(list, [Is].EquivalentTo({"fail"})))

        f(Result(Of String, String).Succeed("1"), Result(Of String, String).FailWith("fail1"), Result(Of String, String).FailWith("fail2")).Match(Sub(s, list) Assert.Fail("should fail"),
                                                                                                                                                  Sub(list) Assert.That(list, [Is].EquivalentTo({"fail1"})))
    End Sub

    <Test>
    Public Sub ToResultOnSomeShouldSucceed()
        Dim opt = FSharpOption(Of Integer).Some(42)
        Dim result = opt.ToResult("error")
        result.Match(Sub(x, msgs)
                         Assert.AreEqual(42, x)
                         Assert.That(msgs, [Is].Empty)
                     End Sub,
                     Sub(errs) Assert.Fail())
    End Sub

    <Test>
    Public Sub ToResultOnNoneShoulFail()
        Dim opt = FSharpOption(Of Integer).None
        Dim result = opt.ToResult("error")
        result.Match(Sub(x, list) Assert.Fail(),
                     Sub(errs) Assert.That(errs, [Is].EquivalentTo({"error"})))
    End Sub

    <Test>
    Public Sub MapFailureOnSuccessShouldReturnSuccess()
        Result(Of Integer, String).Succeed(42, "warn1").MapFailure(Function(list) {42}).Match(Sub(v, msgs)
                                                                                                  Assert.AreEqual(42, v)
                                                                                                  Assert.That(msgs, [Is].Empty)
                                                                                              End Sub,
                                                                                              Sub(errs) Assert.Fail())
    End Sub

    <Test>
    Public Sub MapFailureOnFailureShouldMapOverError()
        Result(Of Integer, String).FailWith({"err1", "err2"}).MapFailure(Function(list) {42}).Match(Sub(v, msgs) Assert.Fail(),
                                                                                                    Sub(errs) Assert.That(errs, [Is].EquivalentTo({42})))
    End Sub

    <Test>
    Public Sub MapFailureOnFailureShouldMapOverListOfErrors()
        Result(Of Integer, String).FailWith({"err1", "err2"}).MapFailure(Function(errs) errs.[Select](Function(err)
                                                                                                          Select Case err
                                                                                                              Case "err1"
                                                                                                                  Return 42
                                                                                                              Case "err2"
                                                                                                                  Return 43
                                                                                                              Case Else
                                                                                                                  Return 0
                                                                                                          End Select
                                                                                                      End Function)).Match(Sub(v, msgs) Assert.Fail(),
                                                                                                                           Sub(errs) Assert.That(errs, [Is].EquivalentTo({42, 43})))
    End Sub
End Class
#End If