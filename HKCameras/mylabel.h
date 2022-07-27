#ifndef MYLABEL_H
#define MYLABEL_H
#include <QLabel>
#include <QWidget>

class MyLabel : public QLabel
{
    Q_OBJECT
public:
    explicit MyLabel(QWidget *parent = nullptr);

protected:

    void mousePressEvent(QMouseEvent *ev) override;  //重写标签的点击事件
signals:
    void labelClickSignal();  //标签被点击时发送该信号
};

#endif // MYLABEL_H
