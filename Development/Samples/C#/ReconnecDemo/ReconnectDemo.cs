
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MvCamCtrl.NET;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace ReconnectDemo
{
    public partial class ReconnectDemo : Form
    {
        MyCamera.MV_CC_DEVICE_INFO_LIST m_pDeviceList;
        private MyCamera m_pMyCamera;
        bool m_bGrabbing;

        byte[] m_buffer = new byte[3072 * 2048 * 3];
        byte[] m_arrImageBuffer = new byte[3072 * 2048 * 3];

        Boolean m_bDisConnect = false;
        Thread ReConnectThread = null;    // 设备重连线程ch: | en:

        MyCamera.cbExceptiondelegate pCallBackFunc;

        public ReconnectDemo()
        {
            InitializeComponent();
            m_pDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
            m_pMyCamera = new MyCamera();
            m_bGrabbing = false;
            DeviceListAcq();
            Control.CheckForIllegalCrossThreadCalls = false;
            pCallBackFunc = new MyCamera.cbExceptiondelegate(cbExceptiondelegate);
        }

        // ch:显示错误信息 | en:Show error message
        private void ShowErrorMsg(string csMessage, int nErrorNum)
        {
            string errorMsg;
            if (nErrorNum == 0)
            {
                errorMsg = csMessage;
            }
            else
            {
                errorMsg = csMessage + ": Error =" + String.Format("{0:X}", nErrorNum);
            }

            switch (nErrorNum)
            {
                case MyCamera.MV_E_HANDLE: errorMsg += " Error or invalid handle "; break;
                case MyCamera.MV_E_SUPPORT: errorMsg += " Not supported function "; break;
                case MyCamera.MV_E_BUFOVER: errorMsg += " Cache is full "; break;
                case MyCamera.MV_E_CALLORDER: errorMsg += " Function calling order error "; break;
                case MyCamera.MV_E_PARAMETER: errorMsg += " Incorrect parameter "; break;
                case MyCamera.MV_E_RESOURCE: errorMsg += " Applying resource failed "; break;
                case MyCamera.MV_E_NODATA: errorMsg += " No data "; break;
                case MyCamera.MV_E_PRECONDITION: errorMsg += " Precondition error, or running environment changed "; break;
                case MyCamera.MV_E_VERSION: errorMsg += " Version mismatches "; break;
                case MyCamera.MV_E_NOENOUGH_BUF: errorMsg += " Insufficient memory "; break;
                case MyCamera.MV_E_UNKNOW: errorMsg += " Unknown error "; break;
                case MyCamera.MV_E_GC_GENERIC: errorMsg += " General error "; break;
                case MyCamera.MV_E_GC_ACCESS: errorMsg += " Node accessing condition error "; break;
                case MyCamera.MV_E_ACCESS_DENIED: errorMsg += " No permission "; break;
                case MyCamera.MV_E_BUSY: errorMsg += " Device is busy, or network disconnected "; break;
                case MyCamera.MV_E_NETER: errorMsg += " Network error "; break;
            }

            MessageBox.Show(errorMsg, "PROMPT");
        }

        private void bnEnum_Click(object sender, EventArgs e)
        {
            DeviceListAcq();
        }

        private void DeviceListAcq()
        {
            int nRet;
            //ch:创建设备列表 | en:Create Device List
            System.GC.Collect();
            cbDeviceList.Items.Clear();
            nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref m_pDeviceList);
            if (0 != nRet)
            {
                ShowErrorMsg("EnumDevices Failed", nRet);
                return;
            }

            //ch:在窗体列表中显示设备名 | en:Display device name in the form list
            for (int i = 0; i < m_pDeviceList.nDeviceNum; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO device = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_pDeviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));
                if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                {
                    IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stGigEInfo, 0);
                    MyCamera.MV_GIGE_DEVICE_INFO gigeInfo = (MyCamera.MV_GIGE_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_GIGE_DEVICE_INFO));
                    if (gigeInfo.chUserDefinedName != "")
                    {
                        cbDeviceList.Items.Add(gigeInfo.chUserDefinedName);
                    }
                    else
                    {
                        cbDeviceList.Items.Add(gigeInfo.chModelName + "(" + gigeInfo.chSerialNumber + ")");
                    }
                }
                else if (device.nTLayerType == MyCamera.MV_USB_DEVICE)
                {
                    IntPtr buffer = Marshal.UnsafeAddrOfPinnedArrayElement(device.SpecialInfo.stUsb3VInfo, 0);
                    MyCamera.MV_USB3_DEVICE_INFO usbInfo = (MyCamera.MV_USB3_DEVICE_INFO)Marshal.PtrToStructure(buffer, typeof(MyCamera.MV_USB3_DEVICE_INFO));
                    if (usbInfo.chUserDefinedName != "")
                    {
                        cbDeviceList.Items.Add(usbInfo.chUserDefinedName);
                    }
                    else
                    {
                        cbDeviceList.Items.Add(usbInfo.chModelName + "(" + usbInfo.chSerialNumber + ")");
                    }
                }
            }

            //ch:选择第一项 | en:Select the first item
            if (m_pDeviceList.nDeviceNum != 0)
            {
                cbDeviceList.SelectedIndex = 0;
            }
        }


        // ch:回调函数 | en:Callback function
        private void cbExceptiondelegate(uint nMsgType, IntPtr pUser)
        {
            if (nMsgType == MyCamera.MV_EXCEPTION_DEV_DISCONNECT)
            {
                m_bDisConnect = false;

                m_bGrabbing = false;
                int nRet = -1;

                DeInitCamera();

                if (m_pDeviceList.nDeviceNum == 0 || cbDeviceList.SelectedIndex == -1)
                {
                    ShowErrorMsg("No device, please Select", 0);
                    return;
                }

                // ch:获取选择的设备信息 | en:Get Used Device Info
                MyCamera.MV_CC_DEVICE_INFO device =
                    (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_pDeviceList.pDeviceInfo[cbDeviceList.SelectedIndex],
                                                                  typeof(MyCamera.MV_CC_DEVICE_INFO));

                // ch:打开设备 | en:Open Device
                while (!m_bDisConnect)
                {
                    nRet = m_pMyCamera.MV_CC_CreateDevice_NET(ref device);
                    if (MyCamera.MV_OK != nRet)
                    {
                        ShowErrorMsg("Create Camera failed", nRet);
                        m_pMyCamera.MV_CC_DestroyDevice_NET();
                        continue;
                    }

                    nRet = m_pMyCamera.MV_CC_OpenDevice_NET();
                    if (MyCamera.MV_OK != nRet)
                    {
                        m_pMyCamera.MV_CC_DestroyDevice_NET();
                        continue;
                    }

                    else
                    {
                        nRet = InitCamera();
                        if (MyCamera.MV_OK != nRet)
                        {
                            m_pMyCamera.MV_CC_DestroyDevice_NET();
                            continue;
                        }
                        m_bDisConnect = true;
                    }
                }
            }
        }

        private int InitCamera()
        {
            int nRet = -1;
            nRet = m_pMyCamera.MV_CC_RegisterExceptionCallBack_NET(pCallBackFunc, IntPtr.Zero);
            GC.KeepAlive(pCallBackFunc);
            if (MyCamera.MV_OK != nRet)
            {
                return nRet;
            }

            // ch:控件操作 | en:Control operation
            SetCtrlWhenOpen();

            // ch:开始采集 | en:Start Grabbing
            nRet = m_pMyCamera.MV_CC_StartGrabbing_NET();
            if (MyCamera.MV_OK != nRet)
            {
                return nRet;
            }

            // ch:控件操作 | en:Control Operation
            SetCtrlWhenStartGrab();

            // ch:标志位置位true | en:Set flag bit true
            m_bGrabbing = true;

            // ch:显示 | en:Display
            nRet = m_pMyCamera.MV_CC_Display_NET(pictureBox1.Handle);
            if (MyCamera.MV_OK != nRet)
            {
                return nRet;
            }

            return MyCamera.MV_OK;
        }


        private void DeInitCamera()
        {
            // ch:取流标志位清零 | en:Zero setting grabbing flag bit
            m_bGrabbing = false;

            Thread.Sleep(1000);

            // ch:停止采集 | en:Stop Grabbing
            m_pMyCamera.MV_CC_StopGrabbing_NET();

            // ch:控件操作 | en:Control Operation
            SetCtrlWhenStopGrab();

            // ch:关闭设备 | en:Close Device
            int nRet = m_pMyCamera.MV_CC_CloseDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                return ;
            }

            nRet = m_pMyCamera.MV_CC_DestroyDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                return;
            }
            // ch:控件操作 | en:Control Operation
            SetCtrlWhenClose();
        }

        private void SetCtrlWhenOpen()
        {
            bnOpen.Enabled = false;
            bnClose.Enabled = true;
            bnStartGrab.Enabled = true;
            bnStopGrab.Enabled = false;
            bnContinuesMode.Enabled = true;
            bnContinuesMode.Checked = true;
            bnTriggerMode.Enabled = true;
            cbSoftTrigger.Enabled = false;
            bnTriggerExec.Enabled = false;
        }

        private void bnOpen_Click(object sender, EventArgs e)
        {
            if (m_pDeviceList.nDeviceNum == 0 || cbDeviceList.SelectedIndex == -1)
            {
                ShowErrorMsg("No Device,please Select", 0);
                return;
            }
            int nRet = -1;

            //ch:获取选择的设备信息 | en:Get Used Device Info
            MyCamera.MV_CC_DEVICE_INFO device = 
                (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_pDeviceList.pDeviceInfo[cbDeviceList.SelectedIndex],
                                                              typeof(MyCamera.MV_CC_DEVICE_INFO));
            if (m_pMyCamera == null)
            {
                m_pMyCamera = new MyCamera();
            }

            //ch:打开设备 | en:Open Device
            nRet = m_pMyCamera.MV_CC_CreateDevice_NET(ref device);
            if (MyCamera.MV_OK != nRet)
            {
                ShowErrorMsg("Create Camera failed", nRet);
                m_pMyCamera.MV_CC_CloseDevice_NET();
                return;
            }

            nRet = m_pMyCamera.MV_CC_OpenDevice_NET();
            if (MyCamera.MV_OK != nRet)
            {
                ShowErrorMsg("MV_CC_OpenDevice_NET Failed", nRet);
                m_pMyCamera.MV_CC_CloseDevice_NET();
                return;
            }

            // ch:探测网络最佳包大小(只对GigE相机有效) | en:Detection network optimal package size(It only works for the GigE camera)
            if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                int nPacketSize = m_pMyCamera.MV_CC_GetOptimalPacketSize_NET();
                if (nPacketSize > 0)
                {
                    nRet = m_pMyCamera.MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
                    if (nRet != MyCamera.MV_OK)
                    {
                        Console.WriteLine("Warning: Set Packet Size failed {0:x8}", nRet);
                    }
                }
                else
                {
                    Console.WriteLine("Warning: Get Packet Size failed {0:x8}", nPacketSize);
                }
            }

            // ch:注册异常回调函数 | en:Register Exception Callback
            nRet = m_pMyCamera.MV_CC_RegisterExceptionCallBack_NET(pCallBackFunc, IntPtr.Zero);
            if (MyCamera.MV_OK != nRet)
            {
                ShowErrorMsg("Register expection callback failed", nRet);
            }
            GC.KeepAlive(pCallBackFunc);

            //ch:控件操作 | en:Control Operation
            SetCtrlWhenOpen();
        }


        private void SetCtrlWhenClose()
        {
            bnOpen.Enabled = true;

            bnClose.Enabled = false;
            bnStartGrab.Enabled = false;
            bnStopGrab.Enabled = false;
            bnContinuesMode.Enabled = false;
            bnTriggerMode.Enabled = false;
            cbSoftTrigger.Enabled = false;
            bnTriggerExec.Enabled = false;
        }

        private void bnClose_Click(object sender, EventArgs e)
        {

            //ch:关闭设备 | en:Close Device 
            m_pMyCamera.MV_CC_CloseDevice_NET();
            m_pMyCamera = null;
            GC.Collect();
            //ch:控件操作 | en:Control Operation
            SetCtrlWhenClose();

            //ch:取流标志位清零 | en:Zero setting grabbing flag bit
            m_bGrabbing = false;
        }

        private void bnContinuesMode_CheckedChanged(object sender, EventArgs e)
        {
            if (bnContinuesMode.Checked)
            {
                m_pMyCamera.MV_CC_SetEnumValue_NET("TriggerMode", 0);
                cbSoftTrigger.Enabled = false;
                bnTriggerExec.Enabled = false;
            }
            
        }

        private void bnTriggerMode_CheckedChanged(object sender, EventArgs e)
        {
            //ch:打开触发模式 | en:Open Trigger Mode
            if (bnTriggerMode.Checked)
            {
                m_pMyCamera.MV_CC_SetEnumValue_NET("TriggerMode", 1);

                //ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                //           1 - Line1;
                //           2 - Line2;
                //           3 - Line3;
                //           4 - Counter;
                //           7 - Software;
                if (cbSoftTrigger.Checked)
                {
                    m_pMyCamera.MV_CC_SetEnumValue_NET("TriggerSource", 7);
                    if (m_bGrabbing)
                    {
                        bnTriggerExec.Enabled = true;
                    }
                }
                else
                {
                    m_pMyCamera.MV_CC_SetEnumValue_NET("TriggerSource", 0);
                }
                cbSoftTrigger.Enabled = true;
            }
            
        }

        private void SetCtrlWhenStartGrab()
        {
            bnStartGrab.Enabled = false;
            bnStopGrab.Enabled = true;
 
            //bnTriggerExec.Enabled = true;
            if (bnTriggerMode.Checked && cbSoftTrigger.Checked)
            {
                bnTriggerExec.Enabled = true;
            }
        }

        private void bnStartGrab_Click(object sender, EventArgs e)
        {
            int nRet;

            //ch:开始采集 | en:Start Grabbing
            nRet = m_pMyCamera.MV_CC_StartGrabbing_NET();
            if (MyCamera.MV_OK != nRet)
            {
                ShowErrorMsg("MV_CC_StartGrabbing_NET Failed！", nRet);
                return;
            }

            //ch:控件操作 | en:Control Operation
            SetCtrlWhenStartGrab();

            //ch:标志位置位true | en:Set Position Bit true
            m_bGrabbing = true;

            //ch:显示 | en:Display
            nRet = m_pMyCamera.MV_CC_Display_NET(pictureBox1.Handle);
            if (MyCamera.MV_OK != nRet)
            {
                ShowErrorMsg("Display Failed！", nRet);
            }
        }

        private void cbSoftTrigger_CheckedChanged(object sender, EventArgs e)
        {
            if (cbSoftTrigger.Checked)
            {

                //ch:触发源设为软触发 | en:Set Trigger Source As Software
                m_pMyCamera.MV_CC_SetEnumValue_NET("TriggerSource", 7);
                if (m_bGrabbing)
                {
                    bnTriggerExec.Enabled = true;
                }
            }
            else
            {
                m_pMyCamera.MV_CC_SetEnumValue_NET("TriggerSource", 0);
                bnTriggerExec.Enabled = false;
            }
        }

        private void bnTriggerExec_Click(object sender, EventArgs e)
        {
            int nRet;

            //ch:触发命令 | en:Trigger Command
            nRet = m_pMyCamera.MV_CC_SetCommandValue_NET("TriggerSoftware");
            if (MyCamera.MV_OK != nRet)
            {
                ShowErrorMsg("MV_CC_SetCommandValue_NET Failed", nRet);
            }
        }

        private void SetCtrlWhenStopGrab()
        {
            bnStartGrab.Enabled = true;
            bnStopGrab.Enabled = false;

            bnTriggerExec.Enabled = false;
        }
        private void bnStopGrab_Click(object sender, EventArgs e)
        {
            int nRet = -1;
            //ch:停止采集 | en:Stop Grabbing
            nRet = m_pMyCamera.MV_CC_StopGrabbing_NET();
            if (nRet != MyCamera.MV_OK)
            {
                ShowErrorMsg("MV_CC_StopGrabbing_NET Failed", nRet);
            }

            //ch:标志位设为false | en:Set Flag Bit false
            m_bGrabbing = false;

            //ch:控件操作 | en:Control Operation
            SetCtrlWhenStopGrab();
        }
    }
}
