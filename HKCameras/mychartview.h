#ifndef MYCHARTVIEW_H
#define MYCHARTVIEW_H

#include <QWidget>
#include <QChartView>
#include <QChart>
#include <QEvent>
#include <QWheelEvent>
#include <QValueAxis>

class MyChartView : public QtCharts::QChartView
{
    Q_OBJECT
public:
    explicit MyChartView(QWidget *parent = nullptr);
protected:
      void wheelEvent(QWheelEvent *event) override;

signals:
    void zoomInSignal(void);  //缩放 放大信号
    void zoomoutSinal(void);  //缩放，缩小信号
};

#endif // MYCHARTVIEW_H
