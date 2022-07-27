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

namespace MultipleDemo
{
    struct _MV_MATCH_INFO_NET_DETECT_
    {
        public UInt64 nReviceDataSize;    // 已接收数据大小  [统计StartGrabbing和StopGrabbing之间的数据量]
        public UInt32 nLostPacketCount;   // 丢失的包数量
        public uint nLostFrameCount;    // 丢帧数量
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public uint[] nReserved;          // 保留
    };

    //using TextCallback = SetTextCallback ;
    public partial class MultipleDemo : Form
    {
        public struct CAMERA//定义相机结构体
        {
            public MyCamera Cam_Info;
            public UInt32 m_nBufSizeForSaveImage;
            public byte[] m_pBufForSaveImage;         // 用于保存图像的缓存
        }

        MyCamera.cbOutputdelegate cbImage;
        MyCamera.MV_CC_DEVICE_INFO_LIST m_pDeviceList;
        private CAMERA[] m_pMyCamera;
        bool m_bGrabbing;
        int m_nCanOpenDeviceNum;        // ch:设备使用数量 | en:Used Device Number
        int m_nDevNum;        // ch:在线设备数量 | en:Online Device Number
        int[] m_nFrames;      // ch:帧数 | en:Frame Number
        bool m_bTimerFlag;     // ch:定时器开始计时标志位 | en:Timer Start Timing Flag Bit
        bool[] m_bSaveImg;    // ch:保存图片标志位 | en:Save Image Flag Bit
        IntPtr []m_hDisplayHandle;

        public MultipleDemo()
        {
            InitializeComponent();
            m_pDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
            m_bGrabbing = false;
            m_nCanOpenDeviceNum = 0;
            m_nDevNum = 0;
            DeviceListAcq();
            m_pMyCamera = new CAMERA[4];
            for (int i = 0; i < 4; i++)
            {
                m_pMyCamera[i].m_nBufSizeForSaveImage = 3072 * 2048 * 3 * 3 + 2048;
                m_pMyCamera[i].m_pBufForSaveImage = new byte[3072 * 2048 * 3 * 3 + 2048];
            }
                m_nFrames = new int[4];
            cbImage = new MyCamera.cbOutputdelegate(ImageCallBack1);
            m_bTimerFlag = false;
            m_bSaveImg = new bool[4];
            m_hDisplayHandle = new IntPtr[4];
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

        public void ResetMember()
        {
            m_pDeviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
            m_bGrabbing = false;
            m_nCanOpenDeviceNum = 0;
            m_nDevNum = 0;
            DeviceListAcq();
            m_pMyCamera = new CAMERA[4];
            for (int i = 0; i < 4; i++)
            {
                m_pMyCamera[i].Cam_Info = new MyCamera();
                m_pMyCamera[i].m_nBufSizeForSaveImage = 3072 * 2048 * 3 * 3 + 2048;
                m_pMyCamera[i].m_pBufForSaveImage = new byte[3072 * 2048 * 3 * 3 + 2048];
            }
            m_nFrames = new int[4];
            cbImage = new MyCamera.cbOutputdelegate(ImageCallBack1);
            m_bTimerFlag = false;
            m_bSaveImg = new bool[4];
            m_hDisplayHandle = new IntPtr[4];
        }

        // ch:枚举设备 | en:Create Device List
        private void DeviceListAcq()
        {
            int nRet;
            
            System.GC.Collect();
            nRet = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref m_pDeviceList);
            if (0 != nRet)
            {
                ShowErrorMsg("Enumerate devices fail!",0);
                return;
            }

            m_nDevNum = (int)m_pDeviceList.nDeviceNum;
            tbDevNum.Text = m_nDevNum.ToString("d");
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

            tbExposure.Enabled = true;
            tbGain.Enabled = true;
            bnSetParam.Enabled = true;
        }

