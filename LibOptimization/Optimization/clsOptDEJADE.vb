﻿Imports LibOptimization.Util
Imports LibOptimization.MathUtil

Namespace Optimization
    ''' <summary>
    ''' Adaptive Differential Evolution Algorithm
    ''' JADE
    ''' </summary>
    ''' <remarks>
    ''' Features:
    '''  -Derivative free optimization algorithm.
    '''  -similar to GA algorithm.
    ''' 
    ''' Refference:
    '''  [1]Z.-H. Zhan, J. Zhang, Y. Li, and H. Chung, “JADE: Adaptive Differential Evolution With Optional External Archive” IEEE Trans. Systems, Man, and Cybernetics-Part B, vol. 39, no. 6, pp. 1362-1381, Dec. 2009. 
    '''  [2]阪井 節子,高濱 徹行, "パラメータの相関を考慮した適応型差分進化アルゴリズムJADEの改良", 不確実性の下での数理モデルとその周辺 Mathematical Model under Uncertainty and Related Topics RIMS 研究集会報告集
    '''     (JADEの原著論文が見れなかったので[2]文献を参照)
    ''' 
    ''' Implment:
    ''' N.Tomi(tomi.nori+github at gmail.com)
    ''' </remarks>
    Public Class clsOptDEJADE : Inherits absOptimization
#Region "Member"
        '----------------------------------------------------------------
        'Common parameters
        '----------------------------------------------------------------
        ''' <summary>epsilon(Default:1e-8) for Criterion</summary>
        Public Property EPS As Double = 0.000000001

        ''' <summary>Use criterion</summary>
        Public Property IsUseCriterion As Boolean = True

        ''' <summary>
        ''' higher N percentage particles are finished at the time of same evaluate value.
        ''' This parameter is valid is when IsUseCriterion is true.
        ''' </summary>
        Public Property HigherNPercent As Double = 0.8 'for IsCriterion()
        Private HigherNPercentIndex As Integer = 0 'for IsCriterion())

        ''' <summary>
        ''' Max iteration count
        ''' </summary>
        Public Property Iteration As Integer = 50000

        ''' <summary>Upper bound(limit solution space)</summary>
        Public Property UpperBounds As Double() = Nothing

        ''' <summary>Lower bound(limit solution space)</summary>
        Public Property LowerBounds As Double() = Nothing

        '----------------------------------------------------------------
        'DE parameters
        '----------------------------------------------------------------
        ''' <summary>
        ''' Population Size(Default:100)
        ''' </summary>
        Public Property PopulationSize As Integer = 100

        ''' <summary>population</summary>
        Private m_parents As New List(Of clsPoint)

        ''' <summary>Constant raio 0 to 1(Adaptive paramter for muF, muCR)(Default:0.1)</summary>
        Public Property C As Double = 0.1

        ''' <summary>Adapative cross over ratio</summary>
        Private muCR As Double = 0.5

        ''' <summary>Adapative F</summary>
        Private muF As Double = 0.5
#End Region

#Region "Constructor"
        ''' <summary>
        ''' Constructor
        ''' </summary>
        ''' <param name="ai_func">Objective Function</param>
        ''' <remarks>
        ''' </remarks>
        Public Sub New(ByVal ai_func As absObjectiveFunction)
            Me.m_func = ai_func
        End Sub
#End Region

#Region "Public"
        ''' <summary>
        ''' Init
        ''' </summary>
        ''' <remarks></remarks>
        Public Overrides Sub Init()
            Try
                'init meber varibles
                Me.m_iteration = 0
                Me.m_parents.Clear()
                Me.m_error.Clear()

                'init muF, muCR
                muCR = 0.5
                muF = 0.5

                'bound check
                If UpperBounds IsNot Nothing AndAlso LowerBounds IsNot Nothing Then
                    If UpperBounds.Length <> Me.m_func.NumberOfVariable Then
                        Throw New Exception("UpperBounds.Length is different")
                    End If
                    If LowerBounds.Length <> Me.m_func.NumberOfVariable Then
                        Throw New Exception("LowerBounds.Length is different")
                    End If
                End If

                'generate population
                For i As Integer = 0 To Me.PopulationSize - 1
                    'initialize
                    Dim temp As New List(Of Double)
                    For j As Integer = 0 To Me.m_func.NumberOfVariable - 1
                        Dim value As Double = clsUtil.GenRandomRange(Me.m_rand, -Me.InitialValueRange, Me.InitialValueRange)
                        temp.Add(value)
                    Next

                    'bound check
                    Dim tempPoint = New clsPoint(MyBase.m_func, temp)
                    If UpperBounds IsNot Nothing AndAlso LowerBounds IsNot Nothing Then
                        clsUtil.LimitSolutionSpace(tempPoint, Me.LowerBounds, Me.UpperBounds)
                    End If

                    'save point
                    Me.m_parents.Add(tempPoint)
                Next

                'add initial point
                clsUtil.SetInitialPoint(Me.m_parents, InitialPosition)

                'Sort Evaluate
                Me.m_parents.Sort()

                'Detect HigherNPercentIndex
                Me.HigherNPercentIndex = CInt(Me.m_parents.Count * Me.HigherNPercent)
                If Me.HigherNPercentIndex = Me.m_parents.Count OrElse Me.HigherNPercentIndex >= Me.m_parents.Count Then
                    Me.HigherNPercentIndex = Me.m_parents.Count - 1
                End If

            Catch ex As Exception
                Me.m_error.SetError(True, clsError.ErrorType.ERR_INIT)
            End Try
        End Sub

        ''' <summary>
        ''' Do Iteration
        ''' </summary>
        ''' <param name="ai_iteration">Iteration count. When you set zero, use the default value.</param>
        ''' <returns>True:Stopping Criterion. False:Do not Stopping Criterion</returns>
        ''' <remarks></remarks>
        Public Overrides Function DoIteration(Optional ByVal ai_iteration As Integer = 0) As Boolean
            'Check Last Error
            If Me.IsRecentError() = True Then
                Return True
            End If

            'Do Iterate
            ai_iteration = If(ai_iteration = 0, Me.Iteration - 1, ai_iteration - 1)
            For iterate As Integer = 0 To ai_iteration
                'Sort Evaluate
                Me.m_parents.Sort()

                'check criterion
                If Me.IsUseCriterion = True Then
                    'higher N percentage particles are finished at the time of same evaluate value.
                    If clsUtil.IsCriterion(Me.EPS, Me.m_parents(0).Eval, Me.m_parents(Me.HigherNPercentIndex).Eval) Then
                        Return True
                    End If
                End If

                'Counting generation
                If Me.Iteration <= Me.m_iteration Then
                    Me.m_error.SetError(True, clsError.ErrorType.ERR_OPT_MAXITERATION)
                    Return True
                End If
                m_iteration += 1

                '--------------------------------------------------------------------------------------------
                'DE process
                '--------------------------------------------------------------------------------------------
                'Mutation and Crossover
                Dim sumF As Double = 0.0
                Dim sumFSquare As Double = 0.0
                Dim sumCR As Double = 0.0
                Dim countSuccess As Integer = 0
                For i As Integer = 0 To Me.PopulationSize - 1
                    'update F
                    Dim F As Double = 0.0
                    While True
                        F = clsUtil.CauchyRand(muF, 0.1)
                        If F < 0 Then
                            Continue While
                        End If
                        If F > 1 Then
                            F = 1.0
                        End If
                        Exit While
                    End While

                    'update CR 0 to 1
                    Dim CR As Double = clsUtil.NormRand(muCR, 0.1)
                    If CR < 0 Then
                        CR = 0.0
                    ElseIf CR > 1 Then
                        CR = 1.0
                    End If

                    'Sort Evaluate
                    m_parents.Sort()

                    'pick different parent without i
                    Dim randIndex As List(Of Integer) = clsUtil.RandomPermutaion(m_parents.Count, i)
                    Dim xi = m_parents(i)
                    Dim p1 As clsPoint = m_parents(randIndex(0))
                    Dim p2 As clsPoint = m_parents(randIndex(1))

                    'Mutation and Crossover
                    Dim child = New clsPoint(ObjectiveFunction)
                    Dim j = Random.Next() Mod ObjectiveFunction.NumberOfVariable
                    Dim D = ObjectiveFunction.NumberOfVariable - 1

                    'DE/current-to-pbest/1 for JADE Strategy. current 100p% p<-(0,1)
                    Dim p = CInt(Me.PopulationSize * Random.NextDouble()) 'range is 0 to PopulationSize
                    Dim pbest As clsPoint = Nothing
                    If p = 0 Then
                        pbest = m_parents(0) 'best
                    ElseIf p >= Me.PopulationSize Then
                        pbest = m_parents(PopulationSize - 1) 'worst
                    Else
                        pbest = m_parents(Random.Next(0, p))
                    End If

                    'crossover
                    For k = 0 To ObjectiveFunction.NumberOfVariable - 1
                        If Random.NextDouble() < CR OrElse k = D Then
                            child(j) = xi(j) + F * (pbest(j) - xi(j)) + F * (p1(j) - p2(j))
                        Else
                            child(j) = xi(k)
                        End If
                        j = (j + 1) Mod ObjectiveFunction.NumberOfVariable 'next
                    Next
                    child.ReEvaluate() 'Evaluate child

                    'Limit solution space
                    clsUtil.LimitSolutionSpace(child, Me.LowerBounds, Me.UpperBounds)

                    'Survive
                    If child.Eval < m_parents(i).Eval Then
                        'replace
                        m_parents(i) = child

                        'for adaptive parameter
                        sumF += F
                        sumFSquare += F ^ 2
                        sumCR += CR
                        countSuccess += 1
                    End If
                Next 'population iteration

                'calc muF, muCR
                If countSuccess > 0 Then
                    muCR = (1 - C) * muCR + C * (sumCR / countSuccess) '(1-c) * muCR + c * meanA(CRs)
                    muF = (1 - C) * muF + C * (sumFSquare / sumF)
                Else
                    muCR = (1 - C) * muCR
                    muF = (1 - C) * muF
                    'Console.WriteLine("muF={0}, muCR={1}", muF, muCR)
                End If
            Next

            Return False
        End Function

        ''' <summary>
        ''' Best result
        ''' </summary>
        ''' <returns>Best point class</returns>
        ''' <remarks></remarks>
        Public Overrides ReadOnly Property Result() As clsPoint
            Get
                Return Me.m_parents(0)
            End Get
        End Property

        ''' <summary>
        ''' Get recent error
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides Function IsRecentError() As Boolean
            Return Me.m_error.IsError()
        End Function

        ''' <summary>
        ''' All Result
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks>
        ''' for Debug
        ''' </remarks>
        Public Overrides ReadOnly Property Results As List(Of clsPoint)
            Get
                Return Me.m_parents
            End Get
        End Property
#End Region

#Region "Private"
#End Region
    End Class
End Namespace