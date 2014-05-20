﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;

namespace PixIt_0._3
{
    public partial class formMain : Form
    {
        // Dll knihovna pro čtení a zapisování do souboru
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        // Vytvoření bitmap
        Bitmap LoadedImage;
        Bitmap ShowVectors;

        // Načtení ostatních formů
        formSettings settings;
        formManual manualControl;

        // Proměnné
        string[, ,] point = new string[400, 400, 20];

        int[] pointX = new int[400];
        int[] pointY = new int[400];
        string[] directionPoint = new string[400];
        int pointCount = 0;

        int[] pointX_duplicate = new int[400];
        int[] pointY_duplicate = new int[400];
        string[] directionPoint_duplicate = new string[400];


        int[] vectorStartX = new int[400];
        int[] vectorStartY = new int[400];
        int[] vectorEndX = new int[400];
        int[] vectorEndY = new int[400];
        int[] vectorRouteI = new int[400];
        int vectorCount = 0;
        int vectorRoutesCount = 1;



        public static string serialLastReadedValue = "";

        bool settingsFormOpen = false;
        bool manualControlFormOpen = false;
        Form debugFormOpenedID = null;
        bool isPictureLoaded = false;

        // Vytvoření handleru pro sériový port
        public static SerialPort mainSerialPort = new SerialPort();

        int x = 0;
        int y = 0;

        public static string settingColor = "";
        public static Color colorPath = Color.White;
        public static Color colorDrill = Color.White;
        public static Color colorTranslation = Color.White;
        public static int numPort = 3;

        public formMain()
        {
            InitializeComponent();
        }

        // Zapisování do souboru
        public void IniWriteValue(string path, string Section, string Key, string Value)
        {
            var totalPath = Path.Combine(Application.StartupPath, path);
            WritePrivateProfileString(Section, Key, Value, totalPath);
        }

        // Čtení ze souboru
        public string IniReadValue(string path, string Section, string Key)
        {
            var totalPath = Path.Combine(Application.StartupPath, path);
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp, 255, totalPath);
            return temp.ToString();
        }

        // Obnovení Originálního obrázku
        private void ReloadPictureBoxs()
        {
            picOriginal.Image = (Image)LoadedImage;
            picDraw.Image = (Image)ShowVectors;
        }

        // Funkce pro načtení nastavení
        private void Main_Load(object sender, EventArgs e)
        {
            dialogOpenFile.Filter = "All Files (*.*)|*.*";
            dialogOpenFile.FilterIndex = 1;

            //Načtení nastavení z INI
            if (File.Exists(Path.Combine(Application.StartupPath, "settings.ini")) == true)
            {
                colorPath = Color.FromArgb(Convert.ToInt32(IniReadValue("settings.ini", "Colors", "path")));
                colorDrill = Color.FromArgb(Convert.ToInt32(IniReadValue("settings.ini", "Colors", "drill")));
                colorTranslation = Color.FromArgb(Convert.ToInt32(IniReadValue("settings.ini", "Colors", "translation")));
                numPort = Convert.ToInt32(IniReadValue("settings.ini", "COM", "port"));
            }
        }

        // Otevře formulář settings
        private void btnSetttings_Click(object sender, EventArgs e)
        {
            if (settingsFormOpen == false)
            {
                settingsFormOpen = true;
                settings = new formSettings();
                settings.FormClosed += new FormClosedEventHandler(settings_close);
                settings.Show();
            }
        }

        //Uložení nastavení při zavřeni settings
        private void settings_close(object sender, FormClosedEventArgs e)
        {
            //Zapsání nastavení do INI
            settingsFormOpen = false;
            IniWriteValue("settings.ini", "Colors", "path", colorPath.ToArgb().ToString());
            IniWriteValue("settings.ini", "Colors", "drill", colorDrill.ToArgb().ToString());
            IniWriteValue("settings.ini", "Colors", "translation", colorTranslation.ToArgb().ToString());
            IniWriteValue("settings.ini", "COM", "port", numPort.ToString());
            debugAddLine("Okno nastavení bylo uzavřeno");
        }

        // Načtení obrázku
        private void btnLoad_Click(object sender, EventArgs e)
        {
            DialogResult result = dialogOpenFile.ShowDialog();
            if (result == DialogResult.OK)
            {
                LoadedImage = new Bitmap(dialogOpenFile.FileName);
                ShowVectors = new Bitmap(LoadedImage.Width, LoadedImage.Height);
                btnDraw.Enabled = true;
                toolWidth.Text = "Width: " + LoadedImage.Width.ToString();
                toolHeight.Text = "Height: " + LoadedImage.Height.ToString();
                isPictureLoaded = true;
                ReloadPictureBoxs();
                debugAddLine("Byl načten obrázek - " + "Šířka obrázku: " + LoadedImage.Width.ToString() + "    Výška obrázku: " + LoadedImage.Height.ToString());
            }
        }

