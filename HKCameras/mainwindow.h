#ifndef MAINWINDOW_H
#define MAINWINDOW_H

#include <QMetaType>
#include <QMainWindow>
#include <QTabWidget>
#include <QtCharts>
#include <QDebug>
#include <QMessageBox>
#include "hkcamera.h"
#include <QTimer>
#include "workthread.h"
#include <QEvent>

using namespace QtCharts;

QT_BEGIN_NAMESPACE
namespace Ui { class MainWindow; }
QT_END_NAMESPACE

class MainWindow : public QMainWindow
{
    Q_OBJECT

public:
    MainWindow(QWidget *parent = nullptr);
    ~MainWindow();

private slots:
    void on_btnSereachCam_released();
//






    void on_btnAllRun_released();

    void on_btnAllStop_released();

    void on_start_1_released();



    void on_stop_1_released();

    void on_moveLeft_1_released();

    void on_moveRight_1_released();


    void on_width_1_currentTextChanged(const QString &arg1);



    void on_width_3_currentTextChanged(const QString &arg1);

    void on_width_2_currentTextChanged(const QString &arg1);

    void on_width_4_currentTextChanged(const QString &arg1);

    void on_width_5_currentTextChanged(const QString &arg1);

    void on_width_6_currentTextChanged(const QString &arg1);

    void on_width_7_currentTextChanged(const QString &arg1);

    void on_width_8_currentTextChanged(const QString &arg1);
    void on_start_2_released();

    void on_stop_2_released();

    void on_moveLeft_2_released();

    void on_moveRight_2_released();

    void on_save_1_released();

    void on_save_2_released();

    void on_btnAllRun_2_released();

protected:
    void closeEvent(QCloseEvent *event) override;
private:
    Ui::MainWindow *ui;
    QLineSeries* series[8] = {nullptr};
    qreal x[8] = {0};
    qreal grayAvrOfFirst100[8] = {1};
    void createChart();
    HKCamera::DeviceList deviceList;

    QValueAxis* axisY[8] = {nullptr};

    //创建图表
    HKCamera* cameras[8] = {nullptr};  //相机列表
    QChart* charts[8];
    QChartView* chartViews(unsigned index);
    QLabel* icons(unsigned index);
    QLabel* pics(unsigned index);
    QProgressBar* progressBar(unsigned index);
    WorkThread* workThread[8] = {nullptr};  //图像搜集线程
    QComboBox* comboBox(unsigned index);



    void startUpSet(int index);
    void connectIcons();
    void connectChartViews();



    void commanSave(int index,QString path);
    void commanMoveLeft(int index);
    void commanMoveRight(int index);
    void commanStart(int index);
    void commanStop(int index);
    void commanSetViewWidth(int index,const QString &arg);

    void dataHandler(Model model,int threadIndex); //处理第i个线程发来的数据

};
#endif // MAINWINDOW_H
