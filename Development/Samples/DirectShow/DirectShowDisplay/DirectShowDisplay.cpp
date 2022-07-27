
#include <afxwin.h>
#include <dshow.h>
#include <initguid.h>
#include <conio.h>
#include "MvDSSource.h"

#define SAFE_RELEASE( a ) \
    if ( a != NULL ) \
    { a->Release(); a = NULL; }

// Forward declarations
HRESULT GetFirstPin( IBaseFilter *aFilter, IPin **aPin );
BOOL CreateDisplayWindow();
void UpdateDisplayPosition();
LRESULT CALLBACK WndProc( HWND aHwnd, UINT aMsg, WPARAM wParam, LPARAM lParam );
void WaitForKeyPress();

// Globals
HWND gWindowHandle = NULL;
IVideoWindow *gVideoWindow = NULL;

// Main function
int main()
{
    CWinApp lApp; 
    AfxWinInit( ::GetModuleHandle( NULL ), NULL, ::GetCommandLine(), 0 );

    // Initialize the COM library
    ::CoInitialize( NULL );

    // Create the filter graph manager
    printf("Creating filter graph manager\r\n");
    IGraphBuilder *lGraph = NULL;
    HRESULT lResult = ::CoCreateInstance( CLSID_FilterGraph, NULL, CLSCTX_INPROC_SERVER, IID_IGraphBuilder, (void **)&lGraph );
    if ( FAILED( lResult ) )
    {
        // ...
    }

    // Create SDK DSSource filter
    printf("Creating SDK DSSource filter\r\n");
    IBaseFilter *lSourceFilter = NULL;
    lResult = ::CoCreateInstance( CLSID_MvDSSource, NULL, CLSCTX_INPROC_SERVER, IID_IBaseFilter, (void **)&lSourceFilter);
    if ( FAILED( lResult ) )
    {
        // ...
    }

    // Get interface used to control the SDK DSSource filter from source filter
    MvDSSource *lDSSource = NULL;
    lResult = lSourceFilter->QueryInterface( IID_IMvDSSource, (void **)&lDSSource );
    if ( FAILED( lResult ) )
    {
        // ...
    }

    //Set device info
    MV_CC_DEVICE_INFO_LIST stDeviceList ={0};
    lResult = lDSSource->GetDeviceList(&stDeviceList);
    if ( FAILED( lResult ) )
    {
        // ...
    }
    for (unsigned int i=0;i < stDeviceList.nDeviceNum;i++)
    {
        printf("i = %d\r\n",i);
        if(stDeviceList.pDeviceInfo[i]->nTLayerType == MV_USB_DEVICE)
        {
            printf("chUserDefinedName = %s\r\n",stDeviceList.pDeviceInfo[i]->SpecialInfo.stUsb3VInfo.chUserDefinedName);
            printf("chManufacturerName = %s\r\n",stDeviceList.pDeviceInfo[i]->SpecialInfo.stUsb3VInfo.chManufacturerName);
            printf("chModelName = %s\r\n",stDeviceList.pDeviceInfo[i]->SpecialInfo.stUsb3VInfo.chModelName);
        }
        else if(stDeviceList.pDeviceInfo[i]->nTLayerType == MV_GIGE_DEVICE)
        {
            printf("chUserDefinedName = %s\r\n",stDeviceList.pDeviceInfo[i]->SpecialInfo.stGigEInfo.chUserDefinedName);
            printf("chManufacturerName = %s\r\n",stDeviceList.pDeviceInfo[i]->SpecialInfo.stGigEInfo.chManufacturerName);
            printf("chModelName = %s\r\n",stDeviceList.pDeviceInfo[i]->SpecialInfo.stGigEInfo.chModelName);
        }
        printf("************************************\r\n");
    }

    int nCurIndex = -1;
    lResult = lDSSource->GetCurIndex(&nCurIndex);
    if ( FAILED( lResult ) )
    {
        // ...
    }
    printf("nCurIndex = %d\r\n",nCurIndex);
    if (nCurIndex == -1)
    {
        printf("Find no devices!\n");
        printf("Press a key to stop grabbing.\n");
        WaitForKeyPress();
        return -1;
    }

    printf("Input index:");
    scanf("%d",&nCurIndex);
    lResult = lDSSource->SetIndex(nCurIndex);
    if ( FAILED( lResult ) )
    {
        // ...
    }

    /*lResult = lDSSource->SetOutputFormat(PixelType_RGB24);
    if ( FAILED( lResult ) )
    {
        // ...
    }
    printf("SetOutputFormat\r\n");*/

    // Create display window
    printf("Creating display window\r\n");
    BOOL lSuccess = CreateDisplayWindow();
    if ( !lSuccess )
    {
        // ...
    }

    // Create video renderer
    printf("Creating video renderer filter\r\n");
    IBaseFilter *lVideoFilter = NULL;
    lResult = ::CoCreateInstance( CLSID_VideoRenderer,NULL, CLSCTX_INPROC_SERVER, IID_IBaseFilter, (void **)&lVideoFilter );

    // Add source and display filters to the graph
    printf("Adding filters to graph\r\n");
    lResult = lGraph->AddFilter( lSourceFilter, L"Source" );
    if ( FAILED( lResult ) )
    {
        // ...
    }
    lResult = lGraph->AddFilter( lVideoFilter, L"Display" );
    if ( FAILED( lResult ) )
    {
        // ...
    }

    // Get pins of out filters: easy, we want the 1st pin of both filters.
    printf("Retrieving filter pins\r\n");
    IPin *lSourcePin = NULL;
    lResult = GetFirstPin( lSourceFilter, &lSourcePin );
    if ( FAILED( lResult ) )
    {
        // ...
    }
    IPin *lDisplayPin = NULL;
    lResult = GetFirstPin( lVideoFilter, &lDisplayPin );
    if ( FAILED( lResult ) )
    {
        // ...
    }

    // Connect the pins. The graph will instantiate required intermediate filters, if any.
    printf("Connecting filter pins\r\n");
    lResult = lGraph->Connect( lSourcePin, lDisplayPin );
    if ( FAILED( lResult ) )
    {
        // ...
    }

    // Get the video window interface from the graph
    gVideoWindow = NULL;
    lResult = lGraph->QueryInterface( IID_IVideoWindow, (void **)&gVideoWindow );
    if ( FAILED( lResult ) )
    {
        // ...
    }

    // Attach display to our window
    gVideoWindow->put_Owner( (OAHWND)gWindowHandle );
    gVideoWindow->put_MessageDrain( (OAHWND)gWindowHandle );
    gVideoWindow->put_WindowStyle( WS_CHILD | WS_CLIPSIBLINGS );
    UpdateDisplayPosition();

    // Get the media control interface from the graph
    printf("Retrieving media control interface from graph\r\n");
    IMediaControl *lMediaControl = NULL;
    lResult = lGraph->QueryInterface( IID_IMediaControl, (void **)&lMediaControl );
    if ( FAILED( lResult ) )
    {
        // ...
    }

    // Start graph.
    printf("Starting the graph\r\n");
    lResult = lMediaControl->Run();
    if ( FAILED( lResult ) )
    {
        // ...
    }

    // Process display window messages, until it is closed.
    printf("Processing window messages until window terminates\r\n");
    MSG lMsg;
    while ( ::GetMessage( &lMsg, NULL, 0, 0 ) > 0 )
    {
        TranslateMessage( &lMsg );
        DispatchMessage( &lMsg );
    }

    // Stop graph.
    printf("Stopping the graph\r\n");
    lResult = lMediaControl->Stop();
    if ( FAILED( lResult ) )
    {
        // ...
    }

    // Release acquired interfaces
    SAFE_RELEASE( lMediaControl );
    SAFE_RELEASE( gVideoWindow );
    SAFE_RELEASE( lDisplayPin );
    SAFE_RELEASE( lSourcePin );
    SAFE_RELEASE( lVideoFilter );
    SAFE_RELEASE( lDSSource );
    SAFE_RELEASE( lSourceFilter );
    SAFE_RELEASE( lGraph );

    // Release COM library
    ::CoUninitialize();

    AfxWinTerm();

	return 0;
}