        // Zobrazení barvy na kterou najede myš
        private void picOriginal_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPictureLoaded == true)
            {
                if (settingsFormOpen == true && e.X < LoadedImage.Width && e.X > 0 && e.Y < LoadedImage.Height && e.Y > 0)
                {
                    settings.picCursorColor.BackColor = LoadedImage.GetPixel(e.X, e.Y);
                }
            }
        }

        // Nastavení barvy pro určitou věc
        private void picOriginal_MouseClick(object sender, MouseEventArgs e)
        {
            if (settingsFormOpen == true)
            {
                switch (settingColor)
                {
                    case "path":
                        colorPath = LoadedImage.GetPixel(e.X, e.Y);
                        debugAddLine("Číslo portu změněno na: " + formMain.numPort);
                        break;

                    case "drill":
                        colorDrill = LoadedImage.GetPixel(e.X, e.Y);
                        debugAddLine("Barva vrtání změněna na: " + formMain.colorDrill);
                        break;

                    case "translation":
                        colorTranslation = LoadedImage.GetPixel(e.X, e.Y);
                        debugAddLine("Barva přechodu změněna na: " + formMain.colorTranslation);
                        break;

                }
                settingColor = ""; settings.loadColors();
            }
        }

        // Když se klikne na button otevření portu
        private void btnPort_Click(object sender, EventArgs e)
        {

            // Pokud port ješte není otevřen
            if (mainSerialPort.IsOpen == false)
            {
                // Nastavení otevření portu
                mainSerialPort.PortName = "COM" + numPort.ToString();
                mainSerialPort.BaudRate = 115200;
                mainSerialPort.DataBits = 8;
                mainSerialPort.Parity = Parity.None;
                mainSerialPort.StopBits = StopBits.One;
                mainSerialPort.Handshake = Handshake.None;
                mainSerialPort.DtrEnable = true;
                mainSerialPort.RtsEnable = true;
                mainSerialPort.WriteTimeout = 300;
                mainSerialPort.ReadTimeout = 300;
                //mainSerialPort.Encoding = Encoding.Unicode;
                mainSerialPort.Encoding = Encoding.GetEncoding(28591);
                //mainSerialPort.DataReceived += DataReceived_Read;
                mainSerialPort.Close();

                // Zkusit otevřít port
                try
                {
                    mainSerialPort.Open();
                }
                // Pokud se port neotevře vypíše patřičnou chybovou hlášku
                catch (Exception ex)
                {
                    toolPortStatus.Text = "Stav portu: Port je uzavřen! Chyba při otevření - " + ex.GetType().ToString();
                    debugAddLine("Chyba při otevření portu: " + ex.GetType().ToString());
                }
                // Pokud je port otevřen, změní stav ve statusu a zapíše do debugu
                if (mainSerialPort.IsOpen == true)
                {
                    picOpenPort.BackColor = Color.Green;
                    btnPort.Text = "Zavřít port";
                    toolPortStatus.Text = "Stav portu: Port je otevřen!";
                    debugAddLine("Port byl otevřen");
                }
            }
            // Pokud port není uzavřen
            else
            {
                // Pokusí se potr zavřít
                try
                {
                    mainSerialPort.Close();
                }
                // Pokud se port nepodaří zavřít vypíše patřičnou chybovou hlášku
                catch (Exception ex)
                {
                    toolPortStatus.Text = "Stav portu: Port je otevřen! Chyba chyba při uzavření - " + ex.GetType().ToString(); 
                    debugAddLine("Chyba při uzavření portu" + ex.GetType().ToString());
                }
                // Pokud se port uzavřel změní stav ve statusu a zapíše do debugu
                if (mainSerialPort.IsOpen == false)
                {
                    picOpenPort.BackColor = Color.Red;
                    btnPort.Text = "Otevřít port";
                    toolPortStatus.Text = "Stav portu: Port je uzavřen!";
                    debugAddLine("Port byl uzavřen");
                }
            }
        }

        //Čtení z Seriového portu
        private void DataReceived_Read(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort mySerial = (SerialPort)sender;

            string data = Convert.ToChar(mySerial.ReadByte()).ToString();

            if (this.InvokeRequired)
            {
                if (System.Windows.Forms.Application.OpenForms["formDebug"] != null)
                {
                    (System.Windows.Forms.Application.OpenForms["formDebug"] as formDebug).listBoxSerialPort.BeginInvoke(new MethodInvoker(delegate
                    {
                        (System.Windows.Forms.Application.OpenForms["formDebug"] as formDebug).listBoxSerialPort.Items.Add(data);

                        (System.Windows.Forms.Application.OpenForms["formDebug"] as formDebug).listBoxSerialPort.SelectedIndex = (System.Windows.Forms.Application.OpenForms["formDebug"] as formDebug).listBoxSerialPort.Items.Count - 1;
                    }));        
                }
            }
        }
        
        //Funkce pro tevření Debug okna
        private void openDebug()
        {
            if (debugFormOpenedID == null)
            {
                debugFormOpenedID = new formDebug();
                debugFormOpenedID.FormClosed += new FormClosedEventHandler(debug_close);
                debugFormOpenedID.Show();
            }
        }

        //Otevře form manuálního ovládání
        private void btnManual_Click(object sender, EventArgs e)
        {
            if (manualControlFormOpen == false)
            {
                manualControlFormOpen = true;
                manualControl = new formManual();
                manualControl.FormClosed += new FormClosedEventHandler(manualControl_close);
                manualControl.Show();
            }
        }

        // Když se form manuálního ovládání uzavře tak změní kontrolní proměnnou
        private void manualControl_close(object sender, FormClosedEventArgs e)
        {
            manualControlFormOpen = false;
            debugAddLine("Manuální ovládání bylo uzavřeno");

        }

        // Otevře form Debugu
        private void buttonDebug_Click(object sender, EventArgs e)
        {
            openDebug();
        }

        //Když se form debugu uzavře tak změní kontrolní proměnnou
        private void debug_close(object sender, FormClosedEventArgs e)
        {
            debugFormOpenedID = null;
        }

        private void btnDraw_Click(object sender, EventArgs e)
        {
            getRoutes();
            getPoints();


            for (int i = 0; i < pointCount; i++)
            {
                listBoxPoints.Items.Add("[" + pointX[i] + "," + pointY[i] + "] " + directionPoint[i]);
            }

            for (int i = 0; i < pointCount; i++)
            {
                pointX_duplicate[i] = pointX[i];
                pointY_duplicate[i] = pointY[i];
                directionPoint_duplicate[i] = directionPoint[i];
            }

            //Smaže křižovatky typu + z pole, nesou potřeba
            for (int i = 0; i < pointCount; i++){
                string XDir = directionPoint[i].Substring(directionPoint[i].IndexOf("X") + 1, 1);
                string YDir = directionPoint[i].Substring(directionPoint[i].IndexOf("Y") + 1, 1);
                if (XDir == "1" && YDir == "1"){
                    pointX[i] = 0;
                    pointY[i] = 0;
                }
            }



            int startPointIndex = 0;
            while (startPointIndex != -1)
            {
                startPointIndex = getStartPoint();
                listBoxRes.Items.Add("----------------------------");
                if (startPointIndex != -1){
                    convertToVectorAndRoute(startPointIndex);
                    vectorRoutesCount++;
                }
            }

            label2.Text = pointCount.ToString();
        }

        //Vrátí index bodu, který je nejbližší začátku pole a má pouze jeden platný směr
        private int getStartPoint(){
            int retVal = -1;

            for (int i = 0; i < pointCount; i++){
                if (pointX[i] != 0 && pointY[i] != 0)
                {
                    string XDir = directionPoint[i].Substring(directionPoint[i].IndexOf("X") + 1, 1);
                    string YDir = directionPoint[i].Substring(directionPoint[i].IndexOf("Y") + 1, 1);

                    if (XDir == "0" && (YDir == "p" || YDir == "n"))
                    {
                        retVal = i;
                        break;
                    }
                    else if (YDir == "0" && (XDir == "p" || XDir == "n"))
                    {
                        retVal = i;
                        break;
                    }
                }
            }

            return retVal;
        }

        //Zjistí trasy v obrázku
        private void getRoutes()
        {
            int citac = 0;
            Color thisPixel = LoadedImage.GetPixel(x, y);

            while (x < LoadedImage.Width - 1 || y < LoadedImage.Height - 1)
            {
                thisPixel = LoadedImage.GetPixel(x, y);
                if (thisPixel == colorPath || thisPixel == colorTranslation)
                {
                    point[x, y, 0] = "ROUTE";
                    citac++;
                }
                else
                {
                    point[x, y, 0] = "NULL";
                }
                if (x < LoadedImage.Width - 1)
                {
                    x++;
                }
                else
                {
                    x = 0; y++;
                }
            } 
            debugAddLine("V obrázku je " + citac + " pixelů trasy");
        }

        //Zjistí body
        private void getPoints()
        {
            for (int i = 2; i < 398; i++)
            {
                for (int u = 2; u < 398; u++)
                {
                    if (point[i, u, 0] == "ROUTE")
                    {
                        if (point[i + 3, u, 0] == "ROUTE" && point[i - 2, u, 0] == "NULL" && point[i, u + 2, 0] == "ROUTE" && point[i, u - 2, 0] == "NULL" && point[i, u + 3, 0] == "NULL" && point[i, u - 1, 0] == "ROUTE" && point[i + 2, u + 1, 0] == "ROUTE")
                        {
                            LoadedImage.SetPixel(i, u, Color.Red);
                            point[i, u, 1] = "XpY0";

                            pointX[pointCount] = i;
                            pointY[pointCount] = u;
                            directionPoint[pointCount] = "XpY0";

                            pointCount++;
                        }
                        else if (point[i + 2, u, 0] == "ROUTE" && point[i - 1, u, 0] == "NULL" && point[i, u + 1, 0] == "NULL" && point[i, u - 2, 0] == "ROUTE" && point[i + 3, u, 0] == "NULL" && point[i, u - 3, 0] == "NULL")
                        {
                            LoadedImage.SetPixel(i, u, Color.Red);
                            point[i, u, 1] = "XpYn";

                            pointX[pointCount] = i;
                            pointY[pointCount] = u;
                            directionPoint[pointCount] = "XpYn";

                            pointCount++;
                        }
                        else if (point[i + 2, u, 0] == "ROUTE" && point[i - 1, u, 0] == "NULL" && point[i, u + 2, 0] == "NULL" && point[i, u - 2, 0] == "NULL")
                        {
                            LoadedImage.SetPixel(i, u, Color.Red);
                            point[i, u, 1] = "XpY0";

                            pointX[pointCount] = i;
                            pointY[pointCount] = u;
                            directionPoint[pointCount] = "XpY0";

                            pointCount++;
                        }
                        else if (point[i + 1, u, 0] == "NULL" && point[i - 2, u, 0] == "ROUTE" && point[i, u + 2, 0] == "NULL" && point[i, u - 2, 0] == "NULL")
                        {
                            LoadedImage.SetPixel(i, u, Color.Red);
                            point[i, u, 1] = "XnY0";

                            pointX[pointCount] = i;
                            pointY[pointCount] = u;
                            directionPoint[pointCount] = "XnY0";

                            pointCount++;
                        }
                        else if (point[i + 2, u, 0] == "NULL" && point[i - 2, u, 0] == "NULL" && point[i, u + 2, 0] == "ROUTE" && point[i, u - 1, 0] == "NULL")
                        {
                            LoadedImage.SetPixel(i, u, Color.Red);
                            point[i, u, 1] = "X0Yp";

                            pointX[pointCount] = i;
                            pointY[pointCount] = u;
                            directionPoint[pointCount] = "X0Yp";

                            pointCount++;
                        }
                        else if (point[i + 2, u, 0] == "NULL" && point[i - 2, u, 0] == "NULL" && point[i, u + 1, 0] == "NULL" && point[i, u - 2, 0] == "ROUTE")
                        {
                            LoadedImage.SetPixel(i, u, Color.Red);
                            point[i, u, 1] = "X0Yn";

                            pointX[pointCount] = i;
                            pointY[pointCount] = u;
                            directionPoint[pointCount] = "X0Yn";

                            pointCount++;
                        }
                        else if (point[i + 3, u, 0] == "ROUTE" && point[i - 2, u, 0] == "NULL" && point[i, u + 3, 0] == "ROUTE" && point[i, u - 3, 0] == "ROUTE" && point[i + 2, u - 2, 0] == "NULL" && point[i + 2, u + 2, 0] == "NULL")
                        {
                            LoadedImage.SetPixel(i, u, Color.Red);
                            point[i, u, 1] = "XpY1";

                            pointX[pointCount] = i;
                            pointY[pointCount] = u;
                            directionPoint[pointCount] = "XpY1";

                            pointCount++;
                        }
                        else if (point[i + 2, u, 0] == "NULL" && point[i - 3, u, 0] == "ROUTE" && point[i, u + 3, 0] == "ROUTE" && point[i, u - 3, 0] == "ROUTE" && point[i - 2, u - 2, 0] == "NULL" && point[i - 2, u + 2, 0] == "NULL")
                        {
                            LoadedImage.SetPixel(i, u, Color.Red);
                            point[i, u, 1] = "XnY1";

                            pointX[pointCount] = i;
                            pointY[pointCount] = u;
                            directionPoint[pointCount] = "XnY1";

                            pointCount++;
                        }
                        else if (point[i + 3, u, 0] == "ROUTE" && point[i - 3, u, 0] == "ROUTE" && point[i, u + 3, 0] == "ROUTE" && point[i, u - 2, 0] == "NULL" && point[i + 2, u + 2, 0] == "NULL" && point[i - 2, u + 2, 0] == "NULL")
                        {
                            LoadedImage.SetPixel(i, u, Color.Red);
                            point[i, u, 1] = "X1Yp";

                            pointX[pointCount] = i;
                            pointY[pointCount] = u;
                            directionPoint[pointCount] = "X1Yp";

                            pointCount++;
                        }
                        else if (point[i + 3, u, 0] == "ROUTE" && point[i - 3, u, 0] == "ROUTE" && point[i, u + 2, 0] == "NULL" && point[i, u - 3, 0] == "ROUTE" && point[i + 2, u - 2, 0] == "NULL" && point[i - 2, u - 2, 0] == "NULL")
                        {
                            LoadedImage.SetPixel(i, u, Color.Red);
                            point[i, u, 1] = "X1Yn";

                            pointX[pointCount] = i;
                            pointY[pointCount] = u;
                            directionPoint[pointCount] = "X1Yn";

                            pointCount++;
                        }
                        else if (point[i + 3, u, 0] == "ROUTE" && point[i - 2, u, 0] == "NULL" && point[i, u + 3, 0] == "ROUTE" && point[i, u - 2, 0] == "NULL" && point[i + 2, u + 2, 0] == "NULL")
                        {
                            LoadedImage.SetPixel(i, u, Color.Red);
                            point[i, u, 1] = "XpYp";

                            pointX[pointCount] = i;
                            pointY[pointCount] = u;
                            directionPoint[pointCount] = "XpYp";

                            pointCount++;
                        }
                        else if (point[i + 2, u, 0] == "NULL" && point[i - 3, u, 0] == "ROUTE" && point[i, u + 3, 0] == "ROUTE" && point[i, u - 2, 0] == "NULL" && point[i - 2, u + 2, 0] == "NULL")
                        {
                            LoadedImage.SetPixel(i, u, Color.Red);
                            point[i, u, 1] = "XnYp";

                            pointX[pointCount] = i;
                            pointY[pointCount] = u;
                            directionPoint[pointCount] = "XnYp";

                            pointCount++;
                        }
                        else if (point[i + 3, u, 0] == "ROUTE" && point[i - 2, u, 0] == "NULL" && point[i, u + 2, 0] == "NULL" && point[i, u - 3, 0] == "ROUTE" && point[i + 2, u - 2, 0] == "NULL")
                        {
                            LoadedImage.SetPixel(i, u, Color.Red);
                            point[i, u, 1] = "XpYn";

                            pointX[pointCount] = i;
                            pointY[pointCount] = u;
                            directionPoint[pointCount] = "XpYn";

                            pointCount++;
                        }
                        else if (point[i + 2, u, 0] == "NULL" && point[i - 3, u, 0] == "ROUTE" && point[i, u + 2, 0] == "NULL" && point[i, u - 3, 0] == "ROUTE" && point[i - 2, u - 2, 0] == "NULL")
                        {
                            LoadedImage.SetPixel(i, u, Color.Red);
                            point[i, u, 1] = "XnYn";

                            pointX[pointCount] = i;
                            pointY[pointCount] = u;
                            directionPoint[pointCount] = "XnYn";

                            pointCount++;
                        }
                        else if (point[i + 3, u, 0] == "ROUTE" && point[i - 3, u, 0] == "ROUTE" && point[i, u + 3, 0] == "ROUTE" && point[i, u - 3, 0] == "ROUTE")
                        {
                            LoadedImage.SetPixel(i, u, Color.Red);
                            point[i, u, 1] = "X1Y1";

                            pointX[pointCount] = i;
                            pointY[pointCount] = u;
                            directionPoint[pointCount] = "X1Y1";

                            pointCount++;
                        }
                    }
                }
            } 
            
            ReloadPictureBoxs();
        }

        //Zpracuje vektor
        private void processVector(int checkIndex, int i, string deleteDirection, bool deletePoint)
        {
            listBoxRes.Items.Add("  Nalezen bod: " + pointX[i] + "," + pointY[i]);

            //Uloží dvojci bodů jako vektor
            //Zapíše StartX/Y
            vectorStartX[vectorCount] = pointX[checkIndex];
            vectorStartY[vectorCount] = pointY[checkIndex];
            //Zapíše EndX/Y
            vectorEndX[vectorCount] = pointX[i];
            vectorEndY[vectorCount] = pointY[i];

            vectorRouteI[vectorCount] = vectorRoutesCount;
            listBoxDecodedVectors.Items.Add(vectorRouteI[vectorCount] + " [" + vectorStartX[vectorCount] + "," + vectorStartY[vectorCount] + "] -> [" + vectorEndX[vectorCount] + "," + vectorEndY[vectorCount] + "]");

            //Uloží
            vectorCount++;

            //Smaže první (začáteční) bod
            if (deletePoint == true){
                pointX[checkIndex] = 0;
                pointY[checkIndex] = 0;
            }
            
        }

        //Najde nejbližší platný bod pro zadaný bod
        private int copmarePoints(int pointIndex, string direction)
        {
            int retVal = 0;
            int compareValue;
            int minCompareIndex = -1;

            switch (direction)
            {
                case "Yp":
                    compareValue = LoadedImage.Height;

                    for (int i = 0; i < pointCount; i++){
                        string YDir = directionPoint[i].Substring(directionPoint[i].IndexOf("Y") + 1, 1);

                        if (pointX[pointIndex] == pointX[i] && pointY[i] < compareValue && YDir == "n" && pointY[i] > pointY[pointIndex])
                        {
                            compareValue = pointY[i];
                            minCompareIndex = i;
                            listBoxRes.Items.Add("Nalezen směr!");
                        }

                        if (pointX[pointIndex] == pointX[i] && pointY[i] < compareValue && YDir == "1" && pointY[i] > pointY[pointIndex])
                        {
                            compareValue = pointY[i];
                            minCompareIndex = i;
                            listBoxRes.Items.Add("Nalezena křižovatka! (" + directionPoint[i] + ") Index: " + minCompareIndex);
                        }
                    }

                    if (compareValue == LoadedImage.Height) { retVal = -1; } else { retVal = minCompareIndex; }
                    break;

                case "Yn":
                    compareValue = 0;

                    for (int i = 0; i < pointCount; i++)
                    {
                        string YDir = directionPoint[i].Substring(directionPoint[i].IndexOf("Y") + 1, 1);

                        if (pointX[pointIndex] == pointX[i] && pointY[i] > compareValue && YDir == "p" && pointY[i] < pointY[pointIndex])
                        {
                            compareValue = pointY[i];
                            minCompareIndex = i;
                            listBoxRes.Items.Add("  -Nalezen směr!");
                        }

                        if (pointX[pointIndex] == pointX[i] && pointY[i] > compareValue && YDir == "1" && pointY[i] < pointY[pointIndex])
                        {
                            compareValue = pointY[i];
                            minCompareIndex = i;
                            listBoxRes.Items.Add("  -Nalezena křižovatka! (" + directionPoint[i] + ") Index: " + minCompareIndex);
                        }
                    }

                    if (compareValue == 0) { retVal = -1; } else { retVal = minCompareIndex; }
                    break;

                case "Xp":
                    compareValue = LoadedImage.Width;
                    

                    //Křižovatky
                    for (int i = 0; i < pointCount; i++)
                    {
                        string XDir = directionPoint[i].Substring(directionPoint[i].IndexOf("X") + 1, 1);

                        if (pointY[pointIndex] == pointY[i] && pointX[i] < compareValue && XDir == "n" && pointX[i] > pointX[pointIndex])
                        {
                            compareValue = pointX[i];
                            minCompareIndex = i;
                        }

                        if (pointY[pointIndex] == pointY[i] && pointX[i] < compareValue && XDir == "1" && pointX[i] > pointX[pointIndex])
                        {
                            compareValue = pointX[i];
                            minCompareIndex = i;
                        }
                    }

                    if (compareValue == LoadedImage.Width) { retVal = -1; } else { retVal = minCompareIndex; }
                    break;

                case "Xn":
                    compareValue = 0;

                    for (int i = 0; i < pointCount; i++)
                    {
                        string XDir = directionPoint[i].Substring(directionPoint[i].IndexOf("X") + 1, 1);

                        if (pointY[pointIndex] == pointY[i] && pointX[i] > compareValue && XDir == "p" && pointX[i] < pointX[pointIndex])
                        {
                            compareValue = pointX[i];
                            minCompareIndex = i;
                        }

                        if (pointY[pointIndex] == pointY[i] && pointX[i] > compareValue && XDir == "1" && pointX[i] < pointX[pointIndex])
                        {
                            compareValue = pointX[i];
                            minCompareIndex = i;
                        }
                    }

                    if (compareValue == 0) { retVal = -1; } else { retVal = minCompareIndex; }
                    break;
            }

            listBox1.Items.Add(retVal);
            return retVal;
        }

        private void convertToVectorAndRoute(int checkIndex)
        {
            string deleteDirection = "";
            string XDir; string YDir;

            do{
                XDir = "";
                YDir = "";

                if (deleteDirection != ""){
                    listBoxRes.Items.Add("deleteDirection: " + deleteDirection);

                    //Pro křižovatky
                    if (directionPoint[checkIndex].Substring(directionPoint[checkIndex].IndexOf("X") + 1, 1) == "1" || directionPoint[checkIndex].Substring(directionPoint[checkIndex].IndexOf("Y") + 1, 1) == "1")
                    {
                        //Pokud je Lrozcestí
                        if (directionPoint[checkIndex].Substring(directionPoint[checkIndex].IndexOf(deleteDirection.Substring(0, 1)) + 1, 1) == "1")
                        {
                            //Obrátí směr
                            string reverseDir = deleteDirection.Substring(1, 1);
                            if (reverseDir == "n") { reverseDir = "p"; }
                            else if (reverseDir == "p") { reverseDir = "n"; }
                            else { reverseDir = "9"; }

                            directionPoint[checkIndex] = directionPoint[checkIndex].Remove(directionPoint[checkIndex].IndexOf(deleteDirection.Substring(0, 1)) + 1, 1);
                            directionPoint[checkIndex] = directionPoint[checkIndex].Insert(directionPoint[checkIndex].IndexOf(deleteDirection.Substring(0, 1)) + 1, reverseDir);
                            
                            listBoxRes.Items.Add("Upraven index " + checkIndex + " na: " + directionPoint[checkIndex] + " (Lrozcestí)");
                        }else{
                            //Pokud je -Rozcestí

                            //Přepíše
                            directionPoint[checkIndex] = directionPoint[checkIndex].Remove(directionPoint[checkIndex].IndexOf(deleteDirection.Substring(0, 1)) + 1, 1);
                            directionPoint[checkIndex] = directionPoint[checkIndex].Insert(directionPoint[checkIndex].IndexOf(deleteDirection.Substring(0, 1)) + 1, "0");

                            listBoxRes.Items.Add("Upraven index " + checkIndex + " na: " + directionPoint[checkIndex] + " (-rozcestí)");
                        }

                        listBoxRes.Items.Add("Upraveno deleteDirection (křižovatka)");
                    }else{
                    //Pro směry
                        directionPoint[checkIndex] = directionPoint[checkIndex].Replace(deleteDirection, deleteDirection.Substring(0, 1) + "0");
                        listBoxRes.Items.Add("Upraven index " + checkIndex + " na: " + directionPoint[checkIndex] + " (smer)");
                        listBoxRes.Items.Add("Upraveno deleteDirection (směr)");
                    }
                }


               listBoxRes.Items.Add("Bod: [" + pointX[checkIndex] + "," + pointY[checkIndex] + "]" + "Index: " + checkIndex);
               XDir = directionPoint[checkIndex].Substring(directionPoint[checkIndex].IndexOf("X") + 1, 1);
               YDir = directionPoint[checkIndex].Substring(directionPoint[checkIndex].IndexOf("Y") + 1, 1);
               listBoxRes.Items.Add(" XDir: " + XDir + " / YDir: " + YDir);

                //4 základní směry
                if (XDir == "0" && YDir == "p")
                {
                    int pointFindIndex = copmarePoints(checkIndex, "Yp");
                    if(pointFindIndex != -1){
                        processVector(checkIndex, pointFindIndex, deleteDirection, true);
                        checkIndex = pointFindIndex;
                        deleteDirection = "Yn";
                    } else { listBoxRes.Items.Add("ForceBreak! (X0Yp)"); break; }
                }
                else if (XDir == "0" && YDir == "n")
                {
                    int pointFindIndex = copmarePoints(checkIndex, "Yn");
                    if (pointFindIndex != -1)
                    {
                        processVector(checkIndex, pointFindIndex, deleteDirection, true);
                        checkIndex = pointFindIndex;
                        deleteDirection = "Yp";
                    }else { listBoxRes.Items.Add("ForceBreak! (X0Yn)"); break; }
                }
                else if (XDir == "p" && YDir == "0")
                {
                    int pointFindIndex = copmarePoints(checkIndex, "Xp");
                    if (pointFindIndex != -1)
                    {
                        processVector(checkIndex, pointFindIndex, deleteDirection, true);
                        checkIndex = pointFindIndex;
                        deleteDirection = "Xn";
                    }else { listBoxRes.Items.Add("ForceBreak! (XpY0)"); break; }
                }
                else if (XDir == "n" && YDir == "0")
                {
                    int pointFindIndex = copmarePoints(checkIndex, "Xn");
                    if (pointFindIndex != -1)
                    {
                        processVector(checkIndex, pointFindIndex, deleteDirection, true);
                        checkIndex = pointFindIndex;
                        deleteDirection = "Xp";
                    }
                    else { listBoxRes.Items.Add("ForceBreak! (XnY0)"); break; }
                }
                //Pro T-Rozcestí
                else if(XDir == "p" && YDir == "n"){
                    string checkVal = "";

                    string reverseDir = deleteDirection.Substring(1, 1);
                    if (reverseDir == "n") { reverseDir = "p"; }
                    else if (reverseDir == "p") { reverseDir = "n"; }
                    else { reverseDir = "9"; }
                        
                    checkVal = deleteDirection.Substring(0, 1) + reverseDir;
                    listBoxRes.Items.Add(checkVal);


                    int pointFindIndex = copmarePoints(checkIndex, checkVal);
                    if (pointFindIndex != -1)
                    {
                        processVector(checkIndex, pointFindIndex, deleteDirection, false);

                    }
                    else { listBoxRes.Items.Add("ForceBreak!"); break; }

                    //Smaže cestu, kterou lze pokračovat 
                    directionPoint[checkIndex] = directionPoint[checkIndex].Remove(directionPoint[checkIndex].IndexOf(deleteDirection.Substring(0, 1)) + 1, 1);
                    directionPoint[checkIndex] = directionPoint[checkIndex].Insert(directionPoint[checkIndex].IndexOf(deleteDirection.Substring(0, 1)) + 1, "0");

                    checkIndex = pointFindIndex;
                }
                else if (XDir == "n" && YDir == "p")
                {
                    string checkVal = "";

                    string reverseDir = deleteDirection.Substring(1, 1);
                    if (reverseDir == "n") { reverseDir = "p"; }
                    else if (reverseDir == "p") { reverseDir = "n"; }
                    else { reverseDir = "9"; }

                    checkVal = deleteDirection.Substring(0, 1) + reverseDir;
                    listBoxRes.Items.Add(checkVal);


                    int pointFindIndex = copmarePoints(checkIndex, checkVal);
                    if (pointFindIndex != -1)
                    {
                        processVector(checkIndex, pointFindIndex, deleteDirection, false);

                    }
                    else { listBoxRes.Items.Add("ForceBreak!"); break; }

                    //Smaže cestu, kterou lze pokračovat 
                    directionPoint[checkIndex] = directionPoint[checkIndex].Remove(directionPoint[checkIndex].IndexOf(deleteDirection.Substring(0, 1)) + 1, 1);
                    directionPoint[checkIndex] = directionPoint[checkIndex].Insert(directionPoint[checkIndex].IndexOf(deleteDirection.Substring(0, 1)) + 1, "0");

                    checkIndex = pointFindIndex;
                    
                }
                else if (XDir == "n" && YDir == "n")
                {
                    string checkVal = "";

                    string reverseDir = deleteDirection.Substring(1, 1);
                    if (reverseDir == "n") { reverseDir = "p"; }
                    else if (reverseDir == "p") { reverseDir = "n"; }
                    else { reverseDir = "9"; }

                    checkVal = deleteDirection.Substring(0, 1) + reverseDir;
                    listBoxRes.Items.Add(checkVal);


                    int pointFindIndex = copmarePoints(checkIndex, checkVal);
                    if (pointFindIndex != -1)
                    {
                        processVector(checkIndex, pointFindIndex, deleteDirection, false);

                    }
                    else { listBoxRes.Items.Add("ForceBreak!"); break; }

                    //Smaže cestu, kterou lze pokračovat 
                    directionPoint[checkIndex] = directionPoint[checkIndex].Remove(directionPoint[checkIndex].IndexOf(deleteDirection.Substring(0, 1)) + 1, 1);
                    directionPoint[checkIndex] = directionPoint[checkIndex].Insert(directionPoint[checkIndex].IndexOf(deleteDirection.Substring(0, 1)) + 1, "0");

                    checkIndex = pointFindIndex;
                }
                else if (XDir == "p" && YDir == "p")
                {
                    string checkVal = "";

                    string reverseDir = deleteDirection.Substring(1, 1);
                    if (reverseDir == "n") { reverseDir = "p"; }
                    else if (reverseDir == "p") { reverseDir = "n"; }
                    else { reverseDir = "9"; }

                    checkVal = deleteDirection.Substring(0, 1) + reverseDir;
                    listBoxRes.Items.Add(checkVal);


                    int pointFindIndex = copmarePoints(checkIndex, checkVal);
                    if (pointFindIndex != -1)
                    {
                        processVector(checkIndex, pointFindIndex, deleteDirection, false);
                    }
                    else { listBoxRes.Items.Add("ForceBreak!"); break; }

                    //Smaže cestu, kterou lze pokračovat 
                    directionPoint[checkIndex] = directionPoint[checkIndex].Remove(directionPoint[checkIndex].IndexOf(deleteDirection.Substring(0, 1)) + 1, 1);
                    directionPoint[checkIndex] = directionPoint[checkIndex].Insert(directionPoint[checkIndex].IndexOf(deleteDirection.Substring(0, 1)) + 1, "0");

                    checkIndex = pointFindIndex;
                }
                //Pro -Rozcestí
                else if (XDir == "1" && YDir == "0")
                {
                    //Na rozcestí nelze pokračovat po směru, ze kterého jsem přišel
                    //Zvolí preferovaný směr (doprava +)
                    string checkVal = "Xp";

                    int pointFindIndex = copmarePoints(checkIndex, checkVal);
                    if (pointFindIndex != -1)
                    {
                        processVector(checkIndex, pointFindIndex, deleteDirection, false);
                    }
                    else { listBoxRes.Items.Add("ForceBreak!"); break; }

                    //Přepíše bod aby zůstal pouze platný směr (ten kterým tento cyklus nepojedeme)
                    directionPoint[checkIndex] = directionPoint[checkIndex].Remove(directionPoint[checkIndex].IndexOf("X") + 1, 1);
                    directionPoint[checkIndex] = directionPoint[checkIndex].Insert(directionPoint[checkIndex].IndexOf("X") + 1, "n");
                    listBoxRes.Items.Add("Upraven index " + checkIndex + " na " + directionPoint[checkIndex]);

                    YDir = "0";


                    checkIndex = pointFindIndex;
                    deleteDirection = "Xn";
                }
                else if (XDir == "0" && YDir == "1")
                {
                    //Na rozcestí nelze pokračovat po směru, ze kterého jsem přišel
                    //Zvolí preferovaný směr (nahoru -)
                    string checkVal = "Yn";

                    int pointFindIndex = copmarePoints(checkIndex, checkVal);
                    if (pointFindIndex != -1)
                    {
                        processVector(checkIndex, pointFindIndex, deleteDirection, false);
                    }
                    else { listBoxRes.Items.Add("ForceBreak!"); break; }

                    listBoxRes.Items.Add("Hodnota po po provedení: " + directionPoint[checkIndex] + "(" + pointX[checkIndex] + "," + pointY[checkIndex] + ") I: " + checkIndex);

                    //Přepíše bod aby zůstal pouze platný směr (ten kterým tento cyklus nepojedeme)
                    directionPoint[checkIndex] = directionPoint[checkIndex].Remove(directionPoint[checkIndex].IndexOf("Y") + 1, 1);
                    directionPoint[checkIndex] = directionPoint[checkIndex].Insert(directionPoint[checkIndex].IndexOf("Y") + 1, "p");

                    

                    YDir = "0";

                    checkIndex = pointFindIndex;
                    deleteDirection = "Yp";
                }

                else { break; }

            } while (true);

            //Pokud už nelze najít další bod cesty, smaže i poslední použitý bod
            pointX[checkIndex] = 0;
            pointY[checkIndex] = 0;

            //Aktualizuje body v listBoxVectors
            listBoxPoints.Items.Clear();
            for (int i = 0; i < pointCount; i++){
                listBoxPoints.Items.Add("[" + pointX_duplicate[i] + "," + pointY_duplicate[i] + "] " + directionPoint_duplicate[i]);
            }




        }

        public void debugAddLine(string text)
        {
                if (System.Windows.Forms.Application.OpenForms["formDebug"] != null)
                {
                    (System.Windows.Forms.Application.OpenForms["formDebug"] as formDebug).addLineDebug(text);
                }
        }

        private void listBoxVectors_SelectedIndexChanged(object sender, EventArgs e)
        {
            ShowVectors.Dispose();
            ShowVectors = new Bitmap(LoadedImage.Width, LoadedImage.Height);
            ShowVectors.SetPixel(pointX_duplicate[listBoxPoints.SelectedIndex], pointY_duplicate[listBoxPoints.SelectedIndex], Color.DarkBlue);
            //ShowVectors.SetPixel(decodeVectorPointStartX[listBoxVectors.SelectedIndex], decodeVectorPointStartY[listBoxVectors.SelectedIndex], Color.Blue);
            //ShowVectors.SetPixel(decodeVectorPointEndX[listBoxVectors.SelectedIndex], decodeVectorPointEndY[listBoxVectors.SelectedIndex], Color.Red);
            ReloadPictureBoxs();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            label1.Text = vectorCount.ToString();

            ShowVectors.Dispose();
            ShowVectors = new Bitmap(LoadedImage.Width, LoadedImage.Height);

            for (int ii = 0; ii < vectorCount ; ii++)
            {

            int x = vectorEndX[ii] - vectorStartX[ii];
            int y = vectorEndY[ii] - vectorStartY[ii];
            

            if (x != 0)
            {
                for (int i = 0; i <= Math.Abs(x); i++)
                {
                    if (x > 0)
                    {
                        ShowVectors.SetPixel(vectorStartX[ii] + i, vectorStartY[ii], Color.Green);
                    }

                    if (x < 0)
                    {
                        ShowVectors.SetPixel(vectorStartX[ii] - i, vectorStartY[ii], Color.Green);
                    }
                }
            }

            if (y != 0)
            {
                for (int i = 0; i <= Math.Abs(y); i++)
                {
                    if (y > 0)
                    {
                        ShowVectors.SetPixel(vectorStartX[ii], vectorStartY[ii] + i, Color.Green);
                    }

                    if (y < 0)
                    {
                        ShowVectors.SetPixel(vectorStartX[ii], vectorStartY[ii] - i, Color.Green);
                        
                    }
                }
            }

            }

            ReloadPictureBoxs();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            listBoxPoints.SelectedIndex = (int)numericUpDown1.Value;
        }

    }

}