        // ch:初始化、打开相机 | en:Initialization and open devices
        private void bnOpen_Click(object sender, EventArgs e)
        {
            bool bOpened =false;
            // ch:判断输入格式是否正确 | en:Determine whether the input format is correct
            try
            {
                int.Parse(tbUseNum.Text);
            }
            catch
            {
                ShowErrorMsg("Please enter correct format!",0);
                return;
            }
            // ch:获取使用设备的数量 | en:Get Used Device Number
            int nCameraUsingNum = int.Parse(tbUseNum.Text);
            // ch:参数检测 | en:Parameters inspection
            if (nCameraUsingNum <= 0)
            {
                nCameraUsingNum = 1;
            }
            if (nCameraUsingNum > 4)
            {
                nCameraUsingNum = 4;
            }

            int nRet = -1;

            for (int i = 0, j = 0; j < m_nDevNum; ++i, ++j)
            {
                //ch:获取选择的设备信息 | en:Get Selected Device Information
                m_pMyCamera[i] = new CAMERA();
                MyCamera.MV_CC_DEVICE_INFO device =
                    (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(m_pDeviceList.pDeviceInfo[j],
                                                              typeof(MyCamera.MV_CC_DEVICE_INFO));

                //ch:打开设备 | en:Open Device
                if (null == m_pMyCamera[i].Cam_Info)
                {
                    m_pMyCamera[i].Cam_Info = new MyCamera();
                    if (null == m_pMyCamera)
                    {
                        return ;
                    }
                }

                nRet = m_pMyCamera[i].Cam_Info.MV_CC_CreateDevice_NET(ref device);
                if (MyCamera.MV_OK != nRet)
                {
                    return;
                }

                nRet = m_pMyCamera[i].Cam_Info.MV_CC_OpenDevice_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    i--;
                }
                else
                {
                    m_nCanOpenDeviceNum++;

                    // ch:探测网络最佳包大小(只对GigE相机有效) | en:Detection network optimal package size(It only works for the GigE camera)
                    if (device.nTLayerType == MyCamera.MV_GIGE_DEVICE)
                    {
                        int nPacketSize = m_pMyCamera[i].Cam_Info.MV_CC_GetOptimalPacketSize_NET();
                        if (nPacketSize > 0)
                        {
                            nRet = m_pMyCamera[i].Cam_Info.MV_CC_SetIntValue_NET("GevSCPSPacketSize", (uint)nPacketSize);
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

                    m_pMyCamera[i].Cam_Info.MV_CC_SetEnumValue_NET("TriggerMode", 0);
                    m_pMyCamera[i].Cam_Info.MV_CC_RegisterImageCallBack_NET(cbImage, (IntPtr)i);
                    bOpened = true;
                    if (m_nCanOpenDeviceNum == nCameraUsingNum)
                    {
                        break;
                    }
                }

            }

            // ch:只要有一台设备成功打开 | en:As long as there is a device successfully opened
            if (bOpened)
            {
                tbUseNum.Text = m_nCanOpenDeviceNum.ToString();
                SetCtrlWhenOpen();
            }
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

            bnSaveBmp.Enabled = false;
            tbExposure.Enabled = false;
            tbGain.Enabled = false;
            bnSetParam.Enabled = false;
        }

        // ch:关闭相机 | en:Close Device
        private void bnClose_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < m_nCanOpenDeviceNum; ++i)
            {
                int nRet;

                nRet = m_pMyCamera[i].Cam_Info.MV_CC_CloseDevice_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    return;
                }

                nRet = m_pMyCamera[i].Cam_Info.MV_CC_DestroyDevice_NET();
                if (MyCamera.MV_OK != nRet)
                {
                    return;
                }
                    //m_pMyCamera[i].Close();
            }

            //控件操作 ch: | en:Control Operation
            SetCtrlWhenClose();
            // ch:取流标志位清零 | en:Zero setting grabbing flag bit
            m_bGrabbing = false;
            // ch:重置成员变量 | en:Reset member variable
            ResetMember();
        }

        // ch:连续采集 | en:
        private void bnContinuesMode_CheckedChanged(object sender, EventArgs e)
        {
            if (bnContinuesMode.Checked)
            {
                for (int i = 0; i < m_nCanOpenDeviceNum; ++i)
                {
                    m_pMyCamera[i].Cam_Info.MV_CC_SetEnumValue_NET("TriggerMode", 0);
                    cbSoftTrigger.Enabled = false;
                    bnTriggerExec.Enabled = false;
                    bnSaveBmp.Enabled = true;
                }
            }
        }

        // ch:打开触发模式 | en:Open Trigger Mode
        private void bnTriggerMode_CheckedChanged(object sender, EventArgs e)
        {
            if (bnTriggerMode.Checked)
            {
                for (int i = 0; i < m_nCanOpenDeviceNum; ++i)
                {
                    m_pMyCamera[i].Cam_Info.MV_CC_SetEnumValue_NET("TriggerMode", 1);

                    // ch:触发源选择:0 - Line0; | en:Trigger source select:0 - Line0;
                    //           1 - Line1;
                    //           2 - Line2;
                    //           3 - Line3;
                    //           4 - Counter;
                    //           7 - Software;
                    if (cbSoftTrigger.Checked)
                    {
                        m_pMyCamera[i].Cam_Info.MV_CC_SetEnumValue_NET("TriggerSource", 7);
                        if (m_bGrabbing)
                        {
                            bnTriggerExec.Enabled = true;
                        }
                    }
                    else
                    {
                        m_pMyCamera[i].Cam_Info.MV_CC_SetEnumValue_NET("TriggerSource", 0);
                    }
                    cbSoftTrigger.Enabled = true;
                    bnSaveBmp.Enabled = false;
                }
            }
        }

