Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Linq
Imports System.Runtime.InteropServices
Imports System.Net
Imports System.Diagnostics
Imports System.Drawing
Imports System.IO
Imports System.Text
Imports Whois.NET

Namespace SocketConnection
    Public Class SocketConnectionForm
        ' The version of IP used by the TCP/UDP endpoint. AF_INET is used for IPv4.
        Private Const AF_INET As Integer = 2
        ' List of Active TCP Connections.
        Private Shared TcpActiveConnections As List(Of TcpProcessRecord) = Nothing
        ' List of Active UDP Connections.
        Private Shared UdpActiveConnections As List(Of UdpProcessRecord) = Nothing
        Private Shared LookupCount As Integer = 0
        Private Shared LastLookupTime As DateTime
        ' The GetExtendedTcpTable function retrieves a table that contains a list of
        ' TCP endpoints available to the application. Decorating the function with
        ' DllImport attribute indicates that the attributed method is exposed by an
        ' unmanaged dynamic-link library 'iphlpapi.dll' as a static entry point.
        <DllImport("iphlpapi.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
        Private Shared Function GetExtendedTcpTable(pTcpTable As IntPtr, ByRef pdwSize As Integer, bOrder As Boolean, ulAf As Integer, tableClass As TcpTableClass, Optional reserved As UInteger = 0) As UInteger
        End Function

        ' The GetExtendedUdpTable function retrieves a table that contains a list of
        ' UDP endpoints available to the application. Decorating the function with
        ' DllImport attribute indicates that the attributed method is exposed by an
        ' unmanaged dynamic-link library 'iphlpapi.dll' as a static entry point.
        <DllImport("iphlpapi.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
        Private Shared Function GetExtendedUdpTable(pUdpTable As IntPtr, ByRef pdwSize As Integer, bOrder As Boolean, ulAf As Integer, tableClass As UdpTableClass, Optional reserved As UInteger = 0) As UInteger
        End Function

        Public Sub New()
            ' Intialize the components on the Windows Form.
        End Sub

        ''' <summary>
        ''' This function reads and parses the active TCP socket connections available
        ''' and stores them in a list.
        ''' </summary>
        ''' <returns>
        ''' It returns the current set of TCP socket connections which are active.
        ''' </returns>
        ''' <exception cref="OutOfMemoryException">
        ''' This exception may be thrown by the function Marshal.AllocHGlobal when there
        ''' is insufficient memory to satisfy the request.
        ''' </exception>
        Public Shared Function GetAllTcpConnections() As List(Of TcpProcessRecord)
            Dim bufferSize As Integer = 0
            Dim tcpTableRecords As New List(Of TcpProcessRecord)()

            ' Getting the size of TCP table, that is returned in 'bufferSize' variable.
            Dim result As UInteger = GetExtendedTcpTable(IntPtr.Zero, bufferSize, True, AF_INET, TcpTableClass.TCP_TABLE_OWNER_PID_ALL)

            ' Allocating memory from the unmanaged memory of the process by using the
            ' specified number of bytes in 'bufferSize' variable.
            Dim tcpTableRecordsPtr As IntPtr = Marshal.AllocHGlobal(bufferSize)

            Try
                ' The size of the table returned in 'bufferSize' variable in previous
                ' call must be used in this subsequent call to 'GetExtendedTcpTable'
                ' function in order to successfully retrieve the table.
                result = GetExtendedTcpTable(tcpTableRecordsPtr, bufferSize, True, AF_INET, TcpTableClass.TCP_TABLE_OWNER_PID_ALL)

                ' Non-zero value represent the function 'GetExtendedTcpTable' failed,
                ' hence empty list is returned to the caller function.
                If result <> 0 Then
                    Return New List(Of TcpProcessRecord)()
                End If

                ' Marshals data from an unmanaged block of memory to a newly allocated
                ' managed object 'tcpRecordsTable' of type 'MIB_TCPTABLE_OWNER_PID'
                ' to get number of entries of the specified TCP table structure.
                Dim tcpRecordsTable As MIB_TCPTABLE_OWNER_PID = CType(Marshal.PtrToStructure(tcpTableRecordsPtr, GetType(MIB_TCPTABLE_OWNER_PID)), MIB_TCPTABLE_OWNER_PID)
                'Dim tableRowPtr As IntPtr = DirectCast(CLng(tcpTableRecordsPtr) + Marshal.SizeOf(tcpRecordsTable.dwNumEntries), IntPtr)
                Dim tableRowPtr As IntPtr = New IntPtr(CLng(tcpTableRecordsPtr) + Marshal.SizeOf(tcpRecordsTable.dwNumEntries))

                ' Reading and parsing the TCP records one by one from the table and
                ' storing them in a list of 'TcpProcessRecord' structure type objects.
                For row As Integer = 0 To tcpRecordsTable.dwNumEntries - 1
                    Dim tcpRow As MIB_TCPROW_OWNER_PID = CType(Marshal.PtrToStructure(tableRowPtr, GetType(MIB_TCPROW_OWNER_PID)), MIB_TCPROW_OWNER_PID)
                    Dim localip = New IPAddress(tcpRow.localAddr)
                    Dim remoteip = New IPAddress(tcpRow.remoteAddr)
                    Dim localport As UShort = BitConverter.ToUInt16(New Byte(1) {tcpRow.localPort(1), tcpRow.localPort(0)}, 0)
                    Dim remoteport As UShort = BitConverter.ToUInt16(New Byte(1) {tcpRow.remotePort(1), tcpRow.remotePort(0)}, 0)
                    Dim tmpRecord As New TcpProcessRecord(localip, remoteip, localport, remoteport, tcpRow.owningPid, tcpRow.state)
                    If tmpRecord.RemoteAddress.ToString <> "0.0.0.0" AndAlso tmpRecord.RemoteAddress.ToString <> "127.0.0.1" Then LookupLocation(tmpRecord)
                    tcpTableRecords.Add(tmpRecord)
                    'tcpTableRecords.Add(New TcpProcessRecord(New IPAddress(tcpRow.localAddr), New IPAddress(tcpRow.remoteAddr), BitConverter.ToUInt16(New Byte(1) {tcpRow.localPort(1), tcpRow.localPort(0)}, 0), BitConverter.ToUInt16(New Byte(1) {tcpRow.remotePort(1), tcpRow.remotePort(0)}, 0), tcpRow.owningPid, tcpRow.state))
                    'tableRowPtr = DirectCast(CLng(tableRowPtr) + Marshal.SizeOf(tcpRow), IntPtr)
                    tableRowPtr = New IntPtr(CLng(tableRowPtr) + Marshal.SizeOf(tcpRow))
                Next
            Catch outOfMemoryException As OutOfMemoryException
                MessageBox.Show(outOfMemoryException.Message, "Out Of Memory", MessageBoxButton.OK, MessageBoxImage.Stop)
            Catch exception As Exception
                MessageBox.Show(exception.Message, "Exception", MessageBoxButton.OK, MessageBoxImage.Stop)
            Finally
                Marshal.FreeHGlobal(tcpTableRecordsPtr)
            End Try
            Return If(tcpTableRecords IsNot Nothing, tcpTableRecords.Distinct().ToList(), New List(Of TcpProcessRecord)())
        End Function
        Private Shared Sub LookupLocationx(ByRef tpr As TcpProcessRecord)
            If LookupCount = 0 Then LastLookupTime = Now()
            If LookupCount > 150 AndAlso Now.Subtract(LastLookupTime) < New TimeSpan(0, 1, 0) Then
                Do Until Now.Subtract(LastLookupTime) > New TimeSpan(0, 1, 0)
                    Debug.WriteLine("Waiting as to not get my ip banned.")
                Loop
                LookupCount = 0
                LastLookupTime = Now()
            End If

            Dim strResponse As String
            Dim strRequest As String = $"http://ip-api.com/line/{tpr.RemoteAddress.ToString}"
            Dim request As WebRequest = WebRequest.Create(strRequest)

            ' Set credentials to use for this request.
            request.Credentials = CredentialCache.DefaultCredentials

            Dim response As HttpWebResponse = CType(request.GetResponse(), HttpWebResponse)

            Console.WriteLine("Content length is {0}", response.ContentLength)
            Console.WriteLine("Content type is {0}", response.ContentType)

            ' Get the stream associated with the response.
            Dim receiveStream As Stream = response.GetResponseStream()

            ' Pipes the stream to a higher level stream reader with the required encoding format. 
            Dim readStream As New StreamReader(receiveStream, Encoding.UTF8)

            'Console.WriteLine("Response stream received.")
            'Console.WriteLine(readStream.ReadToEnd())
            strResponse = Trim(readStream.ReadToEnd)
            response.Close()
            readStream.Close()

            If strResponse <> String.Empty Then
                Dim responsedata() As String
                responsedata = strResponse.Split(vbLf)
                If responsedata.Count > 0 AndAlso responsedata(0) = "success" Then
                    With tpr
                        .RemoteCountry = responsedata(1)
                        .RemoteRegion = responsedata(4)
                        .RemoteCity = responsedata(5)
                        '_latitude = responsedata(7)
                        '_longitude = responsedata(8)
                    End With
                End If
            End If
            LookupCount += 1
        End Sub
        Private Shared Sub LookupLocation(ByRef tpr As TcpProcessRecord)
            Dim itm = WhoisClient.Query(tpr.RemoteAddress.ToString)
            Dim rawdata() As String = itm.Raw.Split(New Char() {vbCrLf, vbLf})
            Try
                tpr.RemoteCountry = ParseRawDataLineForValue(rawdata, "Country:")
                tpr.RemoteCity = ParseRawDataLineForValue(rawdata, "City:")
                tpr.RemoteRegion = ParseRawDataLineForValue(rawdata, "StateProv:")
            Catch ex As Exception
                Debugger.Break()
            End Try
        End Sub
        Private Shared Function ParseRawDataLineForValue(ByVal rawdata() As String, ByVal strSearchString As String) As String
            Dim retval As String = rawdata.ToList.Where(Function(x) x.StartsWith(strSearchString)).FirstOrDefault
            If retval IsNot Nothing Then
                retval = retval.Replace(strSearchString, "").Trim
            Else
                Return "Not Found."
            End If
            Return retval
        End Function
    End Class

    ' Enum for protocol types.
    Public Enum Protocol
        TCP
        UDP
    End Enum

    ' Enum to define the set of values used to indicate the type of table returned by 
    ' calls made to the function 'GetExtendedTcpTable'.
    Public Enum TcpTableClass
        TCP_TABLE_BASIC_LISTENER
        TCP_TABLE_BASIC_CONNECTIONS
        TCP_TABLE_BASIC_ALL
        TCP_TABLE_OWNER_PID_LISTENER
        TCP_TABLE_OWNER_PID_CONNECTIONS
        TCP_TABLE_OWNER_PID_ALL
        TCP_TABLE_OWNER_MODULE_LISTENER
        TCP_TABLE_OWNER_MODULE_CONNECTIONS
        TCP_TABLE_OWNER_MODULE_ALL
    End Enum

    ' Enum to define the set of values used to indicate the type of table returned by calls
    ' made to the function GetExtendedUdpTable.
    Public Enum UdpTableClass
        UDP_TABLE_BASIC
        UDP_TABLE_OWNER_PID
        UDP_TABLE_OWNER_MODULE
    End Enum

    ' Enum for different possible states of TCP connection
    Public Enum MibTcpState
        CLOSED = 1
        LISTENING = 2
        SYN_SENT = 3
        SYN_RCVD = 4
        ESTABLISHED = 5
        FIN_WAIT1 = 6
        FIN_WAIT2 = 7
        CLOSE_WAIT = 8
        CLOSING = 9
        LAST_ACK = 10
        TIME_WAIT = 11
        DELETE_TCB = 12
        NONE = 0
    End Enum

    ''' <summary>
    ''' The structure contains information that describes an IPv4 TCP connection with 
    ''' IPv4 addresses, ports used by the TCP connection, and the specific process ID
    ''' (PID) associated with connection.
    ''' </summary>
    <StructLayout(LayoutKind.Sequential)>
    Public Structure MIB_TCPROW_OWNER_PID
        Public state As MibTcpState
        Public localAddr As UInteger
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=4)>
        Public localPort As Byte()
        Public remoteAddr As UInteger
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=4)>
        Public remotePort As Byte()
        Public owningPid As Integer
    End Structure

    ''' <summary>
    ''' The structure contains a table of process IDs (PIDs) and the IPv4 TCP links that 
    ''' are context bound to these PIDs.
    ''' </summary>
    <StructLayout(LayoutKind.Sequential)>
    Public Structure MIB_TCPTABLE_OWNER_PID
        Public dwNumEntries As UInteger
        <MarshalAs(UnmanagedType.ByValArray, ArraySubType:=UnmanagedType.Struct, SizeConst:=1)>
        Public table As MIB_TCPROW_OWNER_PID()
    End Structure

    ''' <summary>
    ''' This class provides access an IPv4 TCP connection addresses and ports and its
    ''' associated Process IDs and names.
    ''' </summary>
    <StructLayout(LayoutKind.Sequential)>
    Public Class TcpProcessRecord
        Private _LocalAddress As IPAddress
        <DisplayName("Local Address")>
        Public Property LocalAddress() As IPAddress
            Get
                Return _LocalAddress
            End Get
            Set
                _LocalAddress = Value
            End Set
        End Property
        Private _LocalPort As UShort
        <DisplayName("Local Port")>
        Public Property LocalPort() As UShort
            Get
                Return _LocalPort
            End Get
            Set
                _LocalPort = Value
            End Set
        End Property
        Private _RemoteAddress As IPAddress
        <DisplayName("Remote Address")>
        Public Property RemoteAddress() As IPAddress
            Get
                Return _RemoteAddress
            End Get
            Set
                _RemoteAddress = Value
            End Set
        End Property
        Private _RemotePort As UShort
        <DisplayName("Remote Port")>
        Public Property RemotePort() As UShort
            Get
                Return _RemotePort
            End Get
            Set
                _RemotePort = Value
            End Set
        End Property
        Private _State As MibTcpState
        <DisplayName("State")>
        Public Property State() As MibTcpState
            Get
                Return _State
            End Get
            Set
                _State = Value
            End Set
        End Property
        Private _ProcessId As Integer
        <DisplayName("Process ID")>
        Public Property ProcessId() As Integer
            Get
                Return _ProcessId
            End Get
            Set
                _ProcessId = Value
            End Set
        End Property
        Private _ProcessName As String
        <DisplayName("Process Name")>
        Public Property ProcessName() As String
            Get
                Return _ProcessName
            End Get
            Set
                _ProcessName = Value
            End Set
        End Property
        Private _remoteCountry As String = "local"
        <DisplayName("Country")>
        Public Property RemoteCountry() As String
            Get
                Return _remoteCountry
            End Get
            Set(ByVal value As String)
                _remoteCountry = value
            End Set
        End Property
        Private _remoteCity As String = "local"
        <DisplayName("City")>
        Public Property RemoteCity() As String
            Get
                Return _remoteCity
            End Get
            Set(ByVal value As String)
                _remoteCity = value
            End Set
        End Property

        Private _remoteRegion As String = "local"
        <DisplayName("State")>
        Public Property RemoteRegion() As String
            Get
                Return _remoteRegion
            End Get
            Set(ByVal value As String)
                _remoteRegion = value
            End Set
        End Property
        Private _latitude As String
        Private _longitude As String

        Public Sub New(localIp As IPAddress, remoteIp As IPAddress, localPort__1 As UShort, remotePort__2 As UShort, pId As Integer, state__3 As MibTcpState)
            LocalAddress = localIp
            RemoteAddress = remoteIp
            LocalPort = localPort__1
            RemotePort = remotePort__2
            State = state__3
            ProcessId = pId
            ' Getting the process name associated with a process id.
            If Process.GetProcesses().Any(Function(process__4) process__4.Id = pId) Then
                ProcessName = Process.GetProcessById(ProcessId).ProcessName
            End If
        End Sub

    End Class

    ''' <summary>
    ''' The structure contains an entry from the User Datagram Protocol (UDP) listener
    ''' table for IPv4 on the local computer. The entry also includes the process ID
    ''' (PID) that issued the call to the bind function for the UDP endpoint.
    ''' </summary>
    <StructLayout(LayoutKind.Sequential)>
    Public Structure MIB_UDPROW_OWNER_PID
        Public localAddr As UInteger
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=4)>
        Public localPort As Byte()
        Public owningPid As Integer
    End Structure

    ''' <summary>
    ''' The structure contains the User Datagram Protocol (UDP) listener table for IPv4
    ''' on the local computer. The table also includes the process ID (PID) that issued
    ''' the call to the bind function for each UDP endpoint.
    ''' </summary>
    <StructLayout(LayoutKind.Sequential)>
    Public Structure MIB_UDPTABLE_OWNER_PID
        Public dwNumEntries As UInteger
        <MarshalAs(UnmanagedType.ByValArray, ArraySubType:=UnmanagedType.Struct, SizeConst:=1)>
        Public table As UdpProcessRecord()
    End Structure

    ''' <summary>
    ''' This class provides access an IPv4 UDP connection addresses and ports and its
    ''' associated Process IDs and names.
    ''' </summary>
    <StructLayout(LayoutKind.Sequential)>
    Public Class UdpProcessRecord

        Private _LocalAddress As IPAddress
        <DisplayName("Local Address")>
        Public Property LocalAddress() As IPAddress
            Get
                Return _LocalAddress
            End Get
            Set
                _LocalAddress = Value
            End Set
        End Property

        Private _LocalPort As UInteger
        <DisplayName("Local Port")>
        Public Property LocalPort() As UInteger
            Get
                Return _LocalPort
            End Get
            Set
                _LocalPort = Value
            End Set
        End Property

        Private _ProcessId As Integer
        <DisplayName("Process ID")>
        Public Property ProcessId() As Integer
            Get
                Return _ProcessId
            End Get
            Set
                _ProcessId = Value
            End Set
        End Property

        Private _ProcessName As String
        <DisplayName("Process Name")>
        Public Property ProcessName() As String
            Get
                Return _ProcessName
            End Get
            Set
                _ProcessName = Value
            End Set
        End Property
        Public Sub New(localAddress__1 As IPAddress, localPort__2 As UInteger, pId As Integer)
            LocalAddress = localAddress__1
            LocalPort = localPort__2
            ProcessId = pId
            If Process.GetProcesses().Any(Function(process__3) process__3.Id = pId) Then
                ProcessName = Process.GetProcessById(ProcessId).ProcessName
            End If
        End Sub

    End Class
End Namespace
