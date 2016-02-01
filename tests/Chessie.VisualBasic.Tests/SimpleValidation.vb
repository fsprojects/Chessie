Imports Chessie.ErrorHandling
Imports Chessie.ErrorHandling.CSharp
Imports NUnit.Framework

Public Class Request
    Public Property Name() As String
    Public Property EMail() As String
End Class

Public Class Validation
    Public Shared Function ValidateInput(input As Request) As Result(Of Request, String)
        If input.Name = "" Then
            Return Result(Of Request, String).FailWith("Name must not be blank")
        End If
        If input.EMail = "" Then
            Return Result(Of Request, String).FailWith("Email must not be blank")
        End If
        Return Result(Of Request, String).Succeed(input)

    End Function
End Class

<TestFixture>
Public Class TrySpecs
    <Test>
    Public Sub TryWillCatch()
        Dim exn = New Exception("Hello World")
        Dim result = ErrorHandling.Result(Of String, Exception).[Try](Function()
                                                                          Throw exn
                                                                          Return ""
                                                                      End Function)
        Assert.AreEqual(exn, result.FailedWith().First())
    End Sub

    <Test>
    Public Sub TryWillReturnValue()
        Dim result = ErrorHandling.Result(Of String, Exception).[Try](Function() "hello world")
        Assert.AreEqual("hello world", result.SucceededWith())
    End Sub
End Class

<TestFixture>
Public Class SimpleValidation
    <Test>
    Public Sub CanCreateSuccess()
        Dim request = New Request() With {
            .Name = "Steffen",
            .EMail = "mail@support.com"
        }
        Dim result = Validation.ValidateInput(request)
        Assert.AreEqual(request, result.SucceededWith())
    End Sub
End Class

<TestFixture>
Public Class SimplePatternMatching
    <Test>
    Public Sub CanMatchSuccess()
        Dim request = New Request() With {
            .Name = "Steffen",
            .EMail = "mail@support.com"
        }
        Dim result = Validation.ValidateInput(request)
        result.Match(Sub(x, msgs) Assert.AreEqual(request, x),
                     Sub(msgs) Throw New Exception("wrong match case"))
    End Sub

    <Test>
    Public Sub CanMatchFailure()
        Dim request = New Request() With {
            .Name = "Steffen",
            .EMail = ""
        }
        Dim result = Validation.ValidateInput(request)
        result.Match(Sub(x, msgs) Throw New Exception("wrong match case"),
                     Sub(msgs) Assert.AreEqual("Email must not be blank", msgs(0)))
    End Sub
End Class

<TestFixture>
Public Class SimpleEitherPatternMatching
    <Test>
    Public Sub CanMatchSuccess()
        Dim request = New Request() With {
            .Name = "Steffen",
            .EMail = "mail@support.com"
        }
        Dim result = Validation.ValidateInput(request).Either(Function(x, msgs) x,
                                                              Function(msgs)
                                                                  Throw New Exception("wrong match case")
                                                                  Return request
                                                              End Function)
        Assert.AreEqual(request, result)
    End Sub

    <Test>
    Public Sub CanMatchFailure()
        Dim request = New Request() With {
            .Name = "Steffen",
            .EMail = ""
        }
        Dim result = Validation.ValidateInput(request).Either(Function(x, msgs)
                                                                  Throw New Exception("wrong match case")
                                                                  Return ""
                                                              End Function,
                                                              Function(msgs) msgs(0))

        Assert.AreEqual("Email must not be blank", result)
    End Sub
End Class