        private void SetCtrlWhenStartGrab()
        {
            bnStartGrab.Enabled = false;
            bnStopGrab.Enabled = true;
            bnClose.Enabled = false;
 
            //bnTriggerExec.Enabled = true;
            if (bnTriggerMode.Checked && cbSoftTrigger.Checked)
            {
                bnTriggerExec.Enabled = true;
            }

            bnSaveBmp.Enabled = true;
        }

        // ch:保存图片 | en:Save image
        private void SaveImage(IntPtr pData, MyCamera.MV_FRAME_OUT_INFO stFrameInfo,int nIndex)
        {
            string[] path = { "image1.bmp", "image2.bmp", "image3.bmp", "image4.bmp" };
            int nRet;

            if ((3 * stFrameInfo.nFrameLen + 2048) > m_pMyCamera[nIndex].m_nBufSizeForSaveImage)
            {
                m_pMyCamera[nIndex].m_nBufSizeForSaveImage = 3 * stFrameInfo.nFrameLen + 2048;
                m_pMyCamera[nIndex].m_pBufForSaveImage = new byte[m_pMyCamera[nIndex].m_nBufSizeForSaveImage];
            }

            IntPtr pImage = Marshal.UnsafeAddrOfPinnedArrayElement(m_pMyCamera[nIndex].m_pBufForSaveImage, 0);
            MyCamera.MV_SAVE_IMAGE_PARAM_EX stSaveParam = new MyCamera.MV_SAVE_IMAGE_PARAM_EX();
            stSaveParam.enImageType = MyCamera.MV_SAVE_IAMGE_TYPE.MV_Image_Bmp;
            stSaveParam.enPixelType = stFrameInfo.enPixelType;
            stSaveParam.pData = pData;
            stSaveParam.nDataLen = stFrameInfo.nFrameLen;
            stSaveParam.nHeight = stFrameInfo.nHeight;
            stSaveParam.nWidth = stFrameInfo.nWidth;
            stSaveParam.pImageBuffer = pImage;
            stSaveParam.nBufferSize = m_pMyCamera[nIndex].m_nBufSizeForSaveImage;
            stSaveParam.nJpgQuality = 80;
            nRet = m_pMyCamera[nIndex].Cam_Info.MV_CC_SaveImageEx_NET(ref stSaveParam);
            if (MyCamera.MV_OK != nRet)
            {
                string temp = "No.  + (nIndex + 1).ToString() +"+"Device save Failed!";
                ShowErrorMsg(temp,0);
            }
            else
            {
                FileStream file = new FileStream(path[nIndex], FileMode.Create, FileAccess.Write);
                file.Write(m_pMyCamera[nIndex].m_pBufForSaveImage, 0, (int)stSaveParam.nImageLen);
                file.Close();
                string temp = "No." + (nIndex + 1).ToString() + "Device Save Succeed!";
                ShowErrorMsg(temp,0);
            }
        }

        // ch:取流回调函数 | en:Aquisition Callback Function
        private void ImageCallBack1(IntPtr pData, ref MyCamera.MV_FRAME_OUT_INFO pFrameInfo, IntPtr pUser)
        {
           int nIndex=(int) pUser;

           // ch:抓取的帧数 | en:Aquired Frame Number
           ++m_nFrames[nIndex];

           //ch:判断是否需要保存图片 | en:Determine whether to save image
           if (m_bSaveImg[nIndex])
           {
               SaveImage(pData,pFrameInfo,nIndex);
               m_bSaveImg[nIndex] = false;
           }
       }

        private void bnStartGrab_Click(object sender, EventArgs e)
        {
            int nRet;
            m_hDisplayHandle[0] = pictureBox1.Handle;
            m_hDisplayHandle[1] = pictureBox2.Handle;
            m_hDisplayHandle[2] = pictureBox3.Handle;
            m_hDisplayHandle[3] = pictureBox4.Handle;

            // ch:开始采集 | en:Start Grabbing
            for (int i = 0; i < m_nCanOpenDeviceNum; i++)
            {
                    m_nFrames[i] = 0;
                    nRet = m_pMyCamera[i].Cam_Info.MV_CC_StartGrabbing_NET();
                    if (MyCamera.MV_OK != nRet)
                    {
                        string temp = "No. " + (i + 1).ToString() + " Device Save Failed!";
                        ShowErrorMsg(temp, nRet);
                    }
                    nRet = m_pMyCamera[i].Cam_Info.MV_CC_Display_NET(m_hDisplayHandle[i]);
                    if (MyCamera.MV_OK != nRet)
                    {
                        ShowErrorMsg("No 1 Device Display Failed!", nRet);
                    }
            }
            m_bTimerFlag = true;     //ch:开始计时  | en:Start Timing

            // ch:控件操作 | en:Control Operation
            SetCtrlWhenStartGrab();
            // ch:标志位置位true | en:Set Position Bit true
            m_bGrabbing = true;
        }

        private void cbSoftTrigger_CheckedChanged(object sender, EventArgs e)
        {
            if (cbSoftTrigger.Checked)
            {
                // ch:触发源设为软触发 | en:Set Trigger Source As Software
                for (int i = 0; i < m_nCanOpenDeviceNum; ++i)
                {
                    m_pMyCamera[i].Cam_Info.MV_CC_SetEnumValue_NET("TriggerSource", 7);
                }
                if (m_bGrabbing)
                {
                    bnTriggerExec.Enabled = true;
                }
            }
            else
            {
                bnTriggerExec.Enabled = false;
            }
        }

        // ch:触发命令 | en:Trigger Command
        private void bnTriggerExec_Click(object sender, EventArgs e)
        {
            int nRet;

            for (int i = 0; i < m_nCanOpenDeviceNum; ++i)
            {
                nRet = m_pMyCamera[i].Cam_Info.MV_CC_SetCommandValue_NET("TriggerSoftware");
                if (MyCamera.MV_OK != nRet)
                {
                    string temp = "No. " + (i + 1).ToString() + " Device Trigger Fail!";
                    ShowErrorMsg(temp, nRet);
                }
            }
        }

        private void SetCtrlWhenStopGrab()
        {
            bnStartGrab.Enabled = true;
            bnStopGrab.Enabled = false;
            bnClose.Enabled = true;

            bnTriggerExec.Enabled = false;
            bnSaveBmp.Enabled = false;
        }

