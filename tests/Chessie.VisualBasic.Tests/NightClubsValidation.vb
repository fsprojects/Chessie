#If Not __MonoCS__ Then
Imports Chessie.ErrorHandling
Imports Chessie.ErrorHandling.CSharp
Imports NUnit.Framework

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
    Public Property Age() As Integer
    Public Property Clothes() As List(Of String)
    Public Property Sobriety() As Sobriety

    Public Sub New(gender As Gender, age As Integer, clothes As List(Of String), sobriety As Sobriety)
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
        If {Sobriety.Drunk, Sobriety.Paralytic, Sobriety.Unconscious}.Contains(p.Sobriety) Then
            Return Result(Of Person, String).FailWith("Sober up!")
        End If
        Return Result(Of Person, String).Succeed(p)
    End Function
End Class

Class ClubbedToDeath
    Public Shared Function CostToEnter(p As Person) As Result(Of Decimal, String)
        Return From a In Club.CheckAge(p) From b In Club.CheckClothes(a) From c In Club.CheckSobriety(b) Select If(c.Gender = Gender.Female, 0D, 5D)
    End Function
End Class

<TestFixture>
Class Test1
    <Test>
    Public Sub Part1()
        Dim dave = New Person(Gender.Male, 41, New List(Of String)({"Tie", "Jeans"}), Sobriety.Sober)
        Dim costDave = ClubbedToDeath.CostToEnter(dave)
        Assert.AreEqual("Too old!", costDave.FailedWith().First())

        Dim ken = New Person(Gender.Male, 28, New List(Of String)({"Tie", "Shirt"}), Sobriety.Tipsy)
        Dim costKen = ClubbedToDeath.CostToEnter(ken)
        Assert.AreEqual(5D, costKen.SucceededWith())

        Dim ruby = New Person(Gender.Female, 25, New List(Of String)({"High heels"}), Sobriety.Tipsy)
        Dim costRuby = ClubbedToDeath.CostToEnter(ruby)
        costRuby.Match(Sub(x, msgs) Assert.AreEqual(0D, x),
                       Sub(msgs) Assert.Fail())

        Dim ruby17 = New Person(ruby.Gender, 17, ruby.Clothes, ruby.Sobriety)
        Dim costRuby17 = ClubbedToDeath.CostToEnter(ruby17)
        Assert.AreEqual("Too young!", costRuby17.FailedWith().First())

        Dim kenUnconscious = New Person(ken.Gender, ken.Age, ken.Clothes, Sobriety.Unconscious)
        Dim costKenUnconscious = ClubbedToDeath.CostToEnter(kenUnconscious)
        costKenUnconscious.Match(Sub(x, msgs) Assert.Fail(),
                                 Sub(msgs) Assert.AreEqual("Sober up!", msgs.First()))
    End Sub
End Class

Class ClubTropicana
    Public Shared Function CostToEnter(p As Person) As Result(Of Decimal, String)
        Return From c In Club.CheckAge(p) Join x In Club.CheckClothes(p) On x Equals c Join y In Club.CheckSobriety(p) On x Equals y Select If(c.Gender = Gender.Female, 0D, 7.5D)
    End Function

    Public Shared Function CostByGender(p As Person, x As Person, y As Person) As Decimal
        Return If(p.Gender = Gender.Female, 0D, 7.5D)
    End Function
End Class

<TestFixture>
Class Test2
    <Test>
    Public Sub Part2()
        Dim daveParalytic = New Person(age:=41, clothes:=New List(Of String)({"Tie", "Shirt"}), gender:=Gender.Male, sobriety:=Sobriety.Paralytic)

        Dim costDaveParalytic = ClubTropicana.CostToEnter(daveParalytic)

        costDaveParalytic.Match(Sub(x, msgs) Assert.Fail(),
                                Sub(errs) Assert.That(errs.ToList(), [Is].EquivalentTo({"Too old!", "Sober up!"})))
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
        Return New List(Of Func(Of Person, Result(Of Person, String)))({
            AddressOf CheckGender,
            AddressOf Club.CheckAge,
            AddressOf Club.CheckClothes,
            AddressOf Club.CheckSobriety
        }).[Select](Function(check) check(p)).Collect().[Select](Function(x) x(0).Age + 1.5D)
    End Function
End Class

<TestFixture>
Class Test3
    <Test>
    Public Sub Part3()
        Dim person = New Person(gender:=Gender.Male, age:=59, clothes:=New List(Of String)({"Jeans"}), sobriety:=Sobriety.Paralytic)
        Dim cost = GayBar.CostToEnter(person)
        cost.Match(Sub(x, msgs) Assert.Fail(),
                   Sub(errs) Assert.That(errs, [Is].EquivalentTo({"Too old!", "Smarten up!", "Sober up!"})))
    End Sub
End Class
#End If