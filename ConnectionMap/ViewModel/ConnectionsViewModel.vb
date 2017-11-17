Imports System.Collections.ObjectModel
Imports System.Net
Imports GalaSoft.MvvmLight

Public Class ConnectionsViewModel
    Inherits ViewModelBase
#Region "   Events   "

#End Region
#Region "   Internal Properties   "
    Private _conFrom As New SocketConnection.SocketConnectionForm()
    Private _localIPAddresses As New List(Of IPAddress)
#End Region
#Region "   Exposed Properties   "
    Private _tcpConnections As List(Of SocketConnection.TcpProcessRecord)
    Public ReadOnly Property TCPConnections() As List(Of SocketConnection.TcpProcessRecord)
        Get
            Dim retval As New List(Of SocketConnection.TcpProcessRecord)
            Select Case IgnoreLocalConnections
                Case True
                    retval = _tcpConnections.Where(Function(x) _localIPAddresses.Contains(x.RemoteAddress) = False).ToList
                Case False
                    retval = _tcpConnections
            End Select
            Return retval
        End Get
    End Property
    Private _ignoreLocalConnections As Boolean = True
    Public Property IgnoreLocalConnections() As Boolean
        Get
            Return _ignoreLocalConnections
        End Get
        Set(ByVal value As Boolean)
            _ignoreLocalConnections = value
            RaisePropertyChanged("IgnoreLocalConnections")
            RaisePropertyChanged("TCPConnections")
        End Set
    End Property
#End Region
#Region "   ICommand Properties   "

#End Region
#Region "   Constructors   "
    Sub New()
        MyBase.New
        LoadCommands()
        LoadMessaging()
        GetLocalIPs()
        GetConnections()
    End Sub
#End Region
#Region "   Command Handlers  "

#End Region
#Region "   Message Handlers   "

#End Region
#Region "   Helper Methods   "
    Private Sub LoadCommands()
        'TextBoxSubmit = New RelayCommand(Of String)(AddressOf TextBoxEntrySubmit, AddressOf TextBoxEntrySubmitCanExecute)
    End Sub
    Private Sub LoadMessaging()
        'Messaging.Messenger.Default().Register(Of KeyEntryStringMessage)(Me, AddressOf StringEntry)
    End Sub
#End Region
#Region "   Public methods   "

#End Region
#Region "   Private Methods   "
    Private Sub GetConnections()
        _tcpConnections = SocketConnection.SocketConnectionForm.GetAllTcpConnections().ToList
        RaisePropertyChanged("TCPConnections")
    End Sub
    Private Sub GetLocalIPs()
        Dim homeIP As IPAddress = Nothing
        _localIPAddresses = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList().Where(Function(x) x.AddressFamily = Net.Sockets.AddressFamily.InterNetwork).ToList
        If IPAddress.TryParse("127.0.0.1", homeIP) Then _localIPAddresses.Add(homeIP)
        If IPAddress.TryParse("0.0.0.0", homeIP) Then _localIPAddresses.Add(homeIP)
    End Sub
#End Region

End Class
