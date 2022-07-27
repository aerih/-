#include "mychartview.h"

MyChartView::MyChartView(QWidget *parent) : QChartView(parent)
{

}

void MyChartView::wheelEvent(QWheelEvent *event) {
    if (event->delta() > 0) {

        emit zoomInSignal();
    } else {
        emit zoomoutSinal();
    }
    event->accept();
}
