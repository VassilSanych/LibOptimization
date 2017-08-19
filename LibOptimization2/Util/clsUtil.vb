﻿Imports LibOptimization2.MathUtil
Imports LibOptimization2.Optimization

Namespace Util
    ''' <summary>
    ''' common use
    ''' </summary>
    ''' <remarks></remarks>
    Public Class clsUtil
        Private Shared _callCount As UInt32 = 0

        ''' <summary>
        ''' for random seed
        ''' </summary>
        ''' <returns></returns>
        Public Shared Function GlobalCounter() As UInt32
            clsUtil._callCount = clsUtil._callCount + CType(1, UInt32)
            Return clsUtil._callCount
        End Function

        ''' <summary>
        ''' Normal Distribution
        ''' </summary>
        ''' <param name="ai_ave">Average</param>
        ''' <param name="ai_sigma2">Varianse s^2</param>
        ''' <returns></returns>
        ''' <remarks>
        ''' using Box-Muller method
        ''' </remarks>
        Public Shared Function NormRand(Optional ByVal ai_ave As Double = 0,
                                        Optional ByVal ai_sigma2 As Double = 1) As Double
            Dim x As Double = clsRandomXorshiftSingleton.GetInstance().NextDouble()
            Dim y As Double = clsRandomXorshiftSingleton.GetInstance().NextDouble()

            Dim c As Double = Math.Sqrt(-2.0 * Math.Log(x))
            If (0.5 - clsRandomXorshiftSingleton.GetInstance().NextDouble() > 0.0) Then
                Return c * Math.Sin(2.0 * Math.PI * y) * ai_sigma2 + ai_ave
            Else
                Return c * Math.Cos(2.0 * Math.PI * y) * ai_sigma2 + ai_ave
            End If
        End Function

        ''' <summary>
        ''' Cauchy Distribution
        ''' </summary>
        ''' <param name="ai_mu">default:0</param>
        ''' <param name="ai_gamma">default:1</param>
        ''' <returns></returns>
        ''' <remarks>
        ''' http://www.sat.t.u-tokyo.ac.jp/~omi/random_variables_generation.html#Cauchy
        ''' </remarks>
        Public Shared Function CauchyRand(Optional ByVal ai_mu As Double = 0, Optional ByVal ai_gamma As Double = 1) As Double
            Return ai_mu + ai_gamma * Math.Tan(Math.PI * (clsRandomXorshiftSingleton.GetInstance().NextDouble() - 0.5))
        End Function

        ''' <summary>
        ''' Generate Random permutation
        ''' </summary>
        ''' <param name="ai_max">0 to ai_max-1</param>
        ''' <param name="ai_removeIndex">RemoveIndex</param>
        ''' <returns></returns>
        Public Shared Function RandomPermutaion(ByVal ai_max As Integer, Optional ByVal ai_removeIndex As Integer = -1) As List(Of Integer)
            Return RandomPermutaion(0, ai_max, ai_removeIndex)
        End Function

        ''' <summary>
        ''' Generate Random permutation with range (ai_min to ai_max-1)
        ''' </summary>
        ''' <param name="ai_min">start value</param>
        ''' <param name="ai_max">ai_max-1</param>
        ''' <param name="ai_removeIndex">RemoveIndex -1 is invalid</param>
        ''' <returns></returns>
        Public Shared Function RandomPermutaion(ByVal ai_min As Integer, ByVal ai_max As Integer, ByVal ai_removeIndex As Integer) As List(Of Integer)
            Dim nLength As Integer = ai_max - ai_min
            If nLength = 0 OrElse nLength < 0 Then
                Return New List(Of Integer)
            End If

            Dim ary As New List(Of Integer)(CInt(nLength * 1.5))
            If ai_removeIndex <= -1 Then
                For ii As Integer = ai_min To ai_max - 1
                    ary.Add(ii)
                Next
            Else
                For ii As Integer = ai_min To ai_max - 1
                    If ai_removeIndex <> ii Then
                        ary.Add(ii)
                    End If
                Next
            End If

            'Fisher–Yates shuffle / フィッシャー - イェーツのシャッフル
            Dim n As Integer = ary.Count
            While n > 1
                n -= 1
                Dim k As Integer = clsRandomXorshiftSingleton.GetInstance().Next(0, n + 1)
                Dim tmp As Integer = ary(k)
                ary(k) = ary(n)
                ary(n) = tmp
            End While
            Return ary
        End Function

        ''' <summary>
        ''' For Debug
        ''' </summary>
        ''' <param name="ai_results"></param>
        ''' <remarks></remarks>
        Public Shared Sub DebugValue(ByVal ai_results As List(Of clsPoint))
            If ai_results Is Nothing OrElse ai_results.Count = 0 Then
                Return
            End If
            For i As Integer = 0 To ai_results.Count - 1
                Console.WriteLine("Eval          :" & String.Format("{0}", ai_results(i).Eval))
            Next
            Console.WriteLine()
        End Sub

        ''' <summary>
        ''' For Debug
        ''' </summary>
        ''' <param name="bestResult"></param>
        Public Shared Sub DebugValue(ByVal bestResult As clsPoint)
            Console.WriteLine("Eval          :" & String.Format("{0}", bestResult.Eval))
        End Sub

        ''' <summary>
        ''' Check Criterion
        ''' </summary>
        ''' <param name="ai_eps">EPS</param>
        ''' <param name="ai_comparisonA"></param>
        ''' <param name="ai_comparisonB"></param>
        ''' <param name="ai_tiny"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function IsCriterion(ByVal ai_eps As Double,
                                           ByVal ai_comparisonA As clsPoint, ByVal ai_comparisonB As clsPoint,
                                           Optional ByVal ai_tiny As Double = 0.0000000000001) As Boolean
            Return clsUtil.IsCriterion(ai_eps, ai_comparisonA.Eval, ai_comparisonB.Eval, ai_tiny)
        End Function

        ''' <summary>
        ''' Check Criterion
        ''' </summary>
        ''' <param name="ai_eps">EPS</param>
        ''' <param name="ai_comparisonA"></param>
        ''' <param name="ai_comparisonB"></param>
        ''' <param name="ai_tiny"></param>
        ''' <returns></returns>
        ''' <remarks>
        ''' Reffrence:
        ''' William H. Press, Saul A. Teukolsky, William T. Vetterling, Brian P. Flannery,
        ''' "NUMRICAL RECIPIES 3rd Edition: The Art of Scientific Computing", Cambridge University Press 2007, pp505-506
        ''' </remarks>
        Public Shared Function IsCriterion(ByVal ai_eps As Double,
                                           ByVal ai_comparisonA As Double, ByVal ai_comparisonB As Double,
                                           Optional ByVal ai_tiny As Double = 0.0000000000001) As Boolean
            'check division by zero
            Dim denominator = (Math.Abs(ai_comparisonB) + Math.Abs(ai_comparisonA)) + ai_tiny
            If denominator = 0 Then
                Return True
            End If

            'check criterion
            Dim temp = 2.0 * Math.Abs(ai_comparisonB - ai_comparisonA) / denominator
            If temp < ai_eps Then
                Return True
            Else
                Return False
            End If
        End Function

        ''' <summary>
        ''' Random generator helper
        ''' </summary>
        ''' <param name="oRand"></param>
        ''' <param name="ai_min"></param>
        ''' <param name="ai_max"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function GenRandomRange(ByVal oRand As System.Random, ByVal ai_min As Double, ByVal ai_max As Double) As Double
            Return Math.Abs(ai_max - ai_min) * oRand.NextDouble() + ai_min
        End Function

        ''' <summary>
        ''' to csv
        ''' </summary>
        ''' <param name="arP"></param>
        ''' <remarks></remarks>
        Public Shared Sub ToCSV(ByVal arP As clsPoint)
            For Each p In arP
                Console.Write("{0},", p)
            Next
            Console.WriteLine("")
        End Sub

        ''' <summary>
        ''' to csv
        ''' </summary>
        ''' <param name="arP"></param>
        ''' <remarks></remarks>
        Public Shared Sub ToCSV(ByVal arP As List(Of clsPoint))
            For Each p In arP
                clsUtil.ToCSV(p)
            Next
            Console.WriteLine("")
        End Sub

        ''' <summary>
        ''' eval output for debug
        ''' </summary>
        ''' <param name="arP"></param>
        ''' <remarks></remarks>
        Public Shared Sub ToEvalList(ByVal arP As List(Of clsPoint))
            For Each p In arP
                Console.WriteLine("{0}", p.Eval)
            Next
        End Sub

        ''' <summary>
        ''' Eval list
        ''' </summary>
        ''' <param name="arP"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function GetSortedEvalList(ByVal arP As List(Of clsPoint)) As List(Of clsEval)
            Dim sortedEvalList = New List(Of clsEval)
            For i = 0 To arP.Count - 1
                sortedEvalList.Add(New clsEval(i, arP(i).Eval))
            Next
            sortedEvalList.Sort()
            Return sortedEvalList
        End Function

        ''' <summary>
        ''' Best clsPoint
        ''' </summary>
        ''' <param name="ai_points"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function GetBestPoint(ByVal ai_points As List(Of clsPoint), Optional ByVal isCopy As Boolean = False) As clsPoint
            If ai_points Is Nothing Then
                Return Nothing
            ElseIf ai_points.Count = 0 Then
                Return Nothing
            ElseIf ai_points.Count = 1 Then
                Return ai_points(0)
            End If

            Dim best = ai_points(0)
            For i As Integer = 1 To ai_points.Count - 1
                If best.Eval > ai_points(i).Eval Then
                    best = ai_points(i)
                End If
            Next

            If isCopy = False Then
                Return best
            Else
                Return best.Copy()
            End If
        End Function

        ''' <summary>
        ''' Get sorted population list by evaluate
        ''' </summary>
        ''' <param name="ai_points"></param>
        ''' <returns></returns>
        Public Shared Function GetSortedResultsByEval(ByVal ai_points As List(Of clsPoint)) As List(Of clsPoint)
            If ai_points Is Nothing Then
                Return Nothing
            ElseIf ai_points.Count = 0 Then
                Return Nothing
            ElseIf ai_points.Count = 1 Then
                Dim temp As New List(Of clsPoint)
                temp.Add(ai_points(0))
                Return temp
            End If

            Dim sortedPoints As New List(Of clsPoint)
            Dim sortedEvalList = clsUtil.GetSortedEvalList(ai_points)
            For i As Integer = 0 To sortedEvalList.Count - 1
                sortedPoints.Add(ai_points(sortedEvalList(i).Index))
            Next
            Return sortedPoints
        End Function

        ''' <summary>
        ''' Select Parent
        ''' </summary>
        ''' <param name="ai_population"></param>
        ''' <param name="ai_parentSize"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function SelectParent(ByVal ai_population As List(Of clsPoint), ByVal ai_parentSize As Integer) As List(Of KeyValuePair(Of Integer, clsPoint))
            Dim ret As New List(Of KeyValuePair(Of Integer, clsPoint))

            'Index
            Dim randIndex As List(Of Integer) = clsUtil.RandomPermutaion(ai_population.Count)

            'PickParents
            For i As Integer = 0 To ai_parentSize - 1
                ret.Add(New KeyValuePair(Of Integer, clsPoint)(randIndex(i), ai_population(randIndex(i))))
            Next

            Return ret
        End Function

        ''' <summary>
        ''' calc length from each points
        ''' </summary>
        ''' <param name="points"></param>
        ''' <returns></returns>
        Public Shared Function IsExistZeroLength(ByVal points As List(Of clsPoint)) As Boolean
            Dim isCanCrossover As Boolean = True
            Dim vec As clsEasyVector = Nothing
            For i As Integer = 0 To points.Count - 2
                vec = points(i) - points(i + 1)
                If vec.NormL1() = 0 Then
                    Return True
                End If
            Next
            vec = points(points.Count - 1) - points(0)
            If vec.NormL1() = 0 Then
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Overflow check for debug
        ''' </summary>
        ''' <param name="p"></param>
        ''' <returns></returns>
        Public Shared Function CheckOverflow(ByVal p As List(Of clsPoint)) As Boolean
            For Each temp In p
                For Each v In temp
                    If Double.IsInfinity(v) = True Then
                        Return True
                    End If
                    If Double.IsNaN(v) = True Then
                        Return True
                    End If
                    If Double.IsNegativeInfinity(v) = True Then
                        Return True
                    End If
                    If Double.IsPositiveInfinity(v) = True Then
                        Return True
                    End If
                Next
            Next

            Return False
        End Function

        ''' <summary>
        ''' Debug mode
        ''' </summary>
        ''' <param name="flag"></param>
        Public Shared Sub SetDebugMode(ByVal flag As Boolean)
            'fix random permutation
        End Sub
    End Class
End Namespace