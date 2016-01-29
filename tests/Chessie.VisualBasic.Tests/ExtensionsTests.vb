Imports Chessie.ErrorHandling
Imports Chessie.ErrorHandling.CSharp
Imports Microsoft.FSharp.Collections
Imports Microsoft.FSharp.Core
Imports NUnit.Framework
''' <summary>
''' Had to convert functions to only use features available in .NET 2.0 to be sure of compilation on Mono
''' No LINQ
''' No lambdas
''' No auto properties
''' ect
''' </summary>
<TestFixture>
Public Class ExtensionsTests
    <Test>
    Public Sub JoinToResultsOfSuccessWorks()
        Dim result1 As Result(Of Integer, String) = Result(Of Integer, String).Succeed(1, "added one")
        Dim result2 As Result(Of Integer, String) = Result(Of Integer, String).Succeed(2, "added two")
        Dim result3 As Result(Of Integer, String) = result1.Join(result2, AddressOf Zero, AddressOf NegativeZero, AddressOf AddTwoNumbers)
        result3.Match(AddressOf JoinResultOfSuccessIfSuccess, AddressOf AssertFailAnyFailure)
    End Sub

    Private Function AddTwoNumbers(i1 As Integer, i2 As Integer) As Integer
        Return i1 + i2
    End Function

    Private Function Zero() As Integer
        Return 0
    End Function

    Private Function NegativeZero() As Integer
        Return 0
    End Function

    Private Sub AssertFailAnyFailure(errs As FSharpList(Of String))
        Assert.Fail()
    End Sub

    Private Sub JoinResultOfSuccessIfSuccess(x As Integer, msgs As FSharpList(Of String))
        Assert.AreEqual(3, x)
        Dim expected As String() = {"added one", "added two"}
        Assert.That(msgs, [Is].EquivalentTo(expected))
    End Sub

    ''' <summary>
    ''' To compile for mono, this test is replacing :
    '''     Return From a In r1
    '''            From b In r2
    '''            From c In r3
    '''            Select a + b + c;
    ''' From 
    ''' </summary>
    ''' <param name="r1"></param>
    ''' <param name="r2"></param>
    ''' <param name="r3"></param>
    ''' <returns></returns>
    Private Function TestF(r1 As Result(Of String, String), r2 As Result(Of String, String), r3 As Result(Of String, String)) As Result(Of String, String)
        Return r1.SelectMany(AddressOf New Lambda(r2).ABindB).SelectMany(AddressOf New Lambda(r3).ABindB)
    End Function
    Private Class Lambda
        Private state As Result(Of String, String)
        Private B As String
        Public Sub New(state As Result(Of String, String))
            Me.state = state
        End Sub
        Public Function ABindB(B As String) As Result(Of String, String)
            If state.IsOk Then
                Me.B = B
                Return state.Select(AddressOf BPlusA)
            Else
                Return state
            End If
        End Function
        Public Function BPlusA(A As String) As String
            Return B + A
        End Function
    End Class

    <Test>
    Public Sub Test()
        Dim f As Func(Of Result(Of String, String), Result(Of String, String), Result(Of String, String), Result(Of String, String)) = AddressOf TestF

        f(Result(Of String, String).Succeed("1"), Result(Of String, String).Succeed("2"), Result(Of String, String).Succeed("3")).Match(AddressOf CheckFor123, AddressOf ShouldNotFail)

        f(Result(Of String, String).Succeed("1", "msg1"), Result(Of String, String).Succeed("2", "msg2"), Result(Of String, String).Succeed("3", "msg3")).Match(AddressOf CheckFor123AndMessages, AddressOf ShouldNotFail)

        f(Result(Of String, String).FailWith("fail"), Result(Of String, String).Succeed("2"), Result(Of String, String).Succeed("3")).Match(AddressOf TestIfSuccess, AddressOf TestIfFailureIsfail)

        f(Result(Of String, String).Succeed("1"), Result(Of String, String).FailWith("fail"), Result(Of String, String).Succeed("3")).Match(AddressOf TestIfSuccess, AddressOf TestIfFailureIsfail)

        f(Result(Of String, String).Succeed("1"), Result(Of String, String).FailWith("fail1"), Result(Of String, String).FailWith("fail2")).Match(AddressOf TestIfSuccess, AddressOf TestIfFailureIsfail1)
    End Sub

    Private Sub CheckFor123(s As String, msgs As FSharpList(Of String))
        Assert.That(s, [Is].EqualTo("123"))
    End Sub

    Private Sub CheckFor123AndMessages(s As String, list As FSharpList(Of String))
        Assert.That(s, [Is].EqualTo("123"))
        Dim expected As String() = {"msg1", "msg2", "msg3"}
        Assert.That(list, [Is].EquivalentTo(expected))
    End Sub

    Private Sub TestIfFailureIsfail1(list As FSharpList(Of String))
        Dim expected As String() = {"fail1"}
        Assert.That(list, [Is].EquivalentTo(expected))
    End Sub
    Private Sub TestIfFailureIsfail(list As FSharpList(Of String))
        Dim expected As String() = {"fail"}
        Assert.That(list, [Is].EquivalentTo(expected))
    End Sub

    Private Sub TestIfSuccess(s As String, list As FSharpList(Of String))
        Assert.Fail("should fail")
    End Sub

    Private Sub ShouldNotFail(list As FSharpList(Of String))
        Assert.Fail("should not fail")
    End Sub

    <Test>
    Public Sub ToResultOnSomeShouldSucceed()
        Dim opt As FSharpOption(Of Integer) = FSharpOption(Of Integer).Some(42)
        Dim result As Result(Of Integer, String) = opt.ToResult("error")
        result.Match(AddressOf ToResultOnSomeShouldSucceedIfSuccess, AddressOf AssertFailAnyFailure)
    End Sub

    Private Sub ToResultOnSomeShouldSucceedIfSuccess(x As Integer, msgs As FSharpList(Of String))
        Assert.AreEqual(42, x)
        Assert.That(msgs, [Is].Empty)
    End Sub

    <Test>
    Public Sub ToResultOnNoneShoulFail()
        Dim opt As FSharpOption(Of Integer) = FSharpOption(Of Integer).None
        Dim result As Result(Of Integer, String) = opt.ToResult("error")
        result.Match(AddressOf ToResultOnNoneShouldFailIfSuccess, AddressOf ToResultOnNoneShouldFailIfFailure)
    End Sub

    Private Sub ToResultOnNoneShouldFailIfFailure(errs As FSharpList(Of String))
        Dim expected As String() = {"error"}
        Assert.That(errs, [Is].EquivalentTo(expected))
    End Sub

    Private Sub ToResultOnNoneShouldFailIfSuccess(x As Integer, list As FSharpList(Of String))
        Assert.Fail()
    End Sub

    <Test>
    Public Sub MapFailureOnSuccessShouldReturnSuccess()
        Result(Of Integer, String).Succeed(42, "warn1").MapFailure(AddressOf Create42).Match(AddressOf MapFailureOnSuccessShouldReturnSuccessIfSuccess, AddressOf AssertFailOnAnyFailureInteger)
    End Sub

    Private Sub AssertFailOnAnyFailureInteger(errs As FSharpList(Of Integer))
        Assert.Fail()
    End Sub

    Private Sub MapFailureOnSuccessShouldReturnSuccessIfSuccess(v As Integer, msgs As FSharpList(Of Integer))
        Assert.AreEqual(42, v)
        Assert.That(msgs, [Is].Empty)
    End Sub

    <Test>
    Public Sub MapFailureOnFailureShouldMapOverError()
        Result(Of Integer, String).FailWith({"err1", "err2"}).MapFailure(AddressOf Create42).Match(AddressOf AssertFailOnAnySuccess, AddressOf MapFailureOnFailureShouldBe42)
    End Sub

    Private Function Create42() As IEnumerable(Of Integer)
        Return {42}
    End Function

    Private Sub MapFailureOnFailureShouldBe42(errs As FSharpList(Of Integer))
        Assert.That(errs, [Is].EquivalentTo(Create42))
    End Sub

    Private Sub AssertFailOnAnySuccess(v As Integer, msgs As FSharpList(Of Integer))
        Assert.Fail()
    End Sub

    <Test>
    Public Sub MapFailureOnFailureShouldMapOverListOfErrors()
        Result(Of Integer, String).FailWith({"err1", "err2"}).MapFailure(AddressOf MapFailuresToList).Match(AddressOf AssertFailOnAnySuccess, AddressOf MapFailureOnFailureShouldMapOverListOfErrorsIfFailure)
    End Sub

    Private Function MapFailuresToList(errs As FSharpList(Of String)) As IEnumerable(Of Integer)
        Return errs.[Select](AddressOf ConvertList)
    End Function

    Private Function ConvertList(err As String) As Integer
        Select Case err
            Case "err1"
                Return 42
            Case "err2"
                Return 43
            Case Else
                Return 0
        End Select
    End Function

    Private Sub MapFailureOnFailureShouldMapOverListOfErrorsIfFailure(errs As FSharpList(Of Integer))
        Assert.That(errs, [Is].EquivalentTo({42, 43}))
    End Sub
End Class
