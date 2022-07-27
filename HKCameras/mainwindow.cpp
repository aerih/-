#include "mainwindow.h"
#include "ui_mainwindow.h"

MainWindow::MainWindow(QWidget *parent)
    : QMainWindow(parent)
    , ui(new Ui::MainWindow)
{
    ui->setupUi(this);

    ui->progressBar_1->setVisible(false);
    ui->progressBar_2->setVisible(false); //123456


    createChart();
    connectIcons();
    qRegisterMetaType<Model>("Model"); //注册自定义类型Model
    connectChartViews();  //连接表的滚轮事件
}

MainWindow::~MainWindow()
{
    delete ui;
    for (int i = 0; i< 8; ++i) {
        if (cameras[i] != nullptr){
            delete cameras[i];
            cameras[i] = nullptr;
        }
        if (charts[i] != nullptr) {
            delete charts[i];
            charts[i] = nullptr;
        }
    }
}

void MainWindow::connectChartViews() {
    connect(ui->chartView_1,&MyChartView::zoomInSignal,charts[0],[=]{
        axisY[0]->setRange(axisY[0]->min()*0.8,axisY[0]->max()*0.8);
    });

    connect(ui->chartView_1,&MyChartView::zoomoutSinal,charts[0],[=]{
        axisY[0]->setRange(axisY[0]->min()*1.25,axisY[0]->max()*1.25);
    });

    connect(ui->chartView_2,&MyChartView::zoomInSignal,charts[1],[=]{
        axisY[1]->setRange(axisY[1]->min()*0.8,axisY[1]->max()*0.8);
    });

    connect(ui->chartView_2,&MyChartView::zoomoutSinal,charts[1],[=]{
        axisY[1]->setRange(axisY[1]->min()*1.25,axisY[1]->max()*1.25);
    });

    connect(ui->chartView_3,&MyChartView::zoomInSignal,charts[2],[=]{
        axisY[2]->setRange(axisY[2]->min()*0.8,axisY[2]->max()*0.8);
    });

    connect(ui->chartView_3,&MyChartView::zoomoutSinal,charts[2],[=]{
        axisY[2]->setRange(axisY[2]->min()*1.25,axisY[2]->max()*1.25);
    });

    connect(ui->chartView_4,&MyChartView::zoomInSignal,charts[3],[=]{
        axisY[3]->setRange(axisY[3]->min()*0.8,axisY[3]->max()*0.8);
    });

    connect(ui->chartView_4,&MyChartView::zoomoutSinal,charts[3],[=]{
        axisY[3]->setRange(axisY[3]->min()*1.25,axisY[3]->max()*1.25);
    });

    connect(ui->chartView_5,&MyChartView::zoomInSignal,charts[4],[=]{
        axisY[4]->setRange(axisY[4]->min()*0.8,axisY[4]->max()*0.8);
    });

    connect(ui->chartView_5,&MyChartView::zoomoutSinal,charts[4],[=]{
        axisY[4]->setRange(axisY[4]->min()*1.25,axisY[4]->max()*1.25);
    });

    connect(ui->chartView_6,&MyChartView::zoomInSignal,charts[5],[=]{
        axisY[5]->setRange(axisY[5]->min()*0.8,axisY[5]->max()*0.8);
    });

    connect(ui->chartView_6,&MyChartView::zoomoutSinal,charts[5],[=]{
        axisY[5]->setRange(axisY[5]->min()*1.25,axisY[5]->max()*1.25);
    });

    connect(ui->chartView_7,&MyChartView::zoomInSignal,charts[6],[=]{
        axisY[6]->setRange(axisY[6]->min()*0.8,axisY[6]->max()*0.8);
    });

    connect(ui->chartView_7,&MyChartView::zoomoutSinal,charts[6],[=]{
        axisY[6]->setRange(axisY[6]->min()*1.25,axisY[6]->max()*1.25);
    });

    connect(ui->chartView_8,&MyChartView::zoomInSignal,charts[7],[=]{
        axisY[7]->setRange(axisY[7]->min()*0.8,axisY[7]->max()*0.8);
    });

    connect(ui->chartView_8,&MyChartView::zoomoutSinal,charts[7],[=]{
        axisY[7]->setRange(axisY[7]->min()*1.25,axisY[7]->max()*1.25);
    });
}

//为八个标签页创建默认图表
void MainWindow::createChart() {

  //  QChartView* chartViews[] = {ui->chartView_1,ui->chartView_2,ui->chartView_3,ui->chartView_4,ui->chartView_5,ui->chartView_6,ui->chartView_7,ui->chartView_8};
    for (int i = 0; i < 8; ++i){
        charts[i] = new QChart;
        auto axisX = new QValueAxis(this);
        axisY[i] = new QValueAxis(this);
        axisX->setRange(0,1);
        axisY[i]->setRange(-5,5);
        axisX->setTitleText("step");
        axisY[i]->setTitleText("Proporotion / %");
        axisY[i]->setTickCount(11); //设置坐标轴上的格数(实际上是线数，故加一)

        series[i] = new QLineSeries(this);
        QPen pen;
        pen.setWidth(1);
        pen.setColor(Qt::blue);
        series[i]->setPen(pen);
        charts[i]->addSeries(series[i]);

        charts[i]->addAxis(axisX,Qt::AlignBottom);
        charts[i]->addAxis(axisY[i],Qt::AlignLeft);

        series[i]->attachAxis(axisX);
        series[i]->attachAxis(axisY[i]);
        chartViews(i)->setChart(charts[i]);
    }
}

//查找并绑定相机
void MainWindow::on_btnSereachCam_released()
{

    deviceList = HKCamera::getDeviceList();
    if (deviceList.nDeviceNum <= 0) {
        QMessageBox::warning(this,"Warning","No Device");
        return;
    }
    for (unsigned i = 0; i < 8; ++i) { //关闭所有相机
        if (cameras[i] != nullptr) {
            if (cameras[i]->isOpen == true)
                cameras[i]->closeDevice();
            delete cameras[i];
            cameras[i] = nullptr;
        }
    }
    for (unsigned i = 0; i < deviceList.nDeviceNum; ++i) {
        cameras[i] = new HKCamera;
        if (cameras[i]->openDevice(deviceList.pDeviceInfo[i])!= true) {
            QMessageBox::warning(this,"Warining","Open Camera "+HKCamera::devInfo2String(deviceList.pDeviceInfo[i])+" Failed");
            delete cameras[i];
            cameras[i] = nullptr;
        }
        cameras[i]->isOpen = true;
        startUpSet(i);
    }
}

void MainWindow::startUpSet(int i){
    cameras[i]->setPixeType(PixelType_Gvsp_RGB8_Packed);
     //if (cameras[i]->setViewWidth(3072) != true) {
    if (cameras[i]->setViewWidth(comboBox(i)->currentText().toInt()) != true) {
        qDebug() << "ERROR Seting View Width";
    }
    if (cameras[i]->setViewHeight(1024) != true) {
        qDebug() << "ERROR Seting View Height";
    }//
    cameras[i]->setFrameRate(10);
    icons(i)->setPixmap(QPixmap("://res//ready.png"));
    pics(i)->setPixmap(QPixmap("://res//readyBig.png"));

}

QProgressBar* MainWindow::progressBar(unsigned index) {
    QProgressBar* temp[] = {ui->progressBar_1,ui->progressBar_2,nullptr,nullptr,nullptr,nullptr,nullptr,nullptr}; // 123456
    return temp[index];
}

QChartView* MainWindow::chartViews(unsigned index){
    QChartView* temp[] = {ui->chartView_1,ui->chartView_2,ui->chartView_3,ui->chartView_4,ui->chartView_5,ui->chartView_6,ui->chartView_7,ui->chartView_8};
    return temp[index];
}

QLabel* MainWindow::icons(unsigned int index) {
    QLabel* temp[] = {ui->label_1,ui->label_2,ui->label_3,ui->label_4,ui->label_5,ui->label_6,ui->label_7,ui->label_8};
    return temp[index];
}

QLabel*MainWindow::pics(unsigned i) {
    QLabel* temp[] = {ui->pic,ui->pic_2,ui->pic_3,ui->pic_4,ui->pic_5,ui->pic_6,ui->pic_7,ui->pic_8};
    return temp[i];
}