        //停止采集 ch: | en:Stop Grabbing
        private void bnStopGrab_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < m_nCanOpenDeviceNum; ++i)
            {
                m_pMyCamera[i].Cam_Info.MV_CC_StopGrabbing_NET();
            }
            //ch:标志位设为false  | en:Set Flag Bit false
            m_bGrabbing = false;

            m_bTimerFlag = false;     // ch:停止计时 | en:Stop Timing

            // ch:控件操作 | en:Control Operation
            SetCtrlWhenStopGrab();
        }

        // ch:点击保存图片按钮 | en:Click on Save Image Button
        private void bnSaveBmp_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < m_nCanOpenDeviceNum; ++i)
            {
                m_bSaveImg[i] = true;  // ch:保存图片标志位,在回调函数中执行保存图片的操作 | en:Save Image Flag Bit, execute save image in the callback function
            }
        }

        // ch:设置曝光时间和增益 | en:Set Exposure Time and Gain
        private void bnSetParam_Click(object sender, EventArgs e)
        {
            try
            {
                float.Parse(tbExposure.Text);
                float.Parse(tbGain.Text);
            }
            catch
            {
                ShowErrorMsg("Please Enter Correct Type!", 0);
                return;
            }
            if (float.Parse(tbGain.Text) < 0 || float.Parse(tbGain.Text) < 0)
            {
                ShowErrorMsg("Set ExposureTime or Gain fail,Because ExposureTime or Gain less than zero", 0);
                return;
            }

            int nRet;
            for (int i = 0; i < m_nCanOpenDeviceNum; ++i)
            {
                bool bSuccess = true;
                m_pMyCamera[i].Cam_Info.MV_CC_SetEnumValue_NET("ExposureAuto", 0);

                nRet = m_pMyCamera[i].Cam_Info.MV_CC_SetFloatValue_NET("ExposureTime", float.Parse(tbExposure.Text));
                if (nRet != MyCamera.MV_OK)
                {
                    string temp = "No. " + (i + 1).ToString() + " Device Set Exposure Time Failed!";
                    ShowErrorMsg(temp,0);
                    bSuccess = false;
                }

                m_pMyCamera[i].Cam_Info.MV_CC_SetEnumValue_NET("GainAuto", 0);
                nRet = m_pMyCamera[i].Cam_Info.MV_CC_SetFloatValue_NET("Gain", float.Parse(tbGain.Text));
                if (nRet != MyCamera.MV_OK)
                {
                    string temp = "No. " + (i + 1).ToString() + " Device Set Gain Failed!";
                    ShowErrorMsg(temp,0);
                    bSuccess = false;
                }

                if (bSuccess)
                {
                    string temp = "No. " + (i + 1).ToString() + " Device Set Parameters Succeed!";
                    ShowErrorMsg(temp, nRet);
                }
            }
        }

        // ch:获取丢帧数 | en:Get Throw Frame Number
        private string GetLostFrame(int nIndex) 
        {
            MyCamera.MV_CC_DEVICE_INFO stDevInfo = new MyCamera.MV_CC_DEVICE_INFO();

            int nRet = m_pMyCamera[nIndex].Cam_Info.MV_CC_GetDeviceInfo_NET(ref stDevInfo);

            if (stDevInfo.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                MyCamera.MV_ALL_MATCH_INFO pstInfo = new MyCamera.MV_ALL_MATCH_INFO();
                _MV_MATCH_INFO_NET_DETECT_ MV_NetInfo = new _MV_MATCH_INFO_NET_DETECT_();
                pstInfo.nInfoSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(_MV_MATCH_INFO_NET_DETECT_));
                pstInfo.nType = 0x00000001;
                int size = Marshal.SizeOf(MV_NetInfo);
                pstInfo.pInfo = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(MV_NetInfo, pstInfo.pInfo, false);

                m_pMyCamera[nIndex].Cam_Info.MV_CC_GetAllMatchInfo_NET(ref pstInfo);
                MV_NetInfo = (_MV_MATCH_INFO_NET_DETECT_)Marshal.PtrToStructure(pstInfo.pInfo, typeof(_MV_MATCH_INFO_NET_DETECT_));

                string sTemp = MV_NetInfo.nLostFrameCount.ToString();
                Marshal.FreeHGlobal(pstInfo.pInfo);
                return sTemp;
            }
            else// ch:如果不是Gige设备，默认为U3V设备 | en:If not Gige device, default U3V device
            {
                MyCamera.MV_ALL_MATCH_INFO pstInfo = new MyCamera.MV_ALL_MATCH_INFO();
                MyCamera.MV_MATCH_INFO_USB_DETECT MV_NetInfo = new MyCamera.MV_MATCH_INFO_USB_DETECT();
                pstInfo.nInfoSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(MyCamera.MV_MATCH_INFO_USB_DETECT));
                pstInfo.nType = 0x00000004;
                int size = Marshal.SizeOf(MV_NetInfo);
                pstInfo.pInfo = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(MV_NetInfo, pstInfo.pInfo, false);

                m_pMyCamera[nIndex].Cam_Info.MV_CC_GetAllMatchInfo_NET(ref pstInfo);
                MV_NetInfo = (MyCamera.MV_MATCH_INFO_USB_DETECT)Marshal.PtrToStructure(pstInfo.pInfo, typeof(MyCamera.MV_MATCH_INFO_USB_DETECT));

                string sTemp = MV_NetInfo.nErrorFrameCount.ToString();
                Marshal.FreeHGlobal(pstInfo.pInfo);
                return sTemp;
            }
            return "0";
        }

        // ch:定时器,1秒运行一次 | en:Timer, run once a second
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (m_bTimerFlag)
            {
                if (m_nCanOpenDeviceNum > 0)
                {
                    tbGrabFrame1.Text = m_nFrames[0].ToString();
                    tbLostFrame1.Text = GetLostFrame(0);
                }
                if (m_nCanOpenDeviceNum > 1)
                {
                    tbGrabFrame2.Text = m_nFrames[1].ToString();
                    tbLostFrame2.Text = GetLostFrame(1);
                }
                if (m_nCanOpenDeviceNum > 2)
                {
                    tbGrabFrame3.Text = m_nFrames[2].ToString();
                    tbLostFrame3.Text = GetLostFrame(2);
                }
                if (m_nCanOpenDeviceNum > 3)
                {
                    tbGrabFrame4.Text = m_nFrames[3].ToString();
                    tbLostFrame4.Text = GetLostFrame(3);
                }
            }
        }
    }
}
