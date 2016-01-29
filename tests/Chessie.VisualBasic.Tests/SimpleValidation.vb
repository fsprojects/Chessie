Imports Chessie.ErrorHandling
Imports Chessie.ErrorHandling.CSharp
Imports Chessie.ErrorHandling.CSharp.ResultExtensions
Imports Microsoft.FSharp.Collections
Imports NUnit.Framework

Public Class Request
    Public Property Name() As String
        Get
            Return m_Name
        End Get
        Set
            m_Name = Value
        End Set
    End Property
    Private m_Name As String
    Public Property EMail() As String
        Get
            Return m_EMail
        End Get
        Set
            m_EMail = Value
        End Set
    End Property
    Private m_EMail As String
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
    Dim exn As Exception = New Exception("Hello World")
    <Test>
    Public Sub TryWillCatch()
        Dim result As Result(Of String, Exception) = ErrorHandling.Result(Of String, Exception).[Try](AddressOf TryFunction)
        Assert.AreEqual(exn, result.FailedWith().First())
    End Sub

    Private Function TryFunction() As String
        Throw exn
    End Function

    <Test>
    Public Sub TryWillReturnValue()
        Dim result As Result(Of String, Exception) = ErrorHandling.Result(Of String, Exception).[Try](AddressOf ReturnHelloWorld)
        Assert.AreEqual("hello world", result.SucceededWith())
    End Sub

    Private Function ReturnHelloWorld() As String
        Return "hello world"
    End Function
End Class

<TestFixture>
Public Class SimpleValidation
    <Test>
    Public Sub CanCreateSuccess()
        Dim request As New Request()
        request.Name = "Steffen"
        request.EMail = "mail@support.com"
        Dim result As Result(Of Request, String) = Validation.ValidateInput(request)
        Assert.AreEqual(request, result.SucceededWith())
    End Sub
End Class

<TestFixture>
Public Class SimplePatternMatching
    Private request As Request
    <Test>
    Public Sub CanMatchSuccess()
        request = New Request()
        request.Name = "Steffen"
        request.EMail = "mail@support.com"
        Dim result As Result(Of Request, String) = Validation.ValidateInput(request)
        result.Match(AddressOf CanMatchSuccessEqualsRequest, AddressOf ThrowWrongMatchCaseFromFailure)
    End Sub

    Private Sub ThrowWrongMatchCaseFromFailure(obj As FSharpList(Of String))
        Throw New Exception("wrong match case")
    End Sub
    Private Sub ThrowWrongMatchCaseFromSuccess(x As Request, obj As FSharpList(Of String))
        Throw New Exception("wrong match case")
    End Sub

    Private Sub CanMatchSuccessEqualsRequest(x As Request, msgs As FSharpList(Of String))
        Assert.AreEqual(request, x)
    End Sub

    <Test>
    Public Sub CanMatchFailure()
        request = New Request()
        request.Name = "Steffen"
        request.EMail = ""
        Dim result As Result(Of Request, String) = Validation.ValidateInput(request)
        result.Match(AddressOf ThrowWrongMatchCaseFromSuccess, AddressOf EmailMustNotBeBlank)
    End Sub

    Private Sub EmailMustNotBeBlank(msgs As FSharpList(Of String))
        Assert.AreEqual("Email must not be blank", msgs(0))
    End Sub
End Class

<TestFixture>
Public Class SimpleEitherPatternMatching
    Private request As Request

    <Test>
    Public Sub CanMatchSuccess()
        request = New Request()
        request.Name = "Steffen"
        request.EMail = "mail@support.com"
        Dim result As Object = Validation.ValidateInput(request).Either(AddressOf ReturnRequest, AddressOf ThrowWrongMatchCaseFromFailure)
        Assert.AreEqual(request, result)
    End Sub

    Private Function ReturnRequest(x As Request, msgs As FSharpList(Of String)) As Request
        Return x
    End Function

    Private Function ThrowWrongMatchCaseFromFailure(arg As FSharpList(Of String)) As Object
        Throw New Exception("wrong match case")
    End Function

    Private Function ThrowWrongMatchCaseFromSuccess(x As Request, obj As FSharpList(Of String)) As String
        Throw New Exception("wrong match case")
    End Function
    <Test>
    Public Sub CanMatchFailure()
        request = New Request()
        request.Name = "Steffen"
        request.EMail = ""
        Dim result As String = Validation.ValidateInput(request).Either(AddressOf ThrowWrongMatchCaseFromSuccess, AddressOf ReturnFirstMessage)
        Assert.AreEqual("Email must not be blank", result)
    End Sub

    Private Function ReturnFirstMessage(msgs As FSharpList(Of String)) As String
        Return msgs(0)
    End Function
End Class
