#include "hkcamera.h"
#include <stdint.h>

HKCamera::HKCamera() {

}

bool HKCamera::openDevice(DeviceInfo* devInfo) {
    int ret = MV_CC_CreateHandle(&handle, devInfo);
    if (MV_OK != ret) {
        return false;
    }
    ret = MV_CC_OpenDevice(handle);
    if (MV_OK != ret){
        return false;
    }


    int size =  MV_CC_GetOptimalPacketSize(handle);
    if (size > 0)  {
        if (MV_CC_SetIntValue(handle,"GevSCPSPacketSize",size) != MV_OK) {
            qDebug () << "Oh fuck";
        }

    } else {
        qDebug() << "size smaller than 0";
    }
    return true;
}

bool HKCamera::closeDevice() {
    if (handle == nullptr) {
        return true;
    }
    int ret = MV_CC_CloseDevice(handle);
    if (MV_OK != ret){
        return false;
    }
    ret = MV_CC_DestroyHandle(handle);
    if (MV_OK != ret){
        return false;
    }
    handle = nullptr;
    return true;
}

HKCamera::~HKCamera() {
    if (handle != nullptr) {
        MV_CC_DestroyHandle(handle);
        handle = nullptr;
    }
}

MV_CC_DEVICE_INFO_LIST HKCamera::getDeviceList() {
    MV_CC_DEVICE_INFO_LIST temp;
    memset(&temp,0,sizeof (temp));
    int ret = MV_CC_EnumDevices(MV_GIGE_DEVICE, &temp);
    if (MV_OK != ret) {
        qDebug() << "Enum Devices fail!";
    }
    return temp;
}
QString HKCamera::devInfo2String(MV_CC_DEVICE_INFO* devInfo){
    if (nullptr == devInfo) {
        return "The Pointer of pstMVDevInfo is NULL!";
    }
    if (MV_GIGE_DEVICE == devInfo->nTLayerType){
        QString name = QString::fromUtf8(reinterpret_cast<char *>(devInfo->SpecialInfo.stGigEInfo.chModelName),32);
        int nIp1 = ((devInfo->SpecialInfo.stGigEInfo.nCurrentIp & 0xff000000) >> 24);
        int nIp2 = ((devInfo->SpecialInfo.stGigEInfo.nCurrentIp & 0x00ff0000) >> 16);
        int nIp3 = ((devInfo->SpecialInfo.stGigEInfo.nCurrentIp & 0x0000ff00) >> 8);
        int nIp4 = (devInfo->SpecialInfo.stGigEInfo.nCurrentIp & 0x000000ff);
        QString temp = QString("DEV: %1 IP: %2.%3.%4.%5").arg(name).arg(nIp1).arg(nIp2).arg(nIp3).arg(nIp4);
        return temp;
    }
    return "Not Supported";
}

bool HKCamera::startGrabbing() {
    if (handle == nullptr) {
        return false;
    }
    int packageSize = MV_CC_GetOptimalPacketSize(handle);
    int ret = MV_CC_SetIntValue(handle,"GevSCPSPacketSize",packageSize);  //设置最佳包大小
    if (ret != MV_OK){
        return false;
    }
    ret = MV_CC_StartGrabbing(handle);
    if (MV_OK != ret){
        return false;
    }
    this->grabbing = true;
    return true;
}

bool HKCamera::stopGrabbing() {
    if (handle == nullptr) {
        return true;
    }
    int ret = MV_CC_StopGrabbing(handle);
    if (MV_OK != ret) {
        return false;
    }
    this->grabbing = false;
    return true;
}

QImage HKCamera::getFrame() {
    if (handle == nullptr){
        return QImage();
    }

    memset(&outFrame, 0, sizeof(MV_FRAME_OUT));
    int ret = MV_CC_GetImageBuffer(handle, &outFrame,1000);
    if (MV_OK != ret) {
        MV_CC_FreeImageBuffer(handle, &outFrame);
        qDebug() << "Failed Accquire" << ret;
        return QImage();
    }
    //QImage temp(512,1024,QImage::Format_RGB888);
    QImage temp(outFrame.stFrameInfo.nWidth, outFrame.stFrameInfo.nHeight, QImage::Format_RGB888);
    memcpy(temp.bits(), outFrame.pBufAddr,outFrame.stFrameInfo.nFrameLen);
    MV_CC_FreeImageBuffer(handle, &outFrame);
    return temp;
}

bool HKCamera::moveViewLeft() {
    if (handle == nullptr)
        return false;
    MVCC_INTVALUE_EX preVal;
    int ret = MV_CC_GetIntValueEx(handle,"OffsetX",&preVal);
    if (ret != MV_OK) {
        return false;
    }
    if (preVal.nCurValue-16 < preVal.nMin) {
        return false;
    }
    ret = MV_CC_SetIntValueEx(handle,"OffsetX",preVal.nCurValue-16);
    if (ret != MV_OK)
        return false;
    return true;
}

bool HKCamera::moveViewRight() {
    if (handle == nullptr)
        return false;
    MVCC_INTVALUE_EX preVal;
    int ret = MV_CC_GetIntValueEx(handle,"OffsetX",&preVal);
    if (ret != MV_OK) {
        return false;
    }
    if (preVal.nCurValue+16 > preVal.nMax) {
        return false;
    }
    ret = MV_CC_SetIntValueEx(handle,"OffsetX",preVal.nCurValue+16);
    if (ret != MV_OK)
        return false;
    return true;
}

bool HKCamera::incViewWidth() {
    if (handle == nullptr)
        return false;
    MVCC_INTVALUE_EX preVal;
    int ret = MV_CC_GetIntValueEx(handle,"Width",&preVal);
    if (ret != MV_OK) {
        return false;
    }
    if (preVal.nCurValue+16 > preVal.nMax)
        return false;
    ret = MV_CC_SetIntValueEx(handle,"Width",preVal.nCurValue*2);
    if (ret != MV_OK)
        return false;
    return true;
}

bool HKCamera::decViewWidth() {

    if (handle == nullptr)
        return false;
    MVCC_INTVALUE_EX preVal;
    int ret = MV_CC_GetIntValueEx(handle,"Width",&preVal);
    qDebug() << preVal.nCurValue;
    if (ret != MV_OK) {
        return false;
    }
    if (preVal.nCurValue-16 < preVal.nMin)
        return false;
    ret = MV_CC_SetIntValueEx(handle,"Width",preVal.nCurValue-16);
    if (ret != MV_OK)
        return false;
    return true;
}

bool HKCamera::setPixeType(MvGvspPixelType type) {
    if (handle == nullptr)
        return true;
    int ret = MV_CC_SetPixelFormat(handle,type);
    if (ret != MV_OK){
        qDebug() << ret;
        return false;
    }
    MVCC_ENUMVALUE temp;
    MV_CC_GetEnumValue(handle,"PixelFormat",&temp);
    return true;
}

bool HKCamera::setViewWidth(unsigned width) {
    if (handle == nullptr)
        return false;
    int ret = MV_CC_SetWidth(handle,width);
    if (ret != MV_OK) {
        return false;
    }
    ret = MV_CC_SetIntValueEx(handle,"OffsetX",1536-width/2);
    if (ret != MV_OK){
        return false;
    }
    return true;
}

bool HKCamera::setViewHeight(unsigned height) {
    if (handle == nullptr)
        return  false;
    int ret = MV_CC_SetHeight(handle,height);
    if (ret != MV_OK){
        qDebug() << "set height error";
        return false;
    }
    ret = MV_CC_SetIntValueEx(handle,"OffsetY",(2048-height)/2);
   // ret = MV_CC_setO(handle,(2048-height)/2);
    if (ret != MV_OK) {
        qDebug() << "set offset error";
        return false;
    }
    return true;
}

bool HKCamera::setFrameRate(unsigned fps) {
    int ret = MV_CC_SetFrameRate(handle,fps);
    if (ret != MV_OK) {
        return false;
    }
    return true;
}

unsigned HKCamera::getOffsetX() {
    if (handle == nullptr)
        return 0;
    MVCC_INTVALUE_EX preVal;
    int ret = MV_CC_GetIntValueEx(handle,"OffsetX",&preVal);
    if (ret != MV_OK) {
        return 0;
    }
    return preVal.nCurValue;
}

bool HKCamera::setOffsetX(unsigned int offset) {
    if (handle == nullptr)
        return false;
    int ret = MV_CC_SetIntValueEx(handle,"OffsetX",offset);
    if (ret != MV_OK) {
        return false;
    }
    return true;
}

unsigned HKCamera::getViewWidth() {
    MVCC_INTVALUE preVal;
    MV_CC_GetWidth(handle,&preVal);
    return preVal.nCurValue;
}
