﻿using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using KeyEventHandler = System.Windows.Input.KeyEventHandler;
using MessageBox = System.Windows.MessageBox;
using Point = System.Windows.Point;

namespace JoyPro
{
    /// <summary>
    /// Interaktionslogik für EditJoystickLayoutImage.xaml
    /// </summary>
    public partial class EditJoystickLayoutImage : Window
    {
        string currentSelectedBtnAxis;
        string stick;
        string export;
        System.Drawing.Bitmap backup;
        System.Drawing.Bitmap mainImg;
        Image uiElementImage;
        SolidColorBrush fontColor;
        int textSize;
        Dictionary<string, Point> LabelLocations;
        public EditJoystickLayoutImage(string joystick, string filepath, string exportpath)
        {
            InitializeComponent();
            currentSelectedBtnAxis = "";
            LabelLocations = new Dictionary<string, Point>();
            stick = joystick;
            export = exportpath;
            fontColor = BrushFromHex("#FF000000");
            CloseBtn.Click += new RoutedEventHandler(CloseThis);
            ColorBtn.Click += new RoutedEventHandler(OpenColorPicker);
            PopulateButtonAxisList();
            PopulateFontDropDown();
            if (filepath.EndsWith(".layout"))
            {
                openLayout(filepath);
            }
            else
            {
                initBitMaps(filepath);
            }
            textSize = Convert.ToInt32(TextSizeTB.Text);
            FontDropDown.SelectedIndex = 0;
            ButtonsLB.SelectionChanged += new SelectionChangedEventHandler(SelectedButtonChanged);
            TextSizeTB.LostFocus += new RoutedEventHandler(textSizeChanged);
            TextSizeTB.KeyUp += new KeyEventHandler(textSizeEnterChange);
            FontDropDown.SelectionChanged += new SelectionChangedEventHandler(settingChanged);
            SaveLayoutBtn.Click += new RoutedEventHandler(saveLayout);
            ExportBtn.Click += new RoutedEventHandler(exportInputs);
            refreshImageToShow();
        }
        void textSizeEnterChange(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                textSizeChanged(sender, e);
            }
        }
        void textSizeChanged(object sender, EventArgs e)
        {
            bool succ = false;
            int num = -1;
            succ = int.TryParse(TextSizeTB.Text, out num);
            if (succ == false)
            {
                MessageBox.Show("Not a valid integer as text size");
                TextSizeTB.Text = textSize.ToString();
                return;
            }
            textSize = num;
            refreshImageToShow();
        }
        void initBitMaps(string path)
        {
            Uri fileUri = new Uri(path);
            backup = new System.Drawing.Bitmap(path);
            mainImg = new System.Drawing.Bitmap(path);
        }
        void CloseThis(object sender, EventArgs e)
        {
            Close();
        }
        void SelectedButtonChanged(object sender, EventArgs e)
        {
            currentSelectedBtnAxis = (string)ButtonsLB.SelectedItem;
        }
        void PopulateButtonAxisList()
        {
            List<string> assignedButtons = InternalDataMangement.GetButtonsAxisInUseForStick(stick);
            assignedButtons.Sort();
            assignedButtons.Add("Game");
            assignedButtons.Add("Plane");
            assignedButtons.Add("Joystick");
            ButtonsLB.ItemsSource = assignedButtons;
            ButtonsLB.SelectedIndex = 0;
            currentSelectedBtnAxis = (string)ButtonsLB.SelectedItem;
        }
        void PopulateFontDropDown()
        {
            List<string> fonts = new List<string>();
            InstalledFontCollection installedFonts = new InstalledFontCollection();
            foreach (System.Drawing.FontFamily font in installedFonts.Families)
            {
                fonts.Add(font.Name);
            }
            FontDropDown.ItemsSource = fonts;
        }
        SolidColorBrush BrushFromHex(string hexColorString)
        {
            return (SolidColorBrush)(new BrushConverter().ConvertFrom(hexColorString));
        }
        void OpenColorPicker(object sender, EventArgs e)
        {
            ColorDialog dig = new ColorDialog();
            if (dig.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                fontColor = new SolidColorBrush(Color.FromArgb(dig.Color.A, dig.Color.R, dig.Color.G, dig.Color.B));
            }

            refreshImageToShow();
        }
        void refreshImageToShow()
        {
            mainImg = (System.Drawing.Bitmap)backup.Clone();
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(mainImg);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            System.Drawing.Brush b = new System.Drawing.SolidBrush((System.Drawing.Color)new System.Drawing.ColorConverter().ConvertFromString(new System.Windows.Media.BrushConverter().ConvertToString(fontColor)));
            foreach (KeyValuePair<string, Point> kvp in LabelLocations)
            {
                g.DrawString(kvp.Key, new System.Drawing.Font((string)FontDropDown.SelectedItem, textSize), b, Convert.ToSingle(kvp.Value.X), Convert.ToSingle(kvp.Value.Y));
            }
            g.Flush();

            uiElementImage = new Image();
            uiElementImage.Source = ConverBitmapToBitmapImage(mainImg);
            uiElementImage.Stretch = Stretch.Uniform;
            uiElementImage.MouseLeftButtonUp += new MouseButtonEventHandler(image_MouseLeftButtonUp);
            sv.Content = uiElementImage;
        }
        private void image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point pos = e.GetPosition(uiElementImage);
            AddLabelToImage(pos);
        }
        void settingChanged(object sender, EventArgs e)
        {
            refreshImageToShow();
        }
        void AddLabelToImage(Point pos)
        {
            double scaleFactor = mainImg.Height / uiElementImage.ActualHeight ;
            Point newPos = new Point(pos.X * scaleFactor, pos.Y * scaleFactor);

            if (LabelLocations.ContainsKey(currentSelectedBtnAxis))
                LabelLocations[currentSelectedBtnAxis] = newPos;
            else
                LabelLocations.Add(currentSelectedBtnAxis, newPos);
            refreshImageToShow();
        }
        void openLayout(string filePath)
        {
            LayoutFile lf = MainStructure.ReadFromBinaryFile<LayoutFile>(filePath);
            stick = lf.Joystick;
            backup = (System.Drawing.Bitmap)lf.backup.Clone();
            mainImg = (System.Drawing.Bitmap)lf.backup.Clone();
            fontColor = lf.Color;
            LabelLocations = lf.Positions;
            textSize = lf.Size;
            TextSizeTB.Text = textSize.ToString();
            int toSel = 0;
            for(int i=0; i<FontDropDown.Items.Count; ++i)
            {
                if ((string)FontDropDown.Items[i] == lf.Font) toSel = i;
            }
            FontDropDown.SelectedIndex = toSel;
        }
        void saveLayout(object sender, EventArgs e)
        {
            if (LabelLocations.Count < 1) return;
            LayoutFile lf = new LayoutFile();
            lf.backup = backup;
            lf.Color = fontColor;
            lf.Font = (string)FontDropDown.SelectedItem;
            lf.Joystick = stick;
            lf.Positions = LabelLocations;
            lf.Size = textSize;
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "Layout Files (*.layout)|*.layout|All filed (*.*)|*.*";
            saveFileDialog1.Title = "Save Joystick Layout";
            if (Directory.Exists(MainStructure.msave.lastOpenedLocation))
            {
                saveFileDialog1.InitialDirectory = MainStructure.msave.lastOpenedLocation;
            }
            else
            {
                saveFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            string filePath;
            saveFileDialog1.ShowDialog();
            filePath = saveFileDialog1.FileName;
            MainStructure.WriteToBinaryFile<LayoutFile>(filePath, lf);
        }
        private BitmapImage ConverBitmapToBitmapImage(System.Drawing.Bitmap bmp)
        {
            MemoryStream stream = new MemoryStream();
            bmp.Save(stream, ImageFormat.Png);

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();

            return bitmapImage;
        }
        void exportInputs(object sender, EventArgs e)
        {
            if (LabelLocations.Count < 1)
            {
                MessageBox.Show("No Labels set. Set the labels where you want them before you can export");
                return;
            }
            foreach(KeyValuePair<string, List<string>> kvp in DBLogic.Planes)
            {
                if (!Directory.Exists(export + "\\" + kvp.Key))
                    Directory.CreateDirectory(export + "\\" + kvp.Key);
                for(int i=0; i < kvp.Value.Count; ++i)
                {
                    System.Drawing.Bitmap export = (System.Drawing.Bitmap)backup.Clone();
                    System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(export);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    System.Drawing.Brush b = new System.Drawing.SolidBrush((System.Drawing.Color)new System.Drawing.ColorConverter().ConvertFromString(new System.Windows.Media.BrushConverter().ConvertToString(fontColor)));
                    foreach (KeyValuePair<string, Point> keys in LabelLocations)
                    {
                        string descriptor;
                        if (keys.Key == "Game")
                        {
                            descriptor = kvp.Key;
                        }else if (keys.Key == "Plane")
                        {
                            descriptor = kvp.Value[i];
                        }
                        else if (keys.Key == "Joystick")
                        {
                            descriptor = stick;
                        }
                        else
                        {
                            descriptor = InternalDataMangement.GetDescriptionForJoystickButtonGamePlane(stick, keys.Key, kvp.Key, kvp.Value[i]);
                        }
                        g.DrawString(descriptor, new System.Drawing.Font((string)FontDropDown.SelectedItem, textSize), b, Convert.ToSingle(keys.Value.X), Convert.ToSingle(keys.Value.Y));
                    }
                    g.Flush();
                    export.Save(export + "\\" + kvp.Key + "\\" + kvp.Value[i] +"_"+stick+".png", ImageFormat.Png);
                }
            }
            //One General Overview
            System.Drawing.Bitmap expMain = (System.Drawing.Bitmap)backup.Clone();
            System.Drawing.Graphics gr = System.Drawing.Graphics.FromImage(expMain);
            gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            gr.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            System.Drawing.Brush br = new System.Drawing.SolidBrush((System.Drawing.Color)new System.Drawing.ColorConverter().ConvertFromString(new System.Windows.Media.BrushConverter().ConvertToString(fontColor)));
            foreach (KeyValuePair<string, Point> keys in LabelLocations)
            {
                string descriptor;
                if (keys.Key == "Game")
                {
                    descriptor = "JoyPro";
                }
                else if (keys.Key == "Plane")
                {
                    descriptor = "JoyPro";
                }
                else if (keys.Key == "Joystick")
                {
                    descriptor = stick;
                }
                else
                {
                    descriptor = InternalDataMangement.GetRelationNameForJostickButton(stick, keys.Key);
                }
                gr.DrawString(descriptor, new System.Drawing.Font((string)FontDropDown.SelectedItem, textSize), br, Convert.ToSingle(keys.Value.X), Convert.ToSingle(keys.Value.Y));
            }
            gr.Flush();
            expMain.Save(export + "\\"+stick+".png", ImageFormat.Png);
            MessageBox.Show("Export looks successful.");
        }
    }
}