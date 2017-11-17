Imports System.IO
Imports System.Net
Imports System.Text
Imports GalaSoft.MvvmLight

Public Class MainWindowViewModel
    Inherits ViewModelBase
#Region "   Events   "

#End Region
#Region "   Internal Properties   "

#End Region
#Region "   Exposed Properties   "
    Private _localIP As IPAddress
    Public ReadOnly Property LocalInterFacingIP() As IPAddress
        Get
            Return _localIP
        End Get
    End Property
#End Region
#Region "   ICommand Properties   "

#End Region
#Region "   Constructors   "
    Sub New()

        GetMyIP()
    End Sub
#End Region
#Region "   Command Handlers  "

#End Region
#Region "   Message Handlers   "

#End Region
#Region "   Helper Methods   "

#End Region
#Region "   Public methods   "

#End Region
#Region "   Private Methods   "
    Private Sub GetMyIP()
        Dim strIP As String
        Dim ip As IPAddress = Nothing
        Dim request As WebRequest = WebRequest.Create("https://api.ipify.org/")

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
        strIP = Trim(readStream.ReadToEnd)
        response.Close()
        readStream.Close()

        If IPAddress.TryParse(strIP, ip) Then
            _localIP = ip
            RaisePropertyChanged("LocalInterFacingIP")
        End If
    End Sub
#End Region




End Class