// Returns the first pin of a filter
HRESULT GetFirstPin( IBaseFilter *aFilter, IPin **aPin )
{
    // Get pin enumeration interface from the filter
    IEnumPins *lEP = NULL;
    HRESULT lResult = aFilter->EnumPins( &lEP );
    if ( FAILED( lResult ) )
    {
        return lResult;
    }

    // Retrieve the first pin of the filter
    ULONG lFetched = 0;
    lResult = lEP->Next( 1, aPin, &lFetched );
    if ( FAILED( lResult ) )
    {
        return lResult;
    }

    // Release enumerator. Caller is responsible of releasing the pin.
    lEP->Release();

    return NOERROR;
}

// Creates a window where the display will draw
BOOL CreateDisplayWindow()
{
    const char ClassName[] = "DisplayWindowClass";

    WNDCLASSEX lWindowClass;
    lWindowClass.cbSize = sizeof(WNDCLASSEX);
    lWindowClass.style = 0;
    lWindowClass.lpfnWndProc = WndProc;
    lWindowClass.cbClsExtra = 0;
    lWindowClass.cbWndExtra = 0;
    lWindowClass.hInstance = ::AfxGetInstanceHandle();
    lWindowClass.hIcon = ::LoadIcon( NULL, IDI_APPLICATION );
    lWindowClass.hCursor = ::LoadCursor( NULL, IDC_ARROW );
    lWindowClass.hbrBackground = (HBRUSH)( COLOR_WINDOW + 1 );
    lWindowClass.lpszMenuName = NULL;
    lWindowClass.lpszClassName = ClassName;
    lWindowClass.hIconSm = ::LoadIcon( NULL, IDI_APPLICATION );

    if( !::RegisterClassEx( &lWindowClass ) )
    {
        return FALSE;
    }

    gWindowHandle = ::CreateWindowEx(WS_EX_CLIENTEDGE,ClassName,"DirectShowDisplay",WS_OVERLAPPEDWINDOW,
        CW_USEDEFAULT, CW_USEDEFAULT, 640, 480,NULL, NULL, ::AfxGetInstanceHandle(), NULL );
    if ( gWindowHandle == NULL )
    {
        return FALSE;
    }

    ::ShowWindow( gWindowHandle, SW_SHOW );
    ::UpdateWindow( gWindowHandle );

    return TRUE;
}

