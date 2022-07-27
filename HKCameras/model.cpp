#include "model.h"

Model::Model(QImage img) : pixmap(QPixmap::fromImage(img,Qt::ColorOnly))
{
    gray = 0;
    for (int i = img.width()/2-50; i < img.width()/2+50; ++i) { //将取中间一百列像素点
        for (int j = 0; j < img.height(); ++j){
            gray += qGray(img.pixel(i,j));
         }
    }

    //qDebug() << this->gray << "    " << img.width() << "   " << img.height() << img.pixelColor(256,512).red() << img.pixelColor(256,512).green() << img.pixelColor(256,512).blue();
}
