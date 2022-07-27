#ifndef MODEL_H
#define MODEL_H
#include <QImage>
#include <QPixmap>
#include <QDebug>
/*
该类用以定义工作子线程向主线程传输的数据结构，包括采集到的图片以及图形的数据
*/

class Model
{
public:
    Model(QImage img = QImage());
    QPixmap pixmap;//传送至主线程的图像
    qreal gray;  //储存图像的灰度
};

#endif // MODEL_H
