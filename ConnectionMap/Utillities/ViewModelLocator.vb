
Imports GalaSoft.MvvmLight
Imports GalaSoft.MvvmLight.Ioc
Imports Microsoft.Practices.ServiceLocation

Public Class ViewModelLocator
    Inherits ViewModelBase

#Region "   Events   "

#End Region

#Region "   Properties   "
    Public ReadOnly Property MainWindowVM() As MainWindowViewModel
        Get
            Return ServiceLocator.Current.GetInstance(Of MainWindowViewModel)()
        End Get
    End Property

    Public ReadOnly Property ConnectionsVM() As ConnectionsViewModel
        Get
            Return ServiceLocator.Current.GetInstance(Of ConnectionsViewModel)(New Guid().ToString)
        End Get
    End Property

    Public ReadOnly Property MapVM() As MapViewModel
        Get
            Return ServiceLocator.Current.GetInstance(Of MapViewModel)(New Guid().ToString)
        End Get
    End Property
#End Region

#Region "   Constructors   "
    <CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")>
    Sub New()
        ServiceLocator.SetLocatorProvider(Function() SimpleIoc.[Default])

        If ViewModelBase.IsInDesignModeStatic Then
            ' Create design time view services and models
            'SimpleIoc.[Default].Register(Of IDataAccess, DesignDataAccess)()
        Else
            ' Create run time view services and models
            'SimpleIoc.[Default].Register(Of IDataAccess, DataAccess)()
        End If


        If ViewModelBase.IsInDesignModeStatic Then 'DesignTime DataMock
            'Tested the logic flow here to make sure, the following will cause the all VM registration to
            'be tried in design time, yet throw exceptions back to the caller at runtime.  On Error Resume Next is generally bad
            'but in this case the only other option would be to create a list of type/functions and loop through doing the registrations
            'with a catch and a continue for or try/catch wrappers around each line to ensure that if one registration fails the rest are 
            'still tried.
            On Error Resume Next
            'SimpleIoc.Default.Register(Of CustomTenderViewModel)(Function() New CustomTenderViewModel(New DataAccessPOSMock), True)
        Else
            'SimpleIoc.Default.Register(Of CustomTenderViewModel)(Function() New CustomTenderViewModel(modStoreChekPOS.DataAccessNetworkTables))
        End If
        'For those VMs that don't need a DAL injected no need to have them in both datamock block and runtme data block.
        'SimpleIoc.Default.Register(Of MainWindowViewModel)(True)
        SimpleIoc.Default.Register(Of MainWindowViewModel)(True)
        SimpleIoc.Default.Register(Of ConnectionsViewModel)(False)
        SimpleIoc.Default.Register(Of MapViewModel)(False)
    End Sub
#End Region

#Region "   Public methods   "

#End Region

#Region "   Private Methods   "

#End Region



End Class