QComboBox* MainWindow::comboBox(unsigned int index) {
    QComboBox* temp[] = {ui->width_1,ui->width_2,ui->width_3,ui->width_4,ui->width_5,ui->width_6,ui->width_7,ui->width_8};
    return temp[index];
}

void MainWindow::connectIcons(){
    connect(ui->label_1,&MyLabel::labelClickSignal,this,[=]{
        ui->tabWidget->setCurrentWidget(ui->tab_1);
    });

    connect(ui->label_2,&MyLabel::labelClickSignal,this,[=]{
        ui->tabWidget->setCurrentWidget(ui->tab_2);
    });

    connect(ui->label_3,&MyLabel::labelClickSignal,this,[=]{
        ui->tabWidget->setCurrentWidget(ui->tab_3);
    });

    connect(ui->label_4,&MyLabel::labelClickSignal,this,[=]{
        ui->tabWidget->setCurrentWidget(ui->tab_4);
    });

    connect(ui->label_5,&MyLabel::labelClickSignal,this,[=]{
        ui->tabWidget->setCurrentWidget(ui->tab_5);
    });

    connect(ui->label_6,&MyLabel::labelClickSignal,this,[=]{
        ui->tabWidget->setCurrentWidget(ui->tab_6);
    });

    connect(ui->label_7,&MyLabel::labelClickSignal,this,[=]{
        ui->tabWidget->setCurrentWidget(ui->tab_7);
    });

    connect(ui->label_8,&MyLabel::labelClickSignal,this,[=]{
        ui->tabWidget->setCurrentWidget(ui->tab_8);
    });
}


void MainWindow::on_btnAllRun_released()
{
    for (int i = 0; i < 8; ++i)
        commanStart(i);
}

void MainWindow::on_btnAllStop_released()
{
    for (int i = 0; i < 8; ++i)
        commanStop(i);
}

void MainWindow::on_start_1_released()
{
    commanStart(0);
}

void MainWindow::on_stop_1_released()
{
    commanStop(0);
}

void MainWindow::on_moveLeft_1_released()
{
    commanMoveLeft(0);
}

void MainWindow::on_moveRight_1_released()
{
    commanMoveRight(0);
}

void MainWindow::commanStop(int i) {
    if (cameras[i] == nullptr)
        return;
    pics(i)->setPixmap(QPixmap("://res//readyBig.png"));  //需要修改
    this->icons(i)->setPixmap(QPixmap("://res//ready.png"));
    comboBox(i)->setEnabled(true);
    progressBar(i)->setVisible(false);

    if (workThread[i] != nullptr){
        disconnect(workThread[i],nullptr,nullptr,nullptr);
        workThread[i]->stopWork();
        while (workThread[i]->isFinished() == false) ;
        delete workThread[i];
        workThread[i] = nullptr;
    }
}

void MainWindow::commanStart(int i) {
    if (cameras[i] == nullptr)
        return;
    if (workThread[i] == nullptr)
        workThread[i] = new WorkThread(cameras[i],this);
    if (workThread[i]->camera != cameras[i])
        workThread[i]->camera = cameras[i];
    if (workThread[i]->isCollecting() == true)
        return; 

    progressBar(i)->setVisible(true);
    progressBar(i)->reset();

    connect(workThread[i],&WorkThread::colectFirst100CountSignal,this,[=](int count) {
        progressBar(i)->setValue(count);
    });

    connect(workThread[i],&WorkThread::First100AvrageSingal,this,[=](qreal avr) {
       grayAvrOfFirst100[i] =  avr;
       progressBar(i)->setVisible(false);
    });

    connect(workThread[i],&WorkThread::frameReadySignal,this,[=](Model model){
        dataHandler(model,i);
    });

    series[i]->clear();
    x[i] = 0;
    workThread[i]->start();
    comboBox(i)->setEnabled(false);
}

void MainWindow::commanMoveLeft(int i) {
    if (cameras[i] == nullptr || cameras[i]->isGrabbing() == false)
        return;
    cameras[i]->moveViewLeft();
}

void MainWindow::commanMoveRight(int i) {
    if (cameras[i] == nullptr || cameras[i]->isGrabbing() == false)
        return;
    cameras[i]->moveViewRight();
}

