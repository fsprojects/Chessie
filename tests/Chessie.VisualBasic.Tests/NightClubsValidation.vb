
Imports Chessie.ErrorHandling
Imports Chessie.ErrorHandling.CSharp
Imports Chessie.ErrorHandling.CSharp.ResultExtensions
Imports System.Linq
Imports NUnit.Framework
Imports Microsoft.FSharp.Collections

' originally from https://github.com/fsprojects/fsharpx/blob/master/tests/FSharpx.CSharpTests/ValidationExample.cs

Enum Sobriety
    Sober
    Tipsy
    Drunk
    Paralytic
    Unconscious
End Enum
Enum Gender
    Male
    Female
End Enum

Class Person
    Public Property Gender() As Gender
        Get
            Return m_Gender
        End Get
        Private Set
            m_Gender = Value
        End Set
    End Property
    Private m_Gender As Gender
    Public Property Age() As Integer
        Get
            Return m_Age
        End Get
        Private Set
            m_Age = Value
        End Set
    End Property
    Private m_Age As Integer
    Public Property Clothes() As String()
        Get
            Return m_Clothes
        End Get
        Private Set
            m_Clothes = Value
        End Set
    End Property
    Private m_Clothes As String()
    Public Property Sobriety() As Sobriety
        Get
            Return m_Sobriety
        End Get
        Private Set
            m_Sobriety = Value
        End Set
    End Property
    Private m_Sobriety As Sobriety

    Public Sub New(gender As Gender, age As Integer, clothes As String(), sobriety As Sobriety)
        Me.Gender = gender
        Me.Age = age
        Me.Clothes = clothes
        Me.Sobriety = sobriety
    End Sub
End Class

Class Club
    Public Shared Function CheckAge(p As Person) As Result(Of Person, String)
        If p.Age < 18 Then
            Return Result(Of Person, String).FailWith("Too young!")
        End If
        If p.Age > 40 Then
            Return Result(Of Person, String).FailWith("Too old!")
        End If
        Return Result(Of Person, String).Succeed(p)
    End Function

    Public Shared Function CheckClothes(p As Person) As Result(Of Person, String)
        If p.Gender = Gender.Male AndAlso Not p.Clothes.Contains("Tie") Then
            Return Result(Of Person, String).FailWith("Smarten up!")
        End If
        If p.Gender = Gender.Female AndAlso p.Clothes.Contains("Trainers") Then
            Return Result(Of Person, String).FailWith("Wear high heels!")
        End If
        Return Result(Of Person, String).Succeed(p)
    End Function

    Public Shared Function CheckSobriety(p As Person) As Result(Of Person, String)
        Select Case p.Sobriety
            Case Sobriety.Drunk, Sobriety.Paralytic, Sobriety.Unconscious
                Return Result(Of Person, String).FailWith("Sober up!")
            Case Else
                Return Result(Of Person, String).Succeed(p)
        End Select
    End Function
End Class

Class ClubbedToDeath
    Public Shared Function CostToEnter(p As Person) As Result(Of Decimal, String)
        Return Club.CheckAge(p).SelectMany(AddressOf Club.CheckClothes).SelectMany(AddressOf Club.CheckSobriety).Select(AddressOf ReturnCostFromPerson)
    End Function

    Private Shared Function ReturnCostFromPerson(c As Person) As Decimal
        Return If(c.Gender = Gender.Female, 0D, 5D)
    End Function
End Class

<TestFixture>
Class Test1
    <Test>
    Public Sub Part1()
        Dim clothes As String() = {
                                      "Tie",
                                      "Jeans"
                                  }
        Dim Dave As New Person(Gender.Male, 41, clothes, Sobriety.Sober)
        Dim costDave As Result(Of Decimal, String) = ClubbedToDeath.CostToEnter(Dave)
        Assert.AreEqual("Too old!", costDave.FailedWith().First())

        Dim kenclothes As String() = {
                                      "Tie",
                                      "Shirt"
                                  }
        Dim Ken As New Person(Gender.Male, 28, kenclothes, Sobriety.Tipsy)
        Dim costKen As Result(Of Decimal, String) = ClubbedToDeath.CostToEnter(Ken)
        Assert.AreEqual(5D, costKen.SucceededWith())

        Dim rubyclothes As String() = {
                                      "High heels"
                                  }
        Dim Ruby As New Person(Gender.Female, 25, rubyclothes, Sobriety.Tipsy)
        Dim costRuby As Result(Of Decimal, String) = ClubbedToDeath.CostToEnter(Ruby)
        costRuby.Match(AddressOf CostEqualsZero, AddressOf FailIfMatchFailed)

        Dim Ruby17 As New Person(Ruby.Gender, 17, Ruby.Clothes, Ruby.Sobriety)
        Dim costRuby17 As Result(Of Decimal, String) = ClubbedToDeath.CostToEnter(Ruby17)
        Assert.AreEqual("Too young!", costRuby17.FailedWith().First())

        Dim KenUnconscious As New Person(Ken.Gender, Ken.Age, Ken.Clothes, Sobriety.Unconscious)
        Dim costKenUnconscious As Result(Of Decimal, String) = ClubbedToDeath.CostToEnter(KenUnconscious)
        costKenUnconscious.Match(AddressOf FailAnySuccess, AddressOf FailedAndShouldSoberUp)
    End Sub

    Private Sub FailAnySuccess(arg1 As Decimal, arg2 As FSharpList(Of String))
        Assert.Fail()
    End Sub

    Private Sub FailedAndShouldSoberUp(msgs As FSharpList(Of String))
        Assert.AreEqual("Sober up!", msgs.First())
    End Sub

    Private Sub FailIfMatchFailed(obj As FSharpList(Of String))
        Assert.Fail()
    End Sub

    Private Sub CostEqualsZero(x As Decimal, msgs As FSharpList(Of String))
        Assert.AreEqual(0D, x)
    End Sub
