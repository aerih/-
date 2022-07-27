
#include "CameraParams.h"

#pragma once

#ifdef __cplusplus
extern "C" 
{
#endif

    DEFINE_GUID(CLSID_MvDSSource,0x8e14549a, 0xdb61, 0x4309, 0xaf, 0xa1, 0x35, 0x78, 0xe9, 0x27, 0xe9, 0x33);
    DEFINE_GUID(IID_IMvDSSource, 0x19412d6e, 0x6401,0x475c, 0xb0, 0x48, 0x7a, 0xd2, 0x96, 0xe1, 0x6a, 0x19);

    enum MvOutputPixelType
    {
        PixelType_Undefined = -1,
        PixelType_RGB24 = 0,
        PixelType_BGR24,
        PixelType_RGBA8,
        PixelType_BGRA8,
        PixelType_YUYV,
        PixelType_UYVY,
    };

    DECLARE_INTERFACE_(MvDSSource, IUnknown)
    {
        //获取设备列表
        STDMETHOD (GetDeviceList)(MV_CC_DEVICE_INFO_LIST *pDeviceList) PURE;
        //获取当前相机索引
        STDMETHOD (GetCurIndex)(int* pCurIndex) PURE;
        //设置当前相机索引
        STDMETHOD (SetIndex)(unsigned int nIndex) PURE;

        //获取参数
        STDMETHOD (GetIntValue)(IN const char* strKey,OUT MVCC_INTVALUE *pIntValue) PURE;
        STDMETHOD (GetEnumValue)(IN const char* strKey,OUT MVCC_ENUMVALUE *pEnumValue) PURE;
        STDMETHOD (GetFloatValue)(IN const char* strKey,OUT MVCC_FLOATVALUE *pFloatValue) PURE;
        STDMETHOD (GetBoolValue)(IN const char* strKey,OUT bool *pBoolValue) PURE;
        STDMETHOD (GetStringValue)(IN const char* strKey,OUT MVCC_STRINGVALUE *pStringValue) PURE;

        //设置参数
        STDMETHOD (SetIntValue)(IN const char* strKey,IN unsigned int nValue) PURE;
        STDMETHOD (SetEnumValue)(IN const char* strKey,IN unsigned int nValue) PURE;
        STDMETHOD (SetFloatValue)(IN const char* strKey,IN float fValue) PURE;
        STDMETHOD (SetBoolValue)(IN const char* strKey,IN bool bValue) PURE;
        STDMETHOD (SetStringValue)(IN const char* strKey,IN const char * sValue) PURE;
        STDMETHOD (SetCommandValue)(IN const char* strKey) PURE;

        //输出格式获取和设置
        STDMETHOD (GetOutputFormat)(OUT MvOutputPixelType *pOutputPixelType) PURE;
        STDMETHOD (SetOutputFormat)(IN MvOutputPixelType OutputPixelType) PURE;
    };

#ifdef __cplusplus
}
#endif
