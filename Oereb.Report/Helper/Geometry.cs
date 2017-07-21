﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Oereb.Report.Helper
{
    public static class Geometry
    {
        /// <summary>
        /// this is a compromise because we don't want attached a GDAL framework => more dependencies
        /// </summary>
        /// <param name="geometryGml"></param>
        /// <param name="extent"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>

        public static Image RasterizeGeometryFromGml(string geometryGml, double[] extent, int width, int height)
        {
            var bitmap = new Bitmap(width, height);
            Graphics graphic = Graphics.FromImage(bitmap);

            graphic.Clear(Color.Transparent);

            if (String.IsNullOrEmpty(geometryGml) )
            {
                return bitmap;
            }

            double conversionFactor = (extent[2] - extent[0]) / width; //scale is the same in both directions
            var gmlElement = RemoveAllNamespaces(geometryGml);

            var rings = gmlElement.XPathSelectElements($"/descendant::posList").ToList();

            if (!rings.Any())
            {
                return bitmap;
            }

            //draws first the transparent color and then the redline color (better results with multipart features)

            for (int colorIndex = 0; colorIndex < 2; colorIndex++)
            {
                foreach (var ring in rings)
                {
                    var content = ring.Value.ToString();
                    var coords = content.Split(' ').Where(x=>!String.IsNullOrEmpty(x)).ToList();

                    var points = new List<PointF>();

                    for (int i = 0; i < coords.Count; i += 2)
                    {
                        var point = new double[2];

                        point[0] = Convert.ToDouble(coords[i]);
                        point[1] = Convert.ToDouble(coords[i+1]);

                        var pX = (float)((point[0] - extent[0]) / conversionFactor);
                        var pY = (float)((extent[3] - point[1]) / conversionFactor);

                        points.Add(new PointF(pX, pY));
                    }

                    if (colorIndex == 0)
                    {
                        Color colorBg = ColorTranslator.FromHtml("#bbffffff"); // TODO: another config value candidate
                        var penBg = new Pen(colorBg, 20);
                        graphic.DrawLines(penBg, points.ToArray());
                    }
                    else
                    {
                        Color color = ColorTranslator.FromHtml("#77ff0000"); // TODO: another config value candidate
                        var pen = new Pen(color, 10);
                        graphic.DrawLines(pen, points.ToArray());
                    }

                    graphic.Flush();
                }                
            }

            bitmap = (Bitmap)AddScalebarAndOrientation(bitmap, extent, width, height, 0.2, 30);

            //bitmap.Save(@"c:\temp\testrasterize6.png", System.Drawing.Imaging.ImageFormat.Png);

            return bitmap;
        }

        public static Image AddScalebarAndOrientation(Image image, double[] extent, int width, int height, double maxPercent, int offset)
        {
            Graphics graphic = Graphics.FromImage(image);
            graphic.SmoothingMode = SmoothingMode.AntiAlias;
            graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;

            //todo replace staic image pathes
            var northArrow = Image.FromFile(Path.Combine(GetRootPath(),"Image/report_NorthArrow.png"));
            var scalebar = Image.FromFile(Path.Combine(GetRootPath(), "Image/report_Scalebar.png"));

            var distanceH = (extent[2] - extent[0]);        
            var meterPerPixel = distanceH/width;

            var widthScaleBarMeter = Math.Round(distanceH * maxPercent / 10, 0)*10;
            var widthScalebarPixel = (int)(widthScaleBarMeter /meterPerPixel);
            var heightScalebarPixel = (int)((double)widthScalebarPixel /scalebar.Width*scalebar.Height);
            var fontheight = (int)36;

            var lrX = (int)0;
            var lrY = (int)height;

            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;

            //graphic.DrawString("0", new Font("Arial", fontheight, FontStyle.Regular, GraphicsUnit.Pixel), Brushes.Black,new PointF() {X = (float)lrX + offset, Y = (float)(lrY - offset)}, stringFormat);
            //graphic.DrawString($"{widthScaleBarMeter/2:0#}", new Font("Arial", fontheight, FontStyle.Regular, GraphicsUnit.Pixel), Brushes.Black, new PointF() { X = (float)(lrX + offset + widthScalebarPixel/2), Y = (float)(lrY - offset) }, stringFormat);
            //graphic.DrawString($"{widthScaleBarMeter:0#} m", new Font("Arial", fontheight, FontStyle.Regular, GraphicsUnit.Pixel), Brushes.Black, new PointF() { X = (float)(lrX + offset + widthScalebarPixel), Y = (float)(lrY - offset) }, stringFormat);

            var font = new Font("Arial", fontheight, FontStyle.Regular, GraphicsUnit.Pixel);
            AddText(graphic, "0", font, stringFormat, new PointF() { X = (float)lrX + offset, Y = (float)(lrY - offset) });
            AddText(graphic, $"{widthScaleBarMeter / 2:0#}", font, stringFormat, new PointF() { X = (float)(lrX + offset + widthScalebarPixel / 2), Y = (float)(lrY - offset) });
            AddText(graphic, $"{widthScaleBarMeter:0#} m", font, stringFormat, new PointF() { X = (float)(lrX + offset + widthScalebarPixel), Y = (float)(lrY - offset) });

            graphic.DrawImage(
                scalebar, 
                new Rectangle(
                    lrX + offset, 
                    lrY - offset- fontheight- heightScalebarPixel, 
                    widthScalebarPixel, 
                    heightScalebarPixel
                )
            );

            var widthNorthArrowPixel = (int)(widthScalebarPixel/5);
            var heightNorthArrowPixel = (int)((double)widthNorthArrowPixel / (double)northArrow.Width * northArrow.Height);

            graphic.DrawImage(
                northArrow, 
                new Rectangle
                (
                    lrX + offset + widthScalebarPixel / 2 - widthNorthArrowPixel / 2, 
                    lrY - offset - fontheight - heightScalebarPixel-offset- heightNorthArrowPixel, 
                    widthNorthArrowPixel, 
                    heightNorthArrowPixel
                )
            );

            graphic.Flush();
            return image;
        }

        public static void AddText(Graphics graphic,string value, Font font, StringFormat stringFormat, PointF pointF)
        {
            var graphicsPath = new GraphicsPath();
            var outline = new Pen(Brushes.White, 8){LineJoin = LineJoin.Round};

            using (Brush foreBrush = new SolidBrush(Color.Black))
            {
                graphicsPath.AddString(value, font.FontFamily, (int)font.Style, font.Size, pointF, stringFormat);
                graphic.DrawPath(outline, graphicsPath);
                graphic.FillPath(foreBrush, graphicsPath);
            }
        }

        public static XElement RemoveAllNamespaces(string xmlDocument)
        {
            return RemoveAllNamespaces(XElement.Parse(xmlDocument));
        }

        private static XElement RemoveAllNamespaces(XElement xmlDocument)
        {
            if (!xmlDocument.HasElements)
            {
                XElement xElement = new XElement(xmlDocument.Name.LocalName);
                xElement.Value = xmlDocument.Value;

                foreach (XAttribute attribute in xmlDocument.Attributes())
                {
                    xElement.Add(attribute);
                }

                return xElement;
            }
            return new XElement(xmlDocument.Name.LocalName, xmlDocument.Elements().Select(el => RemoveAllNamespaces(el)));
        }

        private static string GetRootPath()
        {
            var assemblyPath = (new Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath;
            return new FileInfo(assemblyPath).Directory?.Parent?.FullName;
        }
    }
}
