﻿'==================================================='
' IpOffice DevLinken Wrapper for DotNet programmer  '
' Author : Giulio martino                           '
' Email  : giulio.martino@voipexperts.it            '
' Date   : 2012-12-12                               '
'==================================================='
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.IO
Imports System.Threading
Imports System.Windows.Forms

Namespace DevLinkNet


    Public Class Devlink
        ''' <summary>
        ''' Set/Get Call Log Type
        ''' </summary>
        ''' <value>
        ''' Base     = Raise only CallLog_Event
        ''' 
        ''' Advanced = Split CallLog_Event in Type : A,D,S and raise specific Event
        '''            Call A : Raise event CallLog_Event_A
        '''            Call D : Raise event CallLog_Event_D
        '''            Call S : Raise event CallLog_Event_S 
        ''' 
        ''' BaseAndAdvanced = Raise Base and Advanced Events
        ''' </value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Property CallLogEventType As CallLogType = CallLogType.Base

        ''' <summary>
        ''' Get connection Status
        ''' </summary>
        ''' <value>boolean</value>
        ''' <returns>
        ''' True = Connection is open
        ''' False = Connection is closed
        ''' </returns>
        ''' <remarks></remarks>
        ReadOnly Property IsConnect As Boolean
            Get
                IsConnect = bIsConnect
            End Get
        End Property

        Dim bIsConnect As Boolean = False
        Dim bIsInterrupt As Boolean = False

#Region "Definizione delle call back unmanaged"
        ' Definizione delle call back e dei delegate associati
        Friend Delegate Sub COMMSEVENT(ByVal pbxh As IntPtr, ByVal comms_state As Integer, ByVal parm1 As Integer)
        Friend Delegate Sub CALLLOGEVENT(ByVal pbxh As IntPtr, ByVal info As String)

        ' 
        Private oCallLogEvent As CALLLOGEVENT = AddressOf CallEvent
        Private oCommsEvent As COMMSEVENT = AddressOf CommEvent


        ' Definizione delle API unmanaged
        <DllImport("devlink.dll")> _
        Friend Shared Function DLOpen(ByVal pbxh As IntPtr, ByVal pbx_address As String, ByVal pbx_password As String, ByVal reserved1 As String, ByVal reserved2 As String, ByVal cb As COMMSEVENT) As Long
        End Function

        <DllImport("devlink.dll")> _
        Friend Shared Function DLClose(ByVal pbxh As IntPtr) As Long
        End Function

        <DllImport("devlink.dll")> _
        Friend Shared Function DLRegisterType2CallDeltas(ByVal pbxh As IntPtr, ByVal cb As CALLLOGEVENT) As Long
        End Function
#End Region