// Updates the display position
void UpdateDisplayPosition()
{
    RECT lRect;
    ::GetClientRect( gWindowHandle, &lRect );

    if ( gVideoWindow != NULL )
    {
        gVideoWindow->SetWindowPosition( 0, 0, lRect.right, lRect.bottom );
    }
}

// Display window message handler
LRESULT CALLBACK WndProc( HWND aHwnd, UINT aMsg, WPARAM wParam, LPARAM lParam )
{
    switch( aMsg )
    {
    case WM_CLOSE:
        DestroyWindow( aHwnd );
        break;

    case WM_DESTROY:
        PostQuitMessage(0);
        break;

    case WM_LBUTTONDBLCLK :
        if ( gVideoWindow != NULL )
        {
            LONG lCurrent = 0;
            gVideoWindow->get_FullScreenMode( &lCurrent );
            gVideoWindow->put_FullScreenMode( ( lCurrent == OATRUE ) ? OAFALSE : OATRUE );
        }
        break;

    case WM_SIZE:
        UpdateDisplayPosition();
        break;

    default:
        return DefWindowProc( aHwnd , aMsg, wParam, lParam );
    }

    return 0;
}

void WaitForKeyPress()
{
    while(!_kbhit())
    {
        Sleep(10);
    }
    _getch();
}

