Imports System.Runtime.InteropServices
Imports System.Threading.Thread
Imports System.Net.IPAddress
Imports MvCamCtrl.NET


Public Class ReconnectDemo
    Dim dev As MyCamera = New MyCamera

    ' ch:用于从驱动获取图像的缓存 | en:Buffer to get image from driver
    Dim m_nBufSizeForDriver As UInt32 = 3072 * 2048 * 3
    Dim m_pBufForDriver(m_nBufSizeForDriver) As Byte

    ' ch:用于保存图像的缓存 | en:Buffer to save image
    Dim m_nBufSizeForSaveImage As UInt32 = 3072 * 2048 * 3 * 3 + 2048
    Dim m_pBufForSaveImage(m_nBufSizeForSaveImage) As Byte

    Dim m_nByteImageBuffer As UInt32 = 3072 * 2048 * 3
    Dim m_byteImageBuffer(m_nByteImageBuffer) As Byte


    ' ch:成员变量，用于控制相机 | en:Member variable to control camera
    Dim m_stDeviceInfoList As MyCamera.MV_CC_DEVICE_INFO_LIST = New MyCamera.MV_CC_DEVICE_INFO_LIST
    Dim m_stDeviceInfo As MyCamera.MV_CC_DEVICE_INFO = New MyCamera.MV_CC_DEVICE_INFO
    Dim m_handle As IntPtr
    Dim m_nDeviceIndex As UInt32
    Dim m_bIsOpen As Boolean
    Dim m_bIsGrabbing As Boolean
    Dim m_stFrameOutInfo As MyCamera.MV_FRAME_OUT_INFO_EX = New MyCamera.MV_FRAME_OUT_INFO_EX()
    Dim cbCallback As MyCamera.cbExceptiondelegate = New MyCamera.cbExceptiondelegate(AddressOf cbExceptiondelegateFunc)
    Dim picHandle As IntPtr

    Private Sub cbExceptiondelegateFunc(ByVal nMsgType As Integer, ByVal pUser As IntPtr)
        If nMsgType = MyCamera.MV_EXCEPTION_DEV_DISCONNECT Then
            m_bIsOpen = False

            m_bIsGrabbing = False
            ' ch:关闭设备 | en:Close device
            Dim nRet As Int32 = MyCamera.MV_OK
            nRet = dev.MV_CC_CloseDevice_NET()
            nRet = dev.MV_CC_DestroyDevice_NET()

            If (m_stDeviceInfoList.nDeviceNum = 0 Or m_nDeviceIndex = -1) Then
                MsgBox("No device, please Select")
                Return
            End If

            ' ch:获取选择的设备信息 | en:Get Used Device Info
            Dim device As MyCamera.MV_CC_DEVICE_INFO
            device = CType(Marshal.PtrToStructure(m_stDeviceInfoList.pDeviceInfo(m_nDeviceIndex), GetType(MyCamera.MV_CC_DEVICE_INFO)), MyCamera.MV_CC_DEVICE_INFO)

            ' ch:打开设备 | en:Open Device
            Do While m_bIsOpen = False
                nRet = dev.MV_CC_CreateDevice_NET(device)
                If MyCamera.MV_OK <> nRet Then
                    dev.MV_CC_DestroyDevice_NET()
                    Continue Do

                End If

                nRet = dev.MV_CC_OpenDevice_NET()
                If (MyCamera.MV_OK <> nRet) Then
                    dev.MV_CC_DestroyDevice_NET()
                    Continue Do
                Else
                    nRet = dev.MV_CC_StartGrabbing_NET()
                    If MyCamera.MV_OK <> nRet Then
                        MsgBox("Start grab failed")
                    End If
                    nRet = dev.MV_CC_Display_NET(picHandle)
                    If MyCamera.MV_OK <> nRet Then
                        MsgBox("Display failed")
                    End If

                    m_bIsOpen = True
                End If

            Loop
        End If
    End Sub


    ' ch:枚举设备按钮操作 | en:Button operation for device enum
    Private Sub ButtonEnumDevice_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ButtonEnumDevice.Click
        ' ch:清空设备列表 | en:Clear up device list
        ComboBoxDeviceList.Items.Clear()
        ComboBoxDeviceList.SelectedIndex = -1

        Dim Info As String
        Dim nRet As Int32 = MyCamera.MV_OK
        ' ch:枚举设备 | en:Enumerate devices
        nRet = dev.MV_CC_EnumDevices_NET((MyCamera.MV_GIGE_DEVICE Or MyCamera.MV_USB_DEVICE), m_stDeviceInfoList)
        If MyCamera.MV_OK <> nRet Then
            MsgBox("枚举设备失败!" + Convert.ToString(nRet))
            Return
        End If

        If (0 = m_stDeviceInfoList.nDeviceNum) Then
            MsgBox("No Find Gige | Usb Device !")
            Return
        End If

        ' ch:将设备信息放到设备列表中 | en:Put device information in the device list
        Dim i As Int32
        For i = 0 To m_stDeviceInfoList.nDeviceNum - 1
            Dim stDeviceInfo As MyCamera.MV_CC_DEVICE_INFO = New MyCamera.MV_CC_DEVICE_INFO
            stDeviceInfo = CType(Marshal.PtrToStructure(m_stDeviceInfoList.pDeviceInfo(i), GetType(MyCamera.MV_CC_DEVICE_INFO)), MyCamera.MV_CC_DEVICE_INFO)
            If (MyCamera.MV_GIGE_DEVICE = stDeviceInfo.nTLayerType) Then
                Dim stGigeInfoPtr As IntPtr = Marshal.AllocHGlobal(216)
                Marshal.Copy(stDeviceInfo.SpecialInfo.stGigEInfo, 0, stGigeInfoPtr, 216)
                Dim stGigeInfo As MyCamera.MV_GIGE_DEVICE_INFO
                stGigeInfo = CType(Marshal.PtrToStructure(stGigeInfoPtr, GetType(MyCamera.MV_GIGE_DEVICE_INFO)), MyCamera.MV_GIGE_DEVICE_INFO)
                Dim nIpByte1 As UInt32 = (stGigeInfo.nCurrentIp And &HFF000000) >> 24
                Dim nIpByte2 As UInt32 = (stGigeInfo.nCurrentIp And &HFF0000) >> 16
                Dim nIpByte3 As UInt32 = (stGigeInfo.nCurrentIp And &HFF00) >> 8
                Dim nIpByte4 As UInt32 = (stGigeInfo.nCurrentIp And &HFF)

                Info = "DEV[" + Convert.ToString(i) + "] NAME[" + stGigeInfo.chUserDefinedName + "]IP[" + nIpByte1.ToString() + "." + nIpByte2.ToString() + "." + nIpByte3.ToString() + "." + nIpByte4.ToString() + "]"
                ComboBoxDeviceList.Items.Add(Info)
            Else
                Dim stUsbInfoPtr As IntPtr = Marshal.AllocHGlobal(540)
                Marshal.Copy(stDeviceInfo.SpecialInfo.stUsb3VInfo, 0, stUsbInfoPtr, 540)
                Dim stUsbInfo As MyCamera.MV_USB3_DEVICE_INFO
                stUsbInfo = CType(Marshal.PtrToStructure(stUsbInfoPtr, GetType(MyCamera.MV_USB3_DEVICE_INFO)), MyCamera.MV_USB3_DEVICE_INFO)
                Info = "DEV[" + Convert.ToString(i) + "] NAME[" + stUsbInfo.chUserDefinedName + "]Model[" + stUsbInfo.chSerialNumber + "]"
                ComboBoxDeviceList.Items.Add(Info)
            End If
        Next i

        ' ch:设置各个控件的可见度 | en:Set visibility of controls
        GroupBoxInit.Enabled = True
        GroupBoxDeviceControl.Enabled = False
        GroupBoxGrabImage.Enabled = False

        ComboBoxDeviceList.Enabled = True
        ButtonEnumDevice.Enabled = True
        ButtonOpenDevice.Enabled = False
        ButtonCloseDevice.Enabled = False
        RadioButtonTriggerOff.Enabled = False
        RadioButtonTriggerOn.Enabled = False
        ButtonStartGrabbing.Enabled = False
        ButtonStopGrabbing.Enabled = False
        CheckBoxSoftware.Enabled = False
        ButtonSoftwareOnce.Enabled = False
    End Sub

    ' ch:打开设备按钮操作 | en:Button operation for opening device
    Private Sub ButtonOpenDevice_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ButtonOpenDevice.Click
        ' ch:创建句柄 | en:Create handle
        Dim nRet As Int32 = MyCamera.MV_OK
        m_stDeviceInfo = CType(Marshal.PtrToStructure(m_stDeviceInfoList.pDeviceInfo(m_nDeviceIndex), GetType(MyCamera.MV_CC_DEVICE_INFO)), MyCamera.MV_CC_DEVICE_INFO)
        nRet = dev.MV_CC_CreateDevice_NET(m_stDeviceInfo)
        If MyCamera.MV_OK <> nRet Then
            MsgBox("Create handle failed")
            Return
        End If

        '打 ch:开设备 | en:Open device
        nRet = dev.MV_CC_OpenDevice_NET()
        If MyCamera.MV_OK <> nRet Then
            dev.MV_CC_DestroyDevice_NET()
            MsgBox("Open device failed")
            Return
        End If

        ' ch:探测网络最佳包大小(只对GigE相机有效) | en:Detection network optimal package size(It only works for the GigE camera)
        If m_stDeviceInfo.nTLayerType = MyCamera.MV_GIGE_DEVICE Then
            Dim nPacketSize As Int32
            nPacketSize = dev.MV_CC_GetOptimalPacketSize_NET()
            If nPacketSize > 0 Then
                nRet = dev.MV_CC_SetIntValue_NET("GevSCPSPacketSize", nPacketSize)
                If 0 <> nRet Then
                    Console.WriteLine("Warning: Set Packet Size failed:{0:x8}", nRet)
                End If
            Else
                Console.WriteLine("Warning: Get Packet Size failed:{0:x8}", nPacketSize)
            End If
        End If

        ' ch:获取触发模式 | en:Acquire trigger mode
        Dim stTriggerMode As MyCamera.MVCC_ENUMVALUE = New MyCamera.MVCC_ENUMVALUE
        nRet = dev.MV_CC_GetEnumValue_NET("TriggerMode", stTriggerMode)
        If MyCamera.MV_OK <> nRet Then
            MsgBox("Get trigger mode failed")
            Return
        End If
        If 0 = stTriggerMode.nCurValue Then
            RadioButtonTriggerOff.Checked = True
            RadioButtonTriggerOn.Checked = False
        Else
            RadioButtonTriggerOff.Checked = False
            RadioButtonTriggerOn.Checked = True
        End If

        ' ch:获取触发源 | en:Acquire trigger source
        Dim stTriggerSource As MyCamera.MVCC_ENUMVALUE = New MyCamera.MVCC_ENUMVALUE
        nRet = dev.MV_CC_GetEnumValue_NET("TriggerSource", stTriggerSource)
        If MyCamera.MV_OK <> nRet Then
            MsgBox("Get trigger source failed")
            Return
        End If

        ' ch:注册异常回调函数 | en:Register Exception Callback
        nRet = dev.MV_CC_RegisterExceptionCallBack_NET(cbCallback, IntPtr.Zero)
        If MyCamera.MV_OK <> nRet Then
            MsgBox("Register expection callback failed")
            Return
        End If
        GC.KeepAlive(cbCallback)

        If MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE = stTriggerSource.nCurValue Then
            CheckBoxSoftware.Checked = True
        Else
            CheckBoxSoftware.Checked = False
        End If

        If RadioButtonTriggerOn.Checked Then
            RadioButtonTriggerOn.Enabled = False
            RadioButtonTriggerOff.Enabled = True
        Else
            RadioButtonTriggerOn.Enabled = True
            RadioButtonTriggerOff.Enabled = False
        End If

        If (RadioButtonTriggerOn.Checked) Then
            CheckBoxSoftware.Enabled = True
        Else
            CheckBoxSoftware.Enabled = False
        End If

        If (RadioButtonTriggerOn.Checked And CheckBoxSoftware.Checked) Then
            ButtonSoftwareOnce.Enabled = True
        Else
            ButtonSoftwareOnce.Enabled = False
        End If

        GroupBoxDeviceControl.Enabled = True
        GroupBoxGrabImage.Enabled = True

        ComboBoxDeviceList.Enabled = False
        ButtonOpenDevice.Enabled = False
        ButtonCloseDevice.Enabled = True
        ButtonStartGrabbing.Enabled = True
        ButtonStopGrabbing.Enabled = False

    End Sub

    ' ch:关闭设备 | en:Close device
    Private Sub ButtonCloseDevice_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ButtonCloseDevice.Click

        Dim nRet As Int32 = MyCamera.MV_OK
        nRet = dev.MV_CC_CloseDevice_NET()
        If MyCamera.MV_OK <> nRet Then
            MsgBox("Fail to close device")
            Return
        End If

        nRet = dev.MV_CC_DestroyDevice_NET()
        If MyCamera.MV_OK <> nRet Then
            MsgBox("Fail to destroy handle")
            Return
        End If

        ' ch:设置各个控件的可见性 | en:Set visibility of controls
        ComboBoxDeviceList.Enabled = True
        ButtonOpenDevice.Enabled = True
        ButtonCloseDevice.Enabled = False

        GroupBoxGrabImage.Enabled = False
        ButtonStartGrabbing.Enabled = False
        ButtonStopGrabbing.Enabled = False
        ButtonSoftwareOnce.Enabled = False

    End Sub

    ' ch:刚加载时的初始化 | en:Initialization for loading
    Private Sub Thread_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        ' ch:设置各个控件的可见性 | en:Set visibility of controls
        GroupBoxInit.Enabled = True
        GroupBoxDeviceControl.Enabled = False
        GroupBoxGrabImage.Enabled = False

        ComboBoxDeviceList.Enabled = False
        ButtonEnumDevice.Enabled = True
        ButtonOpenDevice.Enabled = False
        ButtonCloseDevice.Enabled = False
        RadioButtonTriggerOff.Enabled = False
        RadioButtonTriggerOn.Enabled = False
        ButtonStartGrabbing.Enabled = False
        ButtonStopGrabbing.Enabled = False
        CheckBoxSoftware.Enabled = False
        ButtonSoftwareOnce.Enabled = False

        picHandle = PictureBoxDisplay.Handle
    End Sub

    ' ch:设置连续采集 | en:Set continuous acquisition
    Private Sub RadioButtonTriggerOff_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButtonTriggerOff.CheckedChanged
        '关闭触发 | en:Close trigger mode
        Dim nRet As Int32 = MyCamera.MV_OK
        nRet = dev.MV_CC_SetEnumValue_NET("TriggerMode", MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_OFF)
        If MyCamera.MV_OK <> nRet Then
            MsgBox("Fail to close trigger mode")
        End If

        RadioButtonTriggerOff.Enabled = False
        RadioButtonTriggerOn.Enabled = True
        If (RadioButtonTriggerOn.Checked) Then
            CheckBoxSoftware.Enabled = True
        Else
            CheckBoxSoftware.Enabled = False
        End If

        If (RadioButtonTriggerOn.Checked And CheckBoxSoftware.Checked) Then
            ButtonSoftwareOnce.Enabled = True
        Else
            ButtonSoftwareOnce.Enabled = False
        End If

    End Sub

    ' ch:设置触发模式 | en:Set trigger mode
    Private Sub RadioButtonTriggerOn_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RadioButtonTriggerOn.CheckedChanged
        ' ch:开启触发 | en:Open trigger mode
        Dim nRet As Int32 = MyCamera.MV_OK
        nRet = dev.MV_CC_SetEnumValue_NET("TriggerMode", MyCamera.MV_CAM_TRIGGER_MODE.MV_TRIGGER_MODE_ON)
        If MyCamera.MV_OK <> nRet Then
            MsgBox("Fail to close trigger mode")
        End If

        RadioButtonTriggerOff.Enabled = True
        RadioButtonTriggerOn.Enabled = False
        If (RadioButtonTriggerOn.Checked) Then
            CheckBoxSoftware.Enabled = True
        Else
            CheckBoxSoftware.Enabled = False
        End If

        If (RadioButtonTriggerOn.Checked And CheckBoxSoftware.Checked) Then
            ButtonSoftwareOnce.Enabled = True
        Else
            ButtonSoftwareOnce.Enabled = False
        End If

    End Sub

    ' ch:开启采集 | en:Start grabbing
    Private Sub ButtonStartGrabbing_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ButtonStartGrabbing.Click
        Dim nRet As Int32
        nRet = dev.MV_CC_StartGrabbing_NET()
        If MyCamera.MV_OK <> nRet Then
            MsgBox("Fail to start grabbing")
        End If
        nRet = dev.MV_CC_Display_NET(PictureBoxDisplay.Handle)
        If MyCamera.MV_OK <> nRet Then
            MsgBox("Fail to display image")
        End If

        ButtonStartGrabbing.Enabled = False
        ButtonStopGrabbing.Enabled = True

    End Sub

    ' ch:停止采集 | en:Stop grabbing
    Private Sub ButtonStopGrabbing_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ButtonStopGrabbing.Click
        Dim nRet As Int32
        nRet = dev.MV_CC_StopGrabbing_NET()
        If MyCamera.MV_OK <> nRet Then
            MsgBox("Fail to stop grabbing")
        End If
        ButtonStartGrabbing.Enabled = True
        ButtonStopGrabbing.Enabled = False

    End Sub

    ' ch:软触发模式 | en:Software trigger mode
    Private Sub CheckBoxSoftware_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBoxSoftware.CheckedChanged
        If (CheckBoxSoftware.Checked) Then
            ' ch:设置软触发 | en:Set software trigger
            Dim nRet As Int32
            nRet = dev.MV_CC_SetEnumValue_NET("TriggerSource", MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_SOFTWARE)
            If MyCamera.MV_OK <> nRet Then
                MsgBox("Fail to set software trigger")
            End If
        Else
            ' ch:设置硬触发 | en:Set hardware trigger
            Dim nRet As Int32
            nRet = dev.MV_CC_SetEnumValue_NET("TriggerSource", MyCamera.MV_CAM_TRIGGER_SOURCE.MV_TRIGGER_SOURCE_LINE0)
            If MyCamera.MV_OK <> nRet Then
                MsgBox("Fail to set hardware trigger")
            End If
        End If

        CheckBoxSoftware.Enabled = True
        If (RadioButtonTriggerOn.Checked And CheckBoxSoftware.Checked) Then
            ButtonSoftwareOnce.Enabled = True
        Else
            ButtonSoftwareOnce.Enabled = False
        End If

    End Sub

    ' ch:软触发一次 | en:Software trigger once
    Private Sub ButtonSoftwareOnce_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ButtonSoftwareOnce.Click
        Dim nRet As Int32
        nRet = dev.MV_CC_SetCommandValue_NET("TriggerSoftware")
        If MyCamera.MV_OK <> nRet Then
            MsgBox("Get PayloadSize failed")
        End If
    End Sub

    ' ch:保存bmp图片 | en:Save image
    Private Sub ButtonSaveBmp_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)

        Dim nRet As Int32
        Dim stIntValue As MyCamera.MVCC_INTVALUE = New MyCamera.MVCC_INTVALUE
        nRet = dev.MV_CC_GetIntValue_NET("PayloadSize", stIntValue)
        If MyCamera.MV_OK <> nRet Then
            MsgBox("Get PayloadSize failed!")
        End If

        If stIntValue.nCurValue > m_nBufSizeForDriver Then
            m_nBufSizeForDriver = stIntValue.nCurValue
            ReDim m_pBufForDriver(m_nBufSizeForDriver)

            ' ch:同时对保存图像的缓存做大小判断处理
            ' BMP图片大小：width * height * 3 + 2048(预留BMP头大小)
            ' en:Determine the buffer size to save image
            ' BMP image size: width * height * 3 + 2048 (Reserved BMP header size)
            m_nBufSizeForSaveImage = m_nBufSizeForDriver * 3 + 2048
            ReDim m_pBufForSaveImage(m_nBufSizeForSaveImage)
        End If

        Dim pData As IntPtr = Marshal.UnsafeAddrOfPinnedArrayElement(m_pBufForDriver, 0)
        nRet = dev.MV_CC_GetOneFrameTimeout_NET(pData, m_nBufSizeForDriver, m_stFrameOutInfo, 1000)
        If MyCamera.MV_OK = nRet Then
            Dim pImage As IntPtr = Marshal.UnsafeAddrOfPinnedArrayElement(m_pBufForSaveImage, 0)
            Dim stSaveImageParam As MyCamera.MV_SAVE_IMAGE_PARAM_EX = New MyCamera.MV_SAVE_IMAGE_PARAM_EX()
            stSaveImageParam.pData = pData
            stSaveImageParam.nDataLen = m_stFrameOutInfo.nFrameLen
            stSaveImageParam.enPixelType = m_stFrameOutInfo.enPixelType
            stSaveImageParam.nWidth = m_stFrameOutInfo.nWidth
            stSaveImageParam.nHeight = m_stFrameOutInfo.nHeight
            stSaveImageParam.enImageType = MyCamera.MV_SAVE_IAMGE_TYPE.MV_Image_Bmp
            stSaveImageParam.nBufferSize = m_nBufSizeForSaveImage
            stSaveImageParam.pImageBuffer = pImage
            stSaveImageParam.nJpgQuality = 80

            nRet = dev.MV_CC_SaveImageEx_NET(stSaveImageParam)
            If (MyCamera.MV_OK <> nRet) Then
                MsgBox("Fail to convert image format")
                Return
            End If

            ' ch:写文件 | en:Write file
            If stSaveImageParam.nImageLen <> m_nByteImageBuffer Then
                m_nByteImageBuffer = stSaveImageParam.nImageLen
                ReDim m_byteImageBuffer(m_nByteImageBuffer)
            End If
            Marshal.Copy(pImage, m_byteImageBuffer, 0, stSaveImageParam.nImageLen)
            My.Computer.FileSystem.WriteAllBytes("Image.bmp", m_byteImageBuffer, False)
            MsgBox("Save BMP succeed")
            Return
        End If
        MsgBox("Fail to get image stream within 1s")

    End Sub


    ' ch:选择不同相机 | en:Select different cameras
    Private Sub ComboBoxDeviceList_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ComboBoxDeviceList.SelectedIndexChanged
        m_nDeviceIndex = ComboBoxDeviceList.SelectedIndex

        ' ch:设置各个控件的可见性 | en:Set visibility of controls
        GroupBoxInit.Enabled = True
        GroupBoxDeviceControl.Enabled = True
        GroupBoxGrabImage.Enabled = False

        ComboBoxDeviceList.Enabled = True
        ButtonEnumDevice.Enabled = True
        ButtonOpenDevice.Enabled = True
        ButtonCloseDevice.Enabled = False
        RadioButtonTriggerOff.Enabled = False
        RadioButtonTriggerOn.Enabled = False
        ButtonStartGrabbing.Enabled = False
        ButtonStopGrabbing.Enabled = False
        CheckBoxSoftware.Enabled = False
        ButtonSoftwareOnce.Enabled = False
    End Sub

    Private Sub Thread_FormClosing(ByVal sender As System.Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles MyBase.FormClosing
        ' ch:关闭设备 | en:Close device
        Dim nRet As Int32 = MyCamera.MV_OK
        nRet = dev.MV_CC_CloseDevice_NET()
        ' ch:销毁句柄 | en:Destroy handle
        dev.MV_CC_DestroyDevice_NET()
    End Sub
End Class