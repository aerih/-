#ifndef HKCAMERA_H
#define HKCAMERA_H
#include "MvCameraControl.h"
#include "string.h"
#include <QtDebug>
#include <QImage>
#include <QBitmap>

class HKCamera
{
public:
    using DeviceList = MV_CC_DEVICE_INFO_LIST;
    using DeviceInfo = MV_CC_DEVICE_INFO;
    HKCamera();
    ~HKCamera();
    bool openDevice(MV_CC_DEVICE_INFO* devInfo);
    bool closeDevice();
    bool startGrabbing();
    bool stopGrabbing();
    static DeviceList getDeviceList();
    static QString devInfo2String(DeviceInfo* pstMVDevInfo);

    bool moveViewRight();
    bool moveViewLeft();
    bool isOpen = false;
    bool incViewWidth();
    bool decViewWidth();
    unsigned getViewWidth();
    bool setViewWidth(unsigned width);
    bool setViewHeight(unsigned Height);
    bool setFrameRate(unsigned fps);
    bool setPixeType(MvGvspPixelType type);
    bool isGrabbing(){return grabbing;}
    QImage getFrame();
    unsigned getOffsetX();
    bool setOffsetX(unsigned offset);
    //QImage copyToImage();
private:
    MV_FRAME_OUT outFrame;
    void* handle = nullptr;
    bool grabbing = false;
};

#endif // HKCAMERA_H