void MainWindow::commanSetViewWidth(int i, const QString &arg){
    if (cameras[i] == nullptr || cameras[i]->isGrabbing() == true)
        return;
    if (cameras[i]->setViewWidth(arg.toInt()) != true) {
        QMessageBox::warning(this,"Warnning","Set View Width Failed, Try Other Value");
    }
}

void MainWindow::dataHandler(Model model, int i){
    pics(i)->setPixmap(model.pixmap.scaled(300,600,Qt::KeepAspectRatio));
    icons(i)->setPixmap(model.pixmap);
    qreal rate = 100*(model.gray-this->grayAvrOfFirst100[i])/grayAvrOfFirst100[i];
    series[i]->append(x[i],rate);
    charts[i]->axes(Qt::Horizontal).first()->setMax(x[i]++);
}

void MainWindow::on_width_1_currentTextChanged(const QString &arg1)
{
    commanSetViewWidth(0,arg1);
}

void MainWindow::on_width_3_currentTextChanged(const QString &arg1)
{
    commanSetViewWidth(1,arg1);
}

void MainWindow::on_width_2_currentTextChanged(const QString &arg1)
{
    commanSetViewWidth(2,arg1);
}

void MainWindow::on_width_4_currentTextChanged(const QString &arg1)
{
    commanSetViewWidth(3,arg1);
}

void MainWindow::on_width_5_currentTextChanged(const QString &arg1)
{
    commanSetViewWidth(4,arg1);
}

void MainWindow::on_width_6_currentTextChanged(const QString &arg1)
{
    commanSetViewWidth(5,arg1);
}

void MainWindow::on_width_7_currentTextChanged(const QString &arg1)
{
    commanSetViewWidth(6,arg1);
}

void MainWindow::on_width_8_currentTextChanged(const QString &arg1)
{
    commanSetViewWidth(7,arg1);
}

void MainWindow::on_start_2_released()
{
    commanStart(1);
}

void MainWindow::on_stop_2_released()
{
     commanStop(1);
}

void MainWindow::on_moveLeft_2_released()
{
    commanMoveLeft(1);
}

void MainWindow::on_moveRight_2_released()
{
    commanMoveRight(1);
}

void MainWindow::closeEvent(QCloseEvent *e){
    int ret = QMessageBox::question(this,"Quit","Sure to Quit?");
    if (ret == QMessageBox::Yes){
        on_btnAllStop_released();
        e->accept();
    } else {
        e->ignore();
    }
}

void MainWindow::on_save_1_released()
{
    QString path = QFileDialog::getExistingDirectory(this,"保存至");
    if (path.isEmpty() == true){
        return;
    }
   commanSave(0,path);
}

void MainWindow::on_save_2_released()
{
    QString path = QFileDialog::getExistingDirectory(this,"保存至");
    if (path.isEmpty() == true){
        return;
    }
    commanSave(1,path);
}

void MainWindow::commanSave(int index,QString path){
    auto points = series[index]->pointsVector();
    if (points.size() == 0) {
        QMessageBox::warning(this,"警告", "无数据");
        return;
    }
    auto curTime = QDateTime::currentDateTime().toString("yyyy-MM-dd-hh-mm-ss");
    auto filePath = path + "/"+ QString::number(index+1) +"_" +curTime+".txt";
    QFile file(filePath);
    if (file.open(QIODevice::WriteOnly | QIODevice::Truncate) == false) {
        QMessageBox::warning(this,"警告", "打开文件失败");
        return;
    }
    for (const auto& p : points){
        if (file.write(QString("%1 %2\n").arg(p.x()).arg(p.y()).toUtf8().data()) == -1){
            QMessageBox::warning(this,"警告", "文件读写错误");
            return;
        }
    }
    file.close();
    QMessageBox::information(this,"保存数据至文件",QString("已保存至")+filePath);
}

void MainWindow::on_btnAllRun_2_released()
{
    QString path = QFileDialog::getExistingDirectory(this,"保存至");
    if (path.isEmpty() == true){
        return;
    }
    for (int i = 0; i < 8; ++i) {
        if (series[i]->points().size() != 0)
            commanSave(i,path);
    }
}