End Class

Class ClubTropicana
    Public Shared Function CostToEnter(p As Person) As Result(Of Decimal, String)
        Return Club.CheckAge(p).Join(Club.CheckClothes(p), AddressOf PersonKey, AddressOf PersonKey, AddressOf ReturnAnyPerson).
                         Join(Club.CheckSobriety(p), AddressOf PersonKey, AddressOf PersonKey, AddressOf ReturnCost)
    End Function

    Private Shared Function ReturnCost(c As Person, arg2 As Person) As Decimal
        Return If(c.Gender = Gender.Female, 0D, 7.5D)
    End Function

    Private Shared Function ReturnAnyPerson(arg1 As Person, arg2 As Person) As Person
        Return arg1
    End Function

    Private Shared Function PersonKey(p As Person) As Person
        Return p
    End Function

    Public Shared Function CostByGender(p As Person, x As Person, y As Person) As Decimal
        Return If(p.Gender = Gender.Female, 0D, 7.5D)
    End Function
End Class

<TestFixture>
Class Test2
    <Test>
    Public Sub Part2()
        Dim clothes As String() = {
                                      "Tie",
                                      "Shirt"
                                  }
        Dim daveParalytic As New Person(Gender.Male, 41, clothes, Sobriety.Paralytic)

        Dim costDaveParalytic As Result(Of Decimal, String) = ClubTropicana.CostToEnter(daveParalytic)

        costDaveParalytic.Match(AddressOf FailAnySuccess, AddressOf Part2FailsWithOldParalitic)
    End Sub

    Private Sub Part2FailsWithOldParalitic(errs As FSharpList(Of String))
        Dim expected As String() = {"Too old!", "Sober up!"}
        Assert.That(errs.ToList(), [Is].EquivalentTo(expected))
    End Sub

    Private Sub FailAnySuccess(x As Decimal, msgs As FSharpList(Of String))
        Assert.Fail()
    End Sub
End Class

Class GayBar
    Public Shared Function CheckGender(p As Person) As Result(Of Person, String)
        If p.Gender = Gender.Male Then
            Return Result(Of Person, String).Succeed(p)
        End If
        Return Result(Of Person, String).FailWith("Men only")
    End Function

    Public Shared Function CostToEnter(p As Person) As Result(Of Decimal, String)
        Dim collection As Func(Of Person, Result(Of Person, String))() = {
                                            AddressOf CheckGender,
                                            AddressOf Club.CheckAge,
                                            AddressOf Club.CheckClothes,
                                            AddressOf Club.CheckSobriety
                                        }
        Return New List(Of Func(Of Person, Result(Of Person, String)))(collection).[Select](AddressOf New Lambda(p).CheckPerson).Collect().[Select](AddressOf CostByAge)
    End Function
    Private Class Lambda
        Private p As Person
        Public Sub New(p As Person)
            Me.p = p
        End Sub

        Public Function CheckPerson(arg1 As Func(Of Person, Result(Of Person, String))) As Result(Of Person, String)
            Return arg1.Invoke(p)
        End Function
    End Class
    Private Shared Function CostByAge(x As FSharpList(Of Person)) As Decimal

        Return x(0).Age + 1.5D
    End Function
End Class

<TestFixture>
Class Test3
    <Test>
    Public Sub Part3()
        Dim clothes As String() = {"Jeans"}
        Dim person As New Person(Gender.Male, 59, clothes, Sobriety.Paralytic)
        Dim cost As Result(Of Decimal, String) = GayBar.CostToEnter(person)
        cost.Match(AddressOf FailAnySuccess, AddressOf FailsWithOldSmartenSober)
    End Sub

    Private Sub FailsWithOldSmartenSober(errs As FSharpList(Of String))
        Dim expected As String() = {"Too old!", "Smarten up!", "Sober up!"}
        Assert.That(errs, [Is].EquivalentTo(expected))
    End Sub

    Private Sub FailAnySuccess(x As Decimal, msgs As FSharpList(Of String))
        Assert.Fail()
    End Sub
End Class
