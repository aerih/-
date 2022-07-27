#ifndef MYCHART_H
#define MYCHART_H

#include <QChartView>

class MyChart : public QChartView
{
    Q_OBJECT
public:
    explicit MyChart(QObject *parent = nullptr);

signals:

};

#endif // MYCHART_H
