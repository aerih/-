#ifndef WORKTHREAD_H
#define WORKTHREAD_H

#include "model.h"
#include <QThread>
#include "hkcamera.h"

class WorkThread : public QThread
{
    Q_OBJECT
public:
    explicit WorkThread(QObject *parent = nullptr);
    WorkThread(HKCamera* camera,QObject* parent = nullptr);
    bool isCollecting();
    bool stopWork();
    HKCamera* camera;
protected:
    void run() override;
private:

    bool Collecting = false;
    bool startWork();

    int count = 0;
    qreal avr = 0;
    qreal sum = 0;

signals:
    void frameReadySignal(Model pic);   //统计时，帧可用信号
    void colectFirst100CountSignal(int count);//前一百张收集进度
    void First100AvrageSingal(qreal avr);  //前一百张的平均值
};

#endif // WORKTHREAD_H
