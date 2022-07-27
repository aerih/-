#include "workthread.h"

WorkThread::WorkThread(QObject *parent) : QThread(parent)
{
}

WorkThread::WorkThread(HKCamera* camera,QObject *parent) : WorkThread(parent){
    this->camera = camera;
}

void WorkThread::run() {
    if(startWork() == false) {
        return;
        qDebug() << "start work error";
    }
    while (Collecting) {
        QImage img = camera->getFrame();
        if (Collecting && img != QImage()){
            if (count < 100){
                ++count;
                sum+=Model(img).gray;
                emit colectFirst100CountSignal(count);
            } else if (count == 100){
                ++count;
                avr = sum/100;
                emit First100AvrageSingal(avr);
            } else{
                emit frameReadySignal(Model(img));
            }
           QThread::usleep(500);
        }

    }
}
//进行抓取图片前的一些工作
bool WorkThread::startWork() {
    this->count = 0;
    if (camera == nullptr || camera->isOpen == false)
        return false;
    if (camera->startGrabbing() == false)
        return false;
    this->Collecting = true;
    return true;


}

bool WorkThread::stopWork() {

    if (this->Collecting == false)
        return true;
    this->Collecting = false;
    if (camera->stopGrabbing() == false)
        return false;
    return true;
}

bool WorkThread::isCollecting(){
    return this->Collecting;
}