#Region "Definizione degli eventi pubblici"
        '' Eventi Base
        
        ''' <summary>
        ''' The COMMSEVENT callback is called by DevLink whenever the state 
        ''' of the communication with the IP Office system unit changes.
        ''' </summary>
        ''' <param name="Source"></param>
        ''' <param name="e">
        ''' pbxh        = A number used to identify the system. This is the user-supplied parameter used to 
        '''               connect to the IP Office system
        ''' 
        ''' comms_state = A number indicating the state of the communications. See CommsEvent_Enum.CommsEvent_State
        ''' 
        ''' parm1       = This value is only defined for: DEVLINK_COMMS_MISSEDPACKETS events, 
        '''               in which case it indicates the number ofpackets dropped
        ''' </param>
        ''' <remarks></remarks>
        Public Event Comms_Event(Source As Object, e As CommsEvents_Parameter.CommEvent)

        ' Evento base. Riproduce fedelmente la call back originale
        ''' <summary>
        ''' The CALLLOGEVENT callback is called by DevLink to deliver a real-time (Delta2) event.
        ''' </summary>
        ''' <param name="source">Sender object</param>
        ''' <param name="e"> Argument type CallLogEvent_Parameter.CallLog_Base_Parameter
        ''' IpPbx = A number used to identify the system. This is the user-supplied parameter used to connect to the IP Office system
        ''' LogInfo = Text string containing the event.
        ''' Calls in IP Office are modelled as being a communications line between two end-points, called A and B respectively. An A
        ''' end is always present, but a B end may or may not be present, depending on the state of the call (A and B are typically
        ''' extensions or trunks, but they may also be connected to the voicemail system or parked).
        ''' Three different types of real-time events are generated by DevLink. These are used to track the call throughout its'
        ''' lifetime:
        ''' 
        ''' S events
        ''' S events give information on the status of a call. S events are generated when calls are first created, answered, or
        '''   the status of a device involved in the call changes.
        ''' 
        ''' D events
        ''' D events are generated when the call is completed or abandoned. They indicate that the call no longer exists.
        ''' 
        ''' A events
        ''' A events are generated when one end of a call is connected to a line (such as an ISDN, QSig or VoIP line) and the IP
        '''   Office decides to swap the A end and the B end of the call. Examples of when this may happen include:
        '''          When a parked party hangs up
        '''          When an outgoing call is transferred
        '''          When a call is un-parked
        '''         
        ''' </param>
        ''' <remarks></remarks>
        Public Event CallLog_Event(source As Object, e As CallLogEvent_Parameter.CallLog_Base_Parameter)


        ' Eventi in modalita avanzata
        ''' <summary>
        ''' A events indicate that the call ends have been swapped. This occurs, for example, when the originating extension
        ''' unparks an external call. The format of an A event is very similar to that for a D event:
        ''' </summary>
        ''' <param name="source">Sender Object</param>
        ''' <param name="e"> Argument type CallLogEvent_Parameter.CallLog_A_Parameter
        ''' IpPbx = A number used to identify the system. This is the user-supplied parameter used to connect to the IP Office system
        ''' LogInfo = CallLogEvent_Type.CallLogEvent_A
        ''' </param>
        ''' <remarks></remarks>
        Public Event CallLog_Event_A(source As Object, e As CallLogEvent_Parameter.CallLog_A_Parameter)

        ''' <summary>
        ''' S events are sent whenever a call is first created, and whenever any part of the call changes state.
        ''' </summary>
        ''' <param name="source">Sender Object</param>
        ''' <param name="e"></param>
        ''' IpPbx = A number used to identify the system. This is the user-supplied parameter used to connect to the IP Office system
        ''' LogInfo = CallLogEvent_Type.CallLogEvent_S
        ''' <remarks></remarks>
        Public Event CallLog_Event_S(source As Object, e As CallLogEvent_Parameter.CallLog_S_Parameter)

        ''' <summary>
        ''' D events signify that the call is deleted.
        ''' </summary>
        ''' <param name="source">Sender Object</param>
        ''' <param name="e">
        ''' IpPbx = A number used to identify the system. This is the user-supplied parameter used to connect to the IP Office system
        ''' LogInfo = CallLogEvent_Type.CallLogEvent_D
        ''' </param>
        ''' <remarks></remarks>
        Public Event CallLog_Event_D(source As Object, e As CallLogEvent_Parameter.CallLog_D_Parameter)


        ''' <summary>
        ''' Monitor connection status
        ''' </summary>
        ''' <param name="source">Sender Object</param>
        ''' <param name="e">
        ''' </param>
        ''' <remarks></remarks>
        Public Event ConnectionStatus(source As Object, e As Connection_Parameter.Connection_Status_Paramenter)
#End Region

        ''' <summary>
        ''' Connect to Ip Office System
        ''' </summary>
        ''' <param name="idPbx">
        ''' A number used to identify the system. This is a user-supplied parameter that must remain consistent across all calls
        ''' to DevLink.
        ''' </param>
        ''' <param name="PbxAddress">
        ''' The IP address of the IP Office system
        ''' </param>
        ''' <param name="pbxPassword">
        ''' The password of the IP Office system.
        ''' </param>
        ''' <remarks></remarks>
        Public Sub StartMonitor(idPbx As Integer, PbxAddress As String, pbxPassword As String)
            Dim PConn As Connection_Parameter.Connection_Status_Paramenter = New Connection_Parameter.Connection_Status_Paramenter

            Try
                PConn.IdPbx = idPbx
                PConn.ErrorLevel = Connection_Enum.ErrorLevel.info
                PConn.StatusMessage = "Connection in progress..."
                PConn.Status = CommsEvent_Enum.CommsEvent_State.DEVLINK_COMMS_OPERATIONAL

                RaiseEvent ConnectionStatus(Me, PConn)
                Dim iRet As Long = DLOpen(New IntPtr(idPbx), PbxAddress, pbxPassword, Nothing, Nothing, oCommsEvent)

                If iRet = 0 Then
                    PConn.StatusMessage = "IpOffice is present!!"
                    RaiseEvent ConnectionStatus(Me, PConn)
                Else
                    PConn.StatusMessage = "IpOffice is present ?? "
                    RaiseEvent ConnectionStatus(Me, PConn)
                    'Exit Sub
                End If
                PConn.StatusMessage = "Wait connection response..."
                RaiseEvent ConnectionStatus(Me, PConn)
                Do
                    Thread.Sleep(100)
                    Application.DoEvents()

                Loop While Not bIsConnect And Not bIsInterrupt

                iRet = DLRegisterType2CallDeltas(New IntPtr(idPbx), oCallLogEvent)

                If iRet > 0 Then
                    If iRet = 1 Then
                        PConn.ErrorLevel = Connection_Enum.ErrorLevel.warning
                        PConn.StatusMessage = "Error!! Check ip office IP Address or LAN Connection!!!"
                        RaiseEvent ConnectionStatus(Me, PConn)
                    End If
                    If iRet = 2 Then
                        PConn.ErrorLevel = Connection_Enum.ErrorLevel.warning
                        PConn.StatusMessage = "Error!! CTI License not found...!!!"
                        RaiseEvent ConnectionStatus(Me, PConn)
                    End If

                    If iRet > 2 Then
                        PConn.ErrorLevel = Connection_Enum.ErrorLevel.ignore
                        PConn.StatusMessage = "Bhoo!! " & iRet.ToString
                        RaiseEvent ConnectionStatus(Me, PConn)
                    End If
                    'Exit Sub
                Else
                    PConn.StatusMessage = "DLRegisterType2CallDeltas is OK"
                    RaiseEvent ConnectionStatus(Me, PConn)
                End If

            Catch ex As Exception
                'DebugMode()
                'Throw
            Finally
                PConn = Nothing

            End Try
        End Sub
        <Conditional("DEBUG")>
        Shared Sub DebugMode()
            If Not Debugger.IsAttached Then
                Debugger.Launch()
            End If

            Debugger.Break()
        End Sub

        ''' <summary>
        ''' Disconnect from an IP Office system.
        ''' </summary>
        ''' <param name="idPbx">
        ''' A number used to identify the system. This is the user-supplied parameter used to connect to DevLink
        ''' </param>
        ''' <remarks></remarks>
        Public Sub StopMonitor(idPbx As Integer)
            Dim e As Connection_Parameter.Connection_Status_Paramenter = New Connection_Parameter.Connection_Status_Paramenter

            bIsInterrupt = True
            Try
                Dim iRet As Long = DLClose(New IntPtr(idPbx))
                bIsConnect = False

                e.IdPbx = idPbx
                e.ErrorLevel = Connection_Enum.ErrorLevel.info
                e.StatusMessage = "Connection Closed"

                RaiseEvent ConnectionStatus(Me, e)
            Catch ex As Exception
                'MessageBox.Show(ex.StackTrace.ToString())
            End Try


        End Sub




        Private Sub CommEvent(ByVal pbxh As IntPtr, ByVal comms_state As Integer, ByVal parm1 As Integer)
            Dim e As CommsEvents_Parameter.CommEvent = New CommsEvents_Parameter.CommEvent
            Dim e1 As Connection_Parameter.Connection_Status_Paramenter = New Connection_Parameter.Connection_Status_Paramenter

            Try

                e.IdPbx = pbxh
                e.comm_state = comms_state
                e.parm1 = parm1

                e1.IdPbx = pbxh
                e1.ErrorLevel = Connection_Enum.ErrorLevel.info
                e1.StatusMessage = ""
                e1.Status = comms_state


                RaiseEvent ConnectionStatus(Me, e1)

                bIsConnect = True
                RaiseEvent Comms_Event(Me, e)

            Catch ex As Exception
                'DebugMode()
                'Throw
            End Try


        End Sub

        Private Sub CallEvent(ByVal pbxh As IntPtr, ByVal info As String)
            Dim MyThread As Thread
            Dim cParameter As CallLogEvent_Parameter.CallLog_Base_Parameter = New CallLogEvent_Parameter.CallLog_Base_Parameter


            Try
                cParameter.IdPbx = pbxh
                cParameter.LogInfo = info

                MyThread = New Thread(AddressOf CreateCallLogEvent)
                MyThread.Start(cParameter)

            Catch ex As Exception
                'DebugMode()
                'Throw
            End Try

        End Sub

        Private Sub CreateCallLogEvent(Param As CallLogEvent_Parameter.CallLog_Base_Parameter)
            Try

            
            If CallLogEventType = CallLogType.Base Or CallLogEventType = CallLogType.BaseAndAdvanced Then
                RaiseEvent CallLog_Event(Me, Param)
            End If
            If CallLogEventType = CallLogType.Advavanced Or CallLogEventType = CallLogType.BaseAndAdvanced Then
                Dim TmpStr As String

                TmpStr = Param.LogInfo.Substring(0, 6)

                Select Case UCase(TmpStr)
                    Case Is = "CALL:A"
                        CallLogAdvancedEvent_A(Param)
                    Case Is = "CALL:D"
                        CallLogAdvancedEvent_D(Param)
                    Case Is = "CALL:S"
                        CallLogAdvancedEvent_S(Param)
                End Select

            End If
            Catch ex As Exception
                'MessageBox.Show(ex.StackTrace.ToString())
            End Try

        End Sub

        Private Sub CallLogAdvancedEvent_A(Param As CallLogEvent_Parameter.CallLog_Base_Parameter)
            Dim TMP As String()
            Dim e As CallLogEvent_Parameter.CallLog_A_Parameter = New CallLogEvent_Parameter.CallLog_A_Parameter
            Dim oLogInfo As CallLogEvent_Type.CallLogEvent_A = New CallLogEvent_Type.CallLogEvent_A
            Try


                TMP = Param.LogInfo.Split(",")

                oLogInfo.AcallId = TMP(0)
                oLogInfo.BcallId = TMP(1)
                oLogInfo.CallID = CInt((TMP(2)))

                e.IdPbx = Param.IdPbx
                e.LogInfo = oLogInfo

                RaiseEvent CallLog_Event_A(Me, e)
            Catch ex As Exception
                'DebugMode()
                'Throw
            Finally
                oLogInfo = Nothing
            End Try





        End Sub

        Private Sub CallLogAdvancedEvent_S(Param As CallLogEvent_Parameter.CallLog_Base_Parameter)
            Dim TMP As String()
            Dim oLogInfo As CallLogEvent_Type.CallLogEvent_S = New CallLogEvent_Type.CallLogEvent_S
            Dim e As CallLogEvent_Parameter.CallLog_S_Parameter = New CallLogEvent_Parameter.CallLog_S_Parameter
            Try
                TMP = Param.LogInfo.Split(",")



                With oLogInfo
                    .AcallId = If(IsNothing(TMP(0)), "", TMP(0))
                    .BcallId = If(IsNothing(TMP(1)), "", TMP(1))
                    .Astate = If(IsNothing(TMP(2)) Or TMP(2).Trim.Length = 0, 0, Val(TMP(2)))
                    .Bstate = If(IsNothing(TMP(3)) Or TMP(3).Trim.Length = 0, 0, Val(TMP(3)))
                    .Aconnected = If(IsNothing(TMP(4)) Or TMP(2).Trim.Length = 0, 0, Val(TMP(4)))
                    .AisMusic = If(IsNothing(TMP(5)) Or TMP(5).Trim.Length = 0, 0, Val(TMP(5)))
                    .Bconnected = If(IsNothing(TMP(6)) Or TMP(6).Trim.Length = 0, 0, Val(TMP(6)))
                    .BisMusic = If(IsNothing(TMP(7)) Or TMP(7).Trim.Length = 0, 0, Val(TMP(7)))
                    .Aname = If(IsNothing(TMP(8)), "", TMP(8))
                    .Bname = If(IsNothing(TMP(9)), "", TMP(9))
                    .Blist = If(IsNothing(TMP(10)), "", TMP(10))
                    .AslotAchannel = If(IsNothing(TMP(11)), "", TMP(11))
                    .BslotBchannel = If(IsNothing(TMP(12)), "", TMP(12))
                    .CalledPartyPresentation = If(IsNothing(TMP(13)), "", TMP(13))
                    .CalledPartyNumber = If(IsNothing(TMP(14)), "", TMP(14))
                    .CallingPartyPresentationType = If(IsNothing(TMP(15)), "", TMP(15))
                    .CallingPartyNumber = If(IsNothing(TMP(16)), "", TMP(16))
                    .CalledSubAddress = If(IsNothing(TMP(17)), "", TMP(17))
                    .CallingSubAddress = If(IsNothing(TMP(18)), "", TMP(18))
                    .DialledPartyType = If(IsNothing(TMP(19)) Or TMP(19).Trim.Length = 0, 0, Val(TMP(19)))
                    .DialledPartyNumber = If(IsNothing(TMP(20)), "", TMP(20))
                    .KeypadType = If(IsNothing(TMP(21)) Or TMP(21).Trim.Length = 0, 0, Val(TMP(21)))
                    .KeypadNumber = If(IsNothing(TMP(22)), "", TMP(22))
                    .RingAttemptCount = If(IsNothing(TMP(23)) Or TMP(23).Trim.Length = 0, 0, Val(TMP(23)))
                    .Cause = If(IsNothing(TMP(24)) Or TMP(24).Trim.Length = 0, 0, Val(TMP(24)))
                    .VoicemailDisallow = If(IsNothing(TMP(25)) Or TMP(25).Trim.Length = 0, 0, Val(TMP(25)))
                    .SendingComplete = If(IsNothing(TMP(26)) Or TMP(26).Trim.Length = 0, 0, Val(TMP(26)))
                    .CallTypeTransportType = If(IsNothing(TMP(27)), "", TMP(27))
                    .OwnerHuntGroupName = If(IsNothing(TMP(28)), "", TMP(28))
                    .OriginalHuntGroupName = If(IsNothing(TMP(29)), "", TMP(29))
                    .OriginalUserName = If(IsNothing(TMP(30)), "", TMP(30))
                    .TargetHuntGroupName = If(IsNothing(TMP(31)), "", TMP(31))
                    .TargetUserName = If(IsNothing(TMP(32)), "", TMP(32))
                    .TargetRASName = If(IsNothing(TMP(33)), "", TMP(33))
                    .IsInternalCall = If(IsNothing(TMP(34)) Or TMP(34).Trim.Length = 0, 0, Val(TMP(34)))
                    .TimeStamp = If(IsNothing(TMP(35)), "", TMP(35))
                    .ConnectedTime = If(IsNothing(TMP(36)) Or TMP(36).Trim.Length = 0, 0, Val(TMP(36)))
                    .RingTime = If(IsNothing(TMP(37)) Or TMP(37).Trim.Length = 0, 0, Val(TMP(37)))
                    .ConnectedDuration = If(IsNothing(TMP(38)) Or TMP(38).Trim.Length = 0, 0, Val(TMP(38)))
                    .RingDuration = If(IsNothing(TMP(39)) Or TMP(39).Trim.Length = 0, 0, Val(TMP(39)))
                    .Locale = If(IsNothing(TMP(40)), "", TMP(40))
                    .ParkSlotNumber = If(IsNothing(TMP(41)), "", TMP(41))
                    .CallWaiting = If(IsNothing(TMP(42)), "", TMP(42))
                    .Tag = If(IsNothing(TMP(43)), "", TMP(43))
                    .Transferring = If(IsNothing(TMP(44)), "0", TMP(44))
                    .ServiceActive = If(IsNothing(TMP(45)) Or TMP(45).Trim.Length = 0, 0, Val(TMP(45)))
                    .ServiceQuotaUsed = If(IsNothing(TMP(46)) Or TMP(46).Trim.Length = 0, 0, Val(TMP(46)))
                    .ServiceQuotaTime = If(IsNothing(TMP(47)) Or TMP(47).Trim.Length = 0, 0, Val(TMP(47)))
                    .AccountCode = If(IsNothing(TMP(48)), "", TMP(48))
                    .CallID = If(IsNothing(TMP(49)) Or TMP(49).Trim.Length = 0, 0, Val(TMP(49)))


                End With


                e.IdPbx = Param.IdPbx
                e.LogInfo = oLogInfo

                RaiseEvent CallLog_Event_S(Me, e)
            Catch ex As Exception
                'DebugMode()
                'Throw
            Finally
                oLogInfo = Nothing
            End Try

        End Sub

        Private Sub CallLogAdvancedEvent_D(Param As CallLogEvent_Parameter.CallLog_Base_Parameter)
            Dim TMP As String()
            Dim e As CallLogEvent_Parameter.CallLog_D_Parameter = New CallLogEvent_Parameter.CallLog_D_Parameter
            Dim oLogInfo As CallLogEvent_Type.CallLogEvent_D = New CallLogEvent_Type.CallLogEvent_D
            Try
                TMP = Param.LogInfo.Split(",")

                oLogInfo.AcallId = TMP(0)
                oLogInfo.BcallId = TMP(1)
                oLogInfo.CallID = CInt((TMP(2)))

                e.IdPbx = Param.IdPbx
                e.LogInfo = oLogInfo

                RaiseEvent CallLog_Event_D(Me, e)
            Catch ex As Exception
                'DebugMode()
                'Throw
            Finally
                oLogInfo = Nothing
            End Try



        End Sub

       

    End Class




End Namespace